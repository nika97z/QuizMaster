using QuizMaster.Domain.Exceptions;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Domain.Models.Enums;
using QuizMaster.Infrastructure.Ai;
using System.Net;
using System.Text;
using System.Text.Json;

namespace QuizMaster.Tests.Ai
{
	/// <summary>
	/// The OpenAI generator against a fake HTTP handler: no real request is ever sent.
	/// </summary>
	public class OpenAiQuizGeneratorTests
	{
		private sealed class FakeHandler : HttpMessageHandler
		{
			private readonly HttpStatusCode _status;
			private readonly string _body;

			public FakeHandler(HttpStatusCode status, string body)
			{
				_status = status;
				_body = body;
			}

			public HttpRequestMessage? Request { get; private set; }
			public string? RequestBody { get; private set; }

			protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				Request = request;
				if (request.Content != null)
				{
					using var reader = new StreamReader(request.Content.ReadAsStream(cancellationToken));
					RequestBody = reader.ReadToEnd();
				}
				return new HttpResponseMessage(_status) { Content = new StringContent(_body, Encoding.UTF8, "application/json") };
			}

			protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				return Task.FromResult(Send(request, cancellationToken));
			}
		}

		private static readonly OpenAiSettings Settings = new() { ApiKey = "test-key", Model = "test-model" };

		private static (OpenAiQuizGenerator Generator, FakeHandler Handler) Create(HttpStatusCode status, string body, OpenAiSettings? settings = null)
		{
			var handler = new FakeHandler(status, body);
			return (new OpenAiQuizGenerator(settings ?? Settings, new HttpClient(handler)), handler);
		}

		private static string Completion(object quiz, string finishReason = "stop")
		{
			return JsonSerializer.Serialize(new
			{
				choices = new[]
				{
					new { index = 0, message = new { role = "assistant", content = JsonSerializer.Serialize(quiz), refusal = (string?)null }, finish_reason = finishReason }
				}
			});
		}

		private static readonly object SampleQuiz = new
		{
			title = "Photosynthesis Basics",
			topic = "Biology",
			questions = new object[]
			{
				new { type = "single", text = "Which pigment captures light?", points = 2, options = new[] { "Chlorophyll", "Melanin", "Keratin" }, correctOptionIndexes = new[] { 0 } },
				new { type = "multi", text = "Which are products?", points = 3, options = new[] { "Oxygen", "Glucose", "Iron", "Salt" }, correctOptionIndexes = new[] { 0, 1 } },
				new { type = "truefalse", text = "Roots do photosynthesis.", points = 1, options = new[] { "True", "False" }, correctOptionIndexes = new[] { 1 } }
			}
		};

		[Fact]
		public void Generate_SendsAStructuredOutputRequestAndReadsTheQuiz()
		{
			var (generator, handler) = Create(HttpStatusCode.OK, Completion(SampleQuiz));

			var quiz = generator.Generate("Photosynthesis", 3);

			Assert.Equal("Photosynthesis Basics", quiz.Title);
			Assert.Equal("Biology", quiz.Topic);
			Assert.Equal(new[] { QuestionType.Single, QuestionType.Multi, QuestionType.TrueFalse }, quiz.Questions.Select(q => q.Type));
			Assert.Equal(new[] { "Chlorophyll", "Melanin", "Keratin" }, quiz.Questions[0].Options);
			Assert.Equal(new[] { 0, 1 }, quiz.Questions[1].CorrectOptionIndexes);
			Assert.Empty(quiz.Questions[2].Options);
			Assert.Equal(new[] { TrueFalseQuestion.FalseIndex }, quiz.Questions[2].CorrectOptionIndexes);

			Assert.Equal("https://api.openai.com/v1/chat/completions", handler.Request!.RequestUri!.ToString());
			Assert.Equal("Bearer test-key", handler.Request.Headers.Authorization!.ToString());
			using var body = JsonDocument.Parse(handler.RequestBody!);
			Assert.Equal("test-model", body.RootElement.GetProperty("model").GetString());
			var format = body.RootElement.GetProperty("response_format");
			Assert.Equal("json_schema", format.GetProperty("type").GetString());
			Assert.True(format.GetProperty("json_schema").GetProperty("strict").GetBoolean());
			var prompt = body.RootElement.GetProperty("messages")[1].GetProperty("content").GetString();
			Assert.Contains("\"Photosynthesis\"", prompt);
			Assert.Contains("Exactly 3 questions", prompt);
		}

		[Fact]
		public void Generate_TrueFalseAnswer_FollowsTheOptionText()
		{
			var quiz = new
			{
				title = "T",
				topic = "B",
				questions = new[] { new { type = "truefalse", text = "Water is wet.", points = 1, options = new[] { "False", "True" }, correctOptionIndexes = new[] { 1 } } }
			};
			var (generator, _) = Create(HttpStatusCode.OK, Completion(quiz));

			var result = generator.Generate("Water", 1);

			Assert.Equal(new[] { TrueFalseQuestion.TrueIndex }, result.Questions[0].CorrectOptionIndexes);
		}

		[Theory]
		[InlineData(HttpStatusCode.Unauthorized, "invalid_api_key", "API key")]
		[InlineData(HttpStatusCode.NotFound, "model_not_found", "test-model")]
		[InlineData(HttpStatusCode.TooManyRequests, "insufficient_quota", "no credit left")]
		[InlineData(HttpStatusCode.TooManyRequests, "rate_limit_exceeded", "rate limit")]
		[InlineData(HttpStatusCode.InternalServerError, null, "problem right now")]
		public void Generate_ErrorFromOpenAi_IsExplained(HttpStatusCode status, string? code, string expected)
		{
			var body = JsonSerializer.Serialize(new { error = new { message = "Something failed.", type = "error", code } });
			var (generator, _) = Create(status, body);

			var ex = Assert.Throws<QuizGenerationException>(() => generator.Generate("Photosynthesis", 3));

			Assert.Contains(expected, ex.Message);
		}

		[Fact]
		public void Generate_AnswerCutOff_AsksForFewerQuestions()
		{
			var (generator, _) = Create(HttpStatusCode.OK, Completion(SampleQuiz, finishReason: "length"));

			var ex = Assert.Throws<QuizGenerationException>(() => generator.Generate("Photosynthesis", 3));

			Assert.Contains("fewer questions", ex.Message);
		}

		[Fact]
		public void Generate_AnswerThatIsNotJson_IsReported()
		{
			var body = JsonSerializer.Serialize(new { choices = new[] { new { message = new { content = "Sorry, here is a quiz: ..." }, finish_reason = "stop" } } });
			var (generator, _) = Create(HttpStatusCode.OK, body);

			var ex = Assert.Throws<QuizGenerationException>(() => generator.Generate("Photosynthesis", 3));

			Assert.Contains("couldn't be read", ex.Message);
		}

		[Fact]
		public void Generate_WithoutAnApiKey_SendsNothing()
		{
			var (generator, handler) = Create(HttpStatusCode.OK, Completion(SampleQuiz), new OpenAiSettings { ApiKey = "" });

			Assert.False(generator.IsConfigured);
			Assert.Throws<QuizGenerationException>(() => generator.Generate("Photosynthesis", 3));
			Assert.Null(handler.Request);
		}
	}
}

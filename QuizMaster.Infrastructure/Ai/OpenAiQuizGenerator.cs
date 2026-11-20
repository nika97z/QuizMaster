using QuizMaster.Domain.Exceptions;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.QuestionDTOs;
using QuizMaster.Domain.Models.DTOs.QuizDTOs;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Domain.Models.Enums;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace QuizMaster.Infrastructure.Ai
{
	/// <summary>
	/// Writes quizzes with OpenAI's Chat Completions API. A strict JSON schema (structured outputs) makes the model
	/// answer in exactly the shape the app reads. The AI can still make mistakes, so QuizGenerationService checks every
	/// question with the domain's rules, and the quiz is saved as a draft for the lecturer to review.
	/// </summary>
	public class OpenAiQuizGenerator : IQuizGenerator
	{
		public const string NotConfiguredMessage =
			"AI quiz generation isn't set up. Add an OpenAI API key (OpenAI:ApiKey) to appsettings.Local.json.";

		private const string UnreadableMessage = "The AI's answer couldn't be read. Please try again.";

		private const string SystemPrompt =
			"You write quizzes for QuizMaster, a quiz platform used at a university. Write in English. "
			+ "Every question must be clear, have one reading only, and be factually correct: check each correct answer twice. "
			+ "Every option must be a sensible answer to the question as it is asked. "
			+ "Never give the answer away in the question text. Never use options such as \"All of the above\" or \"None of the above\".";

		private static readonly JsonNode QuizSchema = JsonNode.Parse("""
			{
				"type": "object",
				"properties": {
					"title": { "type": "string" },
					"topic": { "type": "string" },
					"questions": {
						"type": "array",
						"items": {
							"type": "object",
							"properties": {
								"type": { "type": "string", "enum": ["single", "multi", "truefalse"] },
								"text": { "type": "string" },
								"points": { "type": "integer" },
								"options": { "type": "array", "items": { "type": "string" } },
								"correctOptionIndexes": { "type": "array", "items": { "type": "integer" } }
							},
							"required": ["type", "text", "points", "options", "correctOptionIndexes"],
							"additionalProperties": false
						}
					}
				},
				"required": ["title", "topic", "questions"],
				"additionalProperties": false
			}
			""")!;

		private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

		private readonly OpenAiSettings _settings;
		private readonly HttpClient _httpClient;

		public OpenAiQuizGenerator(OpenAiSettings settings) : this(settings, new HttpClient())
		{
		}

		/// <summary>
		/// Tests pass an HttpClient with a fake handler, so no real request is sent.
		/// </summary>
		public OpenAiQuizGenerator(OpenAiSettings settings, HttpClient httpClient)
		{
			_settings = settings;
			_httpClient = httpClient;
			_httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(10, settings.TimeoutSeconds));
		}

		public bool IsConfigured => _settings.IsConfigured;

		public GeneratedQuizDTO Generate(string subject, int questionCount)
		{
			if (!IsConfigured)
			{
				throw new QuizGenerationException(NotConfiguredMessage);
			}

			using var request = new HttpRequestMessage(HttpMethod.Post, _settings.BaseUrl.TrimEnd('/') + "/chat/completions");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
			request.Content = new StringContent(BuildRequestBody(subject, questionCount), Encoding.UTF8, "application/json");

			int status;
			string responseText;
			try
			{
				using var response = _httpClient.Send(request);
				status = (int)response.StatusCode;
				using var reader = new StreamReader(response.Content.ReadAsStream());
				responseText = reader.ReadToEnd();
			}
			catch (OperationCanceledException ex)
			{
				throw new QuizGenerationException("OpenAI took too long to answer. Try again, or ask for fewer questions.", ex);
			}
			catch (HttpRequestException ex)
			{
				throw new QuizGenerationException($"OpenAI couldn't be reached: {ex.Message}", ex);
			}

			if (status is < 200 or > 299)
			{
				throw new QuizGenerationException(DescribeError(status, responseText));
			}
			return ReadQuiz(responseText);
		}

		private string BuildRequestBody(string subject, int questionCount)
		{
			var body = new JsonObject
			{
				["model"] = _settings.Model,
				["messages"] = new JsonArray(
					new JsonObject { ["role"] = "system", ["content"] = SystemPrompt },
					new JsonObject { ["role"] = "user", ["content"] = BuildUserPrompt(subject, questionCount) }),
				["response_format"] = new JsonObject
				{
					["type"] = "json_schema",
					["json_schema"] = new JsonObject
					{
						["name"] = "quiz",
						["strict"] = true,
						["schema"] = QuizSchema.DeepClone()
					}
				}
			};
			return body.ToJsonString();
		}

		private static string BuildUserPrompt(string subject, int questionCount)
		{
			return $"""
				Write a quiz about: "{subject}"

				Rules:
				- Exactly {questionCount} questions. Mix the three types "single", "multi" and "truefalse".
				- "single": 3 to 5 options, exactly one of them correct.
				- "multi": 4 to 6 options, at least two correct and at least one wrong.
				- "truefalse": the text is a statement, options are exactly ["True", "False"], and correctOptionIndexes is [0] when the statement is true or [1] when it is false.
				- correctOptionIndexes are 0-based positions in the options list.
				- The options of one question must all be different.
				- points: 1 for "truefalse", 2 for "single", 3 for "multi".
				- title: a short quiz title, at most 80 characters.
				- topic: the subject area in at most 40 characters, for example "Biology" or "C# programming".
				- Each question text at most 300 characters, each option at most 120 characters.
				""";
		}

		private static GeneratedQuizDTO ReadQuiz(string responseText)
		{
			ChatMessage? message;
			string? finishReason;
			AiQuiz? quiz;
			try
			{
				var completion = JsonSerializer.Deserialize<ChatCompletion>(responseText, JsonOptions);
				var choice = completion?.Choices?.FirstOrDefault();
				message = choice?.Message;
				finishReason = choice?.FinishReason;

				if (!string.IsNullOrWhiteSpace(message?.Refusal))
				{
					throw new QuizGenerationException($"The AI didn't write this quiz: {message.Refusal}");
				}
				if (finishReason == "length")
				{
					throw new QuizGenerationException("The AI's answer was cut off because it got too long. Try fewer questions.");
				}
				if (string.IsNullOrWhiteSpace(message?.Content))
				{
					throw new QuizGenerationException(UnreadableMessage);
				}
				quiz = JsonSerializer.Deserialize<AiQuiz>(message.Content, JsonOptions);
			}
			catch (JsonException ex)
			{
				throw new QuizGenerationException(UnreadableMessage, ex);
			}

			if (quiz?.Questions == null)
			{
				throw new QuizGenerationException(UnreadableMessage);
			}

			return new GeneratedQuizDTO
			{
				Title = quiz.Title?.Trim() ?? string.Empty,
				Topic = quiz.Topic?.Trim() ?? string.Empty,
				Questions = quiz.Questions
					.Select(ToQuestion)
					.OfType<CreateQuestionDTO>()
					.ToList()
			};
		}

		private static CreateQuestionDTO? ToQuestion(AiQuestion? question)
		{
			QuestionType? type = question?.Type?.Trim().ToLowerInvariant() switch
			{
				"single" => QuestionType.Single,
				"multi" => QuestionType.Multi,
				"truefalse" => QuestionType.TrueFalse,
				_ => null
			};
			if (question == null || type == null)
			{
				return null;
			}

			var options = question.Options ?? new List<string>();
			var correct = question.CorrectOptionIndexes ?? new List<int>();
			return new CreateQuestionDTO
			{
				Type = type.Value,
				Text = question.Text ?? string.Empty,
				Points = question.Points,
				// True/False questions get their fixed options from the domain, so only the answer is passed on.
				Options = type == QuestionType.TrueFalse ? new List<string>() : options,
				CorrectOptionIndexes = type == QuestionType.TrueFalse ? TrueFalseAnswer(options, correct) : correct
			};
		}

		/// <summary>
		/// Reads a True/False answer from the text of the chosen option, so it is right even if the AI listed
		/// the options in another order. Anything unexpected is passed on as it is, and the domain rejects it.
		/// </summary>
		private static List<int> TrueFalseAnswer(List<string> options, List<int> correct)
		{
			if (correct.Count == 1 && correct[0] >= 0 && correct[0] < options.Count)
			{
				var chosen = options[correct[0]]?.Trim();
				if (string.Equals(chosen, "True", StringComparison.OrdinalIgnoreCase))
				{
					return new List<int> { TrueFalseQuestion.TrueIndex };
				}
				if (string.Equals(chosen, "False", StringComparison.OrdinalIgnoreCase))
				{
					return new List<int> { TrueFalseQuestion.FalseIndex };
				}
			}
			return correct;
		}

		private string DescribeError(int status, string responseText)
		{
			var (code, message) = ReadError(responseText);
			return status switch
			{
				401 => "OpenAI rejected the API key. Check OpenAI:ApiKey in appsettings.Local.json.",
				403 => $"This OpenAI API key isn't allowed to use the model '{_settings.Model}'.",
				404 => $"OpenAI doesn't know the model '{_settings.Model}'. Check OpenAI:Model in appsettings.json.",
				429 when code == "insufficient_quota" => "The OpenAI account has no credit left. Add credit in the OpenAI dashboard, then try again.",
				429 => "OpenAI's rate limit was reached. Wait a moment, then try again.",
				>= 500 => $"OpenAI has a problem right now (HTTP {status}). Try again later.",
				_ => $"OpenAI refused the request (HTTP {status}): {message ?? "no details"}"
			};
		}

		private static (string? Code, string? Message) ReadError(string responseText)
		{
			try
			{
				var error = JsonSerializer.Deserialize<ErrorResponse>(responseText, JsonOptions)?.Error;
				var message = error?.Message;
				if (message != null && message.Length > 300)
				{
					message = message[..297] + "...";
				}
				return (error?.Code, message);
			}
			catch (JsonException)
			{
				return (null, null);
			}
		}

		// The parts of OpenAI's JSON that the app reads.

		private sealed class ChatCompletion
		{
			public List<ChatChoice>? Choices { get; set; }
		}

		private sealed class ChatChoice
		{
			public ChatMessage? Message { get; set; }

			[JsonPropertyName("finish_reason")]
			public string? FinishReason { get; set; }
		}

		private sealed class ChatMessage
		{
			public string? Content { get; set; }
			public string? Refusal { get; set; }
		}

		private sealed class AiQuiz
		{
			public string? Title { get; set; }
			public string? Topic { get; set; }
			public List<AiQuestion?>? Questions { get; set; }
		}

		private sealed class AiQuestion
		{
			public string? Type { get; set; }
			public string? Text { get; set; }
			public int Points { get; set; }
			public List<string>? Options { get; set; }
			public List<int>? CorrectOptionIndexes { get; set; }
		}

		private sealed class ErrorResponse
		{
			public ErrorBody? Error { get; set; }
		}

		private sealed class ErrorBody
		{
			public string? Message { get; set; }
			public string? Code { get; set; }
		}
	}
}

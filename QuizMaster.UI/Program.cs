using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuizMaster.Application.Services.Implementations;
using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Infrastructure.Ai;
using QuizMaster.Infrastructure.Data;
using QuizMaster.Infrastructure.Email;
using QuizMaster.Infrastructure.Logging;
using QuizMaster.Infrastructure.Repositories;
using QuizMaster.UI.Helpers;
using System.Globalization;
using System.Text;

namespace QuizMaster.UI
{
	internal class Program
	{
		static int Main(string[] args)
		{
			// EF Core design-time tools (dotnet ef / Add-Migration) can run Main. Do nothing in that case.
			if (EF.IsDesignTime)
			{
				return 0;
			}

			Console.OutputEncoding = Encoding.UTF8;
			CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

			// appsettings.json, then the optional appsettings.Local.json (for secrets, not committed),
			// then environment variables such as QUIZMASTER_Smtp__Password.
			var configuration = new ConfigurationBuilder()
				.SetBasePath(AppContext.BaseDirectory)
				.AddJsonFile("appsettings.json", optional: false)
				.AddJsonFile("appsettings.Local.json", optional: true)
				.AddEnvironmentVariables(prefix: "QUIZMASTER_")
				.Build();

			var connectionString = configuration.GetConnectionString("QuizMasterDb");
			if (string.IsNullOrWhiteSpace(connectionString))
			{
				connectionString = QuizMasterDbContext.DefaultConnectionString;
			}
			var emailService = configuration.GetSection("Smtp").Get<EmailService>() ?? new EmailService();
			var openAiSettings = configuration.GetSection("OpenAI").Get<OpenAiSettings>() ?? new OpenAiSettings();

			using var provider = BuildServices(connectionString, emailService, openAiSettings);
			if (!InitializeDatabase(provider))
			{
				return 1;
			}

			try
			{
				new QuizMasterSystem(provider, emailService.IsConfigured).Run();
			}
			catch (EndOfInputException)
			{
				Console.WriteLine();
				Console.WriteLine("Input ended. Goodbye!");
			}
			return 0;
		}

		private static ServiceProvider BuildServices(string connectionString, EmailService emailService, OpenAiSettings openAiSettings)
		{
			var services = new ServiceCollection();
			services.AddLogging();

			// The factory lets the logger use its own short-lived context; everything else gets one context per scope.
			services.AddDbContextFactory<QuizMasterDbContext>(options =>
				options.UseSqlServer(connectionString,
					sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
			services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<QuizMasterDbContext>>().CreateDbContext());

			services.AddScoped<IUnitOfWork, UnitOfWork>();
			services.AddScoped<IUserRepository, UserRepository>();
			services.AddScoped<IQuizRepository, QuizRepository>();
			services.AddScoped<IQuestionRepository, QuestionRepository>();
			services.AddScoped<IAttemptRepository, AttemptRepository>();

			services.AddSingleton<IAppLogger, DbLogger>();
			// Without an SMTP host the email service prints result emails to the console.
			services.AddSingleton<IEmailService>(emailService);

			// Without an API key the generator reports that AI quiz generation isn't set up.
			services.AddSingleton(openAiSettings);
			services.AddSingleton<IQuizGenerator>(_ => new OpenAiQuizGenerator(openAiSettings));

			services.AddScoped<IUserService, UserService>();
			services.AddScoped<IQuizService, QuizService>();
			services.AddScoped<IQuestionService, QuestionService>();
			services.AddScoped<IAttemptService, AttemptService>();
			services.AddScoped<IStatisticsService, StatisticsService>();
			services.AddScoped<ILogService, LogService>();
			services.AddScoped<IQuizGenerationService, QuizGenerationService>();

			services.AddAutoMapper(cfg => cfg.AddMaps(typeof(UserService).Assembly));

			return services.BuildServiceProvider();
		}

		private static bool InitializeDatabase(IServiceProvider provider)
		{
			try
			{
				Console.WriteLine("Connecting to the database...");
				using var scope = provider.CreateScope();
				var context = scope.ServiceProvider.GetRequiredService<QuizMasterDbContext>();
				context.Database.Migrate();
				return true;
			}
			catch (Exception ex)
			{
				ConsoleHelper.Error("Could not open the database. Check the connection string in appsettings.json.");
				ConsoleHelper.Error($"Details: {ex.GetBaseException().Message}");
				return false;
			}
		}
	}
}

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using QuizMaster.Application.Services.Implementations;
using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Infrastructure.Ai;
using QuizMaster.Infrastructure.Data;
using QuizMaster.Infrastructure.Email;
using QuizMaster.Infrastructure.Logging;
using QuizMaster.Infrastructure.Repositories;
using QuizMaster.Web.Helpers;
using System.Globalization;

namespace QuizMaster.Web
{
	public class Program
	{
		public static int Main(string[] args)
		{
			// EF Core design-time tools (dotnet ef / Add-Migration) can run Main. Do nothing in that case.
			// Migrations use QuizMaster.UI as the startup project.
			if (EF.IsDesignTime)
			{
				return 0;
			}

			// The same culture as the console app, so numbers and dates look the same on any machine.
			CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
			CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

			var builder = WebApplication.CreateBuilder(args);

			// The same sources as the console app: appsettings.json (the builder loads it), then the optional
			// appsettings.Local.json (for secrets, not committed), then environment variables such as QUIZMASTER_Smtp__Password.
			builder.Configuration
				.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
				.AddEnvironmentVariables(prefix: "QUIZMASTER_");

			var connectionString = builder.Configuration.GetConnectionString("QuizMasterDb");
			if (string.IsNullOrWhiteSpace(connectionString))
			{
				connectionString = QuizMasterDbContext.DefaultConnectionString;
			}
			var smtpSettings = builder.Configuration.GetSection("Smtp").Get<SmtpSettings>() ?? new SmtpSettings();
			var openAiSettings = builder.Configuration.GetSection("OpenAI").Get<OpenAiSettings>() ?? new OpenAiSettings();

			AddQuizMasterServices(builder.Services, connectionString, smtpSettings, openAiSettings);
			AddWebServices(builder.Services);

			var app = builder.Build();

			// Like the console app, so either app can be started first.
			if (!InitializeDatabase(app.Services))
			{
				return 1;
			}
			if (!smtpSettings.IsConfigured)
			{
				Console.WriteLine("SMTP is not configured, so result emails are printed in this console window.");
			}

			// Configure the HTTP request pipeline.
			if (!app.Environment.IsDevelopment())
			{
				app.UseExceptionHandler("/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseRequestLocalization(InvariantCultureOnly());

			app.UseRouting();

			app.UseAuthentication();
			app.UseAuthorization();

			app.MapStaticAssets();
			app.MapRazorPages()
				.WithStaticAssets();

			app.Run();
			return 0;
		}

		/// <summary>
		/// The same registrations as BuildServices in QuizMaster.UI/Program.cs.
		/// </summary>
		private static void AddQuizMasterServices(IServiceCollection services, string connectionString, SmtpSettings smtpSettings,
			OpenAiSettings openAiSettings)
		{
			// The factory lets the logger use its own short-lived context; everything else gets one context per request.
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
			services.AddSingleton(smtpSettings);
			if (smtpSettings.IsConfigured)
			{
				services.AddSingleton<IEmailService, SmtpEmailService>();
			}
			else
			{
				services.AddSingleton<IEmailService, ConsoleEmailService>();
			}

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
		}

		private static void AddWebServices(IServiceCollection services)
		{
			// Pages under /Lecturer and /Student need that role, Leaderboards needs any logged-in user, and the rest is open.
			// The services still check roles and ownership themselves (AccessGuard); this is an extra layer.
			services.AddRazorPages(options =>
			{
				options.Conventions.AuthorizeFolder("/Lecturer", UserClaims.LecturerPolicy);
				options.Conventions.AuthorizeFolder("/Student", UserClaims.StudentPolicy);
				options.Conventions.AuthorizePage("/Leaderboards");
			});

			// Cookie login on top of IUserService: users, BCrypt hashes and roles stay in the Users table.
			services.AddScoped<UserCookieValidator>();
			services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
				.AddCookie(options =>
				{
					options.Cookie.Name = "QuizMaster.Auth";
					options.LoginPath = "/Account/Login";
					options.AccessDeniedPath = "/Account/AccessDenied";
					options.ExpireTimeSpan = TimeSpan.FromHours(8);
					options.SlidingExpiration = true;
					options.EventsType = typeof(UserCookieValidator);
				});
			services.AddAuthorization(options =>
			{
				options.AddPolicy(UserClaims.LecturerPolicy, policy => policy.RequireRole(UserClaims.LecturerRole));
				options.AddPolicy(UserClaims.StudentPolicy, policy => policy.RequireRole(UserClaims.StudentRole));
			});
		}

		private static bool InitializeDatabase(IServiceProvider provider)
		{
			try
			{
				Console.WriteLine("Connecting to the database...");
				using var scope = provider.CreateScope();
				var context = scope.ServiceProvider.GetRequiredService<QuizMasterDbContext>();
				context.Database.Migrate();
				DbSeeder.Seed(context);
				return true;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("Could not open the database. Check the connection string in appsettings.json.");
				Console.Error.WriteLine($"Details: {ex.GetBaseException().Message}");
				return false;
			}
		}

		/// <summary>
		/// The invariant culture for every request. No query string, cookie or browser header can switch to another culture.
		/// </summary>
		private static RequestLocalizationOptions InvariantCultureOnly()
		{
			var cultures = new List<CultureInfo> { CultureInfo.InvariantCulture };
			var options = new RequestLocalizationOptions
			{
				DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture),
				SupportedCultures = cultures,
				SupportedUICultures = cultures
			};
			options.RequestCultureProviders.Clear();
			return options;
		}
	}
}

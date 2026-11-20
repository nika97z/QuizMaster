# QuizMaster: notes for Claude

University OOP project (course SE515, assignment 6): a console quiz platform. Lecturers create quizzes with Single, Multi and TrueFalse questions; students take them and get a score, Passed/Failed, a result email and (on a pass) a certificate code. There are LINQ leaderboards and statistics and an activity log. A Razor Pages web app (`QuizMaster.Web`) offers the same features on the same database; the console app stays because the assignment requires it. In both apps a lecturer can let AI (OpenAI) write a draft quiz on a chosen or random subject.

- Full spec and design decisions: `../QuizMaster-Prompt.md` (English translation of the assignment; the original Georgian text is at the end). It is out of date on five points, and this file wins: passwords use BCrypt (no PBKDF2, no salt column), there is no lecturer registration code, it says there is no web project, there is no seeder or seed data, and email is one `EmailService` class (not `SmtpEmailService` + `ConsoleEmailService` + `SmtpSettings`).
- Spec of the web app: `../QuizMaster-Web-Prompt.md`. It still mentions `DbSeeder`, the seed logins and the old email classes; this file wins.
- User-facing docs: `README.md` (setup, web app, SMTP, migrations, requirement-to-class table).

## Environment

- Windows, Visual Studio 2026 (18.10), .NET 10 and .NET 11 SDKs.
- SQL Server Express at `.\SQLEXPRESS` (LocalDB is also installed).
- The user opens solutions in Visual Studio: `QuizMaster.slnx` here, and `../FitZone/FitZone.slnx` for the reference project (never `SE515.slnx`, which lists 52 course projects that aren't downloaded).

## Architecture (copied from FitZone)

- Reference project: `../FitZone`, a sparse git clone of github.com/LanaSteen/SE515 with only the FitZone projects. Don't modify it.
- Projects:
  - `QuizMaster.Domain`: entities with behaviour, enums, DTOs in `Models/DTOs/<X>DTOs`, repository and service interfaces, `QuestionFactory`, domain exceptions.
  - `QuizMaster.Application`: services (`Services/Interfaces` + `Implementations`), AutoMapper `Profiles`, exceptions, `AccessGuard` (role checks).
  - `QuizMaster.Infrastructure`: `QuizMasterDbContext`, EF configurations, repositories, `UnitOfWork`, `DbLogger`, `Email/EmailService`, `Ai/` (OpenAI quiz generator), `Migrations`.
  - `QuizMaster.UI`: console app (`Program.cs`, `QuizMasterSystem`, `Menus/`, `Helpers/`).
  - `QuizMaster.Web`: Razor Pages app (`Program.cs`, `Pages/QuizMasterPageModel.cs`, `Pages/Account`, `Pages/Student`, `Pages/Lecturer`, `Pages/Leaderboards`, `Helpers/`). It references Domain, Application and Infrastructure, never `QuizMaster.UI`.
  - `QuizMaster.Tests`: xunit.v3 + Moq service tests, EF Core InMemory repository tests, domain tests.
- Conventions: block-scoped namespaces, tabs, `_camelCase` private fields, constructor injection, synchronous methods (web page handlers too, except login and logout, which await `SignInAsync` / `SignOutAsync`).
- Repositories never call `SaveChanges()`; services call `_unitOfWork.SaveChanges()`.
- Every service implements its interface and is registered in both `QuizMaster.UI/Program.cs` and `QuizMaster.Web/Program.cs` (the same registrations; keep them in sync). DTOs never contain entities.
- Use the latest stable .NET and NuGet versions (the user asked for this explicitly), not FitZone's versions. Currently `net11.0`, EF Core 11, AutoMapper 17, xunit.v3 4.

## Design decisions to keep

- TPH inheritance: `User` → `Lecturer`/`Student` (discriminator column `Role`) and `Question` → `SingleChoiceQuestion`/`MultiChoiceQuestion`/`TrueFalseQuestion` (column `Type`).
- Correct answers are private fields (`_correctOptionIndex`, `_correctOptionIndexes`, `_correctValue`) mapped by EF field mapping. There is no getter, they are never in a DTO, and only `CheckAnswer()` reads them. Lecturers re-enter the answer when editing a question.
- `Attempt` state has private setters; only `Start()`, `SubmitAnswer()`, `Finish()` and `Cancel()` change it.
- Answers store original option positions, so the shuffled display order (bonus) never affects grading.
- `DbLogger` saves each entry at once in its own context from `IDbContextFactory`, so logs survive failed operations.
- The UI runs each action in its own DI scope (`ServiceRunner`) wrapped in `SafeExecutor` (try/catch plus logging).
- Email is one class, `Email/EmailService` (the user asked for it to be simple, 2026-11-20; it replaced `SmtpEmailService`, `ConsoleEmailService` and `SmtpSettings`). Its properties are bound from the `Smtp` section (`Get<EmailService>()`), and both `Program.cs` files register that instance as `IEmailService`. With `Smtp:Host` set it sends through SMTP; without it, it prints the email to the console (for the web app, its console window). Secrets go in `QuizMaster.UI/appsettings.Local.json` or `QuizMaster.Web/appsettings.Local.json` (git-ignored) or `QUIZMASTER_` environment variables.
- Passwords: `BCrypt.Net.BCrypt.HashPassword` / `Verify`, called directly in `UserService` (the user asked to drop the custom hasher and "just hash with BCrypt", so there is no hasher class or interface). BCrypt keeps the salt inside the hash, so `Users` has no salt column. `BCrypt.Net-Next` is referenced by Application only (the tests get it through Application).
- Anyone can register as a student or a lecturer. There is no lecturer code (the user removed it on 2026-11-18).
- No seed data: the user removed `DbSeeder` on 2026-11-20. A new database starts empty; register a lecturer and a student in either app. Existing databases keep their rows, so `QuizMasterDb` still has the old sample users (`lela_lecturer` / `securePass`, `luka_student` / `pass123`) and quizzes ("C# OOP Basics" with luka's 16/20 attempt, "SQL Fundamentals").
- Lecturers can delete student accounts (the user asked, 2026-11-20); any lecturer can delete any student. `UserService.DeleteStudent` uses `UserRepository.GetStudentWithAttempts` (finds only students, so a lecturer can't be deleted) and `DeleteStudent`, which removes the attempts first: `Attempts.StudentId` stays Restrict as a safety net. Answers go with their attempts (cascade); log rows stay (no FKs). Logged as `StudentDeleted` (appended to `LogEventType`; stored as text, so no migration). Console: lecturer menu option 8 (last, so older piped inputs still work). Web: `Lecturer/Students` (navbar "Students"), Delete buttons with `data-confirm`. `UserCookieValidator` signs out a deleted student's browser.

## Web app decisions (`QuizMaster.Web`, added 2026-11-19)

- Cookie authentication on top of `IUserService`: no ASP.NET Core Identity, no second user table. Claims: user id (`NameIdentifier`), username, role (`Helpers/UserClaims`). `QuizMasterPageModel.CurrentUserId` reads the acting user's id from the cookie only, never from a form field, query string or route.
- `Helpers/UserCookieValidator` checks the cookie against `Users` on every request (same id, username and role, otherwise it signs the browser out). Cookie keys persist in the Windows profile, so a cookie outlives a switch to another database, where its id may belong to someone else.
- Authorization: folder conventions in `Program.cs` (`/Lecturer` and `/Student` need that role, `/Leaderboards` any logged-in user). This is an extra layer; the services still check roles and ownership through `AccessGuard`.
- Errors: `QuizMasterPageModel.Execute` / `HandleError` is the web `SafeExecutor` (same exception list, same logging). Errors go to ModelState; messages that must survive a redirect go through TempData (`Helpers/FlashMessages`). `Pages/Shared/_StatusMessages.cshtml`, in the layout, shows both.
- Every change is a POST followed by a redirect (Post/Redirect/Get); GET handlers only read.
- Attempt flow: a GET never starts an attempt, because `StartAttempt` cancels an expired one; it runs only from the catalog's Start button. `Student/Take` reads the attempt on every request with `AttemptService.GetAttemptSession` (read-only, logs nothing). An answer is accepted only for the question the page should show now: `Attempt.SubmitAnswer` replaces an existing answer, so a stale page (Back) would otherwise overwrite it. `TimeLimitExceededException` finishes the attempt; an attempt that already ended redirects to its result (or to My attempts if cancelled).
- The countdown gets the seconds left from the server (`Deadline - DateTime.Now`) and submits the Finish form at zero. The browser never parses the deadline, and the server enforces the limit.
- `FinishAttempt` reports `EmailSent`, `GetAttemptResult` doesn't, so the result page gets it once through TempData (like the console's `justFinished`).
- The domain doesn't check text lengths or the points maximum (the console's input prompts do), so the web forms check them on the server (`Helpers/InputLimits`: title 200, topic 100, question 1000, option 300, points 1-100). Without this, an over-long value would fail in SQL Server.
- The question form has 10 option rows. Blank rows are dropped before `CorrectOptionIndexes` are computed, and a correct mark on a blank row is an error.
- The leaderboard's quiz picker accepts only a quiz from the user's list (published quizzes for students, own quizzes for lecturers), like the console, because `GetQuizLeaderboard` has no access check.
- Answer options are reshuffled on every page load (`ToShuffledDto` uses `Random.Shared`). Grading is unaffected; left as is on purpose.
- `appsettings.json` logs EF Core at Warning only, so the result emails printed by `EmailService` stay readable in the web app's console.

## AI quiz generation (added 2026-11-19)

- The user asked for it: when creating a quiz, the AI can write it, on a subject the lecturer types or a random one. Web: a second form ("Let AI create it") on `Lecturer/CreateQuiz`. Console: lecturer menu option 7, placed last so the other menu numbers and old piped-input files still work.
- Layers: `IQuizGenerator` (Domain) → `OpenAiQuizGenerator` (Infrastructure/Ai) → `QuizGenerationService` (Application). Registered in both `Program.cs` files.
- `OpenAiQuizGenerator` calls OpenAI's Chat Completions API with plain `HttpClient` and System.Text.Json (no SDK package), using a strict JSON schema (structured outputs). Every failure becomes a `QuizGenerationException` (Domain) with a message for the user (wrong key, no credit, rate limit, unknown model, timeout, cut-off or unreadable answer); `HandleError` and `SafeExecutor` show it and log it. True/False answers are read from the option text, in case the AI orders the options differently.
- `QuizGenerationService` checks the request before calling the AI (subject, 1-10 questions, the usual time limit and pass percent), so bad input costs nothing. A random subject comes from `RandomSubjects`. Every AI question goes through `QuestionFactory` like a lecturer's own; questions that fail, or don't fit the column sizes, are skipped and counted. The quiz is saved as a draft, and `QuizGenerated` is logged. The lecturer must review it: in testing the AI marked a true statement as wrong (static constructors in interfaces).
- Column sizes are constants now: `Quiz.MaxTitleLength` (200), `Quiz.MaxTopicLength` (100), `Question.MaxTextLength` (1000), `QuestionOption.MaxTextLength` (300). The web's `InputLimits` uses them. The EF configurations still have the same literal numbers.
- Settings: section `OpenAI` (`ApiKey`, `Model`, optional `BaseUrl`, `TimeoutSeconds`). Model `gpt-5.4-mini`, chosen after a real test (about 4 seconds for 5 questions). The API key is in `QuizMaster.Web/appsettings.Local.json` and `QuizMaster.UI/appsettings.Local.json` (git-ignored), or `QUIZMASTER_OpenAI__ApiKey`. Never put it in `appsettings.json`, code or docs. Without a key, both apps say AI generation isn't set up.
- The web's CreateQuiz binds its two forms as handler parameters (`OnPost(QuizInput input)`, `OnPostGenerate(AiQuizInput ai)`), not `[BindProperty]`, so only the submitted form is validated. Forms with `data-busy-text` disable their buttons while sending (site.js), so a slow AI request can't be sent twice.
- Each generation is a billed OpenAI request. Tests use a fake HTTP handler; real calls happen only in manual smoke tests.

## Temporary simplifications (the user will restore them later)

- Email format checking is off: `UserService.Register` only checks that the email isn't empty or longer than 256 characters. `IsValidEmail` is kept but unused, and a comment in `Register` shows the one line to restore. Without SMTP, `EmailService` prints the email for any address text; with SMTP on, an address that isn't valid email text can't be sent (`EmailFailed` is logged, the attempt still finishes). The web register form uses a plain text input (no `type="email"`, no `[EmailAddress]`); change it too when the check comes back.
- The test `Register_AnyEmailText_IsAcceptedForNow` documents this. Change it when the check comes back.
- Registration asks for the password once (no "repeat password"), in both apps.
- Email uniqueness is still enforced (assignment requirement and unique DB index).

## Commands (run from this folder)

- Build: `dotnet build QuizMaster.slnx`
- Tests: `dotnet test --project QuizMaster.Tests/QuizMaster.Tests.csproj`. `global.json` switches `dotnet test` to the Microsoft Testing Platform; without it, xunit.v3 4 fails on the .NET 10+ SDK.
- Run: `dotnet run --project QuizMaster.UI`. It creates and migrates `QuizMasterDb` on `.\SQLEXPRESS` (no seed data).
- Run the web app: `dotnet run --project QuizMaster.Web`, then http://localhost:5209 (`http` profile in `Properties/launchSettings.json`). It also migrates, so either app can start first, and both can run at once.
- Migrations: `dotnet tool restore`, then `dotnet ef migrations add <Name> --project QuizMaster.Infrastructure --startup-project QuizMaster.UI`. `dotnet-ef` is a local tool (`dotnet-tools.json`), not global. Rebuild before running the exe after adding a migration, or EF reports that no migrations exist. Keep `QuizMaster.UI` as the startup project for migrations.
- `Program.Main` returns at once when `EF.IsDesignTime` is true, because the EF tools run `Main` (both apps).
- Smoke test without touching the real database: set `QUIZMASTER_ConnectionStrings__QuizMasterDb` to a throwaway database such as `QuizMasterDb_SmokeTest`, pipe a file of answers into `QuizMaster.UI/bin/Debug/net11.0/QuizMaster.UI.exe` (piped input is echoed, and the app exits cleanly when input ends), then drop that database. A throwaway database starts empty, so the input must register the users first.
- Both apps now send real email through Gmail (SMTP settings in the `appsettings.Local.json` files). In smoke tests, also set `QUIZMASTER_Smtp__Host` to a single space, so result emails are printed instead of sent (`IsConfigured` treats whitespace as empty; checked 2026-11-20). Send real email only when the user asks.
- Web smoke test: with the same variable set (for example `QuizMasterDb_WebSmokeTest`), `dotnet run --project QuizMaster.Web --launch-profile http`, drive it with curl and a cookie jar (read `__RequestVerificationToken` from each form; posts without it get 400), stop the `QuizMaster.Web.exe` process, then drop the database. Foreground `sleep` is blocked in this environment; wait for a quiz deadline with a background command.
- If the user is running the web app from Visual Studio, it locks `QuizMaster.Web/bin` (the build warns MSB3026) and uses ports 5209 and 7081. Never stop it. Build into a scratch folder (`dotnet build QuizMaster.Web/QuizMaster.Web.csproj -o <scratch>/web-build`) and run `dotnet <scratch>/web-build/QuizMaster.Web.dll --urls http://localhost:5299` from the `QuizMaster.Web` folder with `ASPNETCORE_ENVIRONMENT=Development`; stop only the process on port 5299.

## Status

- Done: every requirement in the assignment, including the bonus. 120 tests pass. An end-to-end run on SQL Express worked, including after a restart.
- AI quiz generation done (2026-11-19) and checked with real OpenAI calls on a throwaway database: a chosen subject and a random subject in the web app, a chosen subject in the console, publishing an AI quiz, a wrong key (the message is shown and logged), and no key (the console says it isn't set up).
- Web app done (2026-11-19) and checked end-to-end on a throwaway database: registration and login, a full quiz with a certificate, reload, resume, quit, empty and stale answers, the 1-minute timeout (late answer and late Finish), the lecturer pages (create, publish errors, three question types, edit with the answer re-entered, publish, close, delete), statistics, logs, leaderboards, access denied, the return URL after login, the cookie check, a database outage, and a failing SMTP server (`smtp.invalid`: the attempt finishes and the result page says the email couldn't be sent). The console app worked on the same database alongside it.
- Simplified (2026-11-20): `DbSeeder` removed, the three email classes merged into `EmailService`, the unused `IndexList.Parse` removed. Checked: build (0 warnings), 113 tests, a console run on a throwaway database (register both roles, create and publish a quiz, take it, result email printed, `EmailSent` logged, no seed rows), and the web app on that database (start-up, login, My attempts).
- Real email works (2026-11-20): Gmail SMTP (`smtp.gmail.com:587` with SSL and an App Password from the user's Gmail account) is set in both `appsettings.Local.json` files. A console run on a throwaway database sent a real result email (Passed, with certificate) and logged `EmailSent`. The web app loads the same settings (checked at start-up; no email was sent from it).
- Deleting students done (2026-11-20) and checked on a throwaway database. Console: the list, a lecturer's id refused, a student with a finished attempt deleted (attempt and answers gone, the quiz, other users and old log rows kept, `StudentDeleted` logged), and the deleted student can't log in. Web (scratch build on port 5299): a student gets Access denied, delete with the success message, a lecturer's id gives "Student 1 was not found.", a post without the antiforgery token gets 400, and the deleted student's cookie is signed out.
- Not verified: the countdown's automatic Finish in a real browser (the script was tested in Node with a fake DOM, and the Finish request itself was tested).
- Open item: switch email format checking back on (in `UserService.Register` and the web register form).

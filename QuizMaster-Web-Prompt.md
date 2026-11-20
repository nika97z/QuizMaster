# Prompt: Add an ASP.NET Core web frontend to QuizMaster

You are adding a **web frontend** to the existing **QuizMaster** solution in `C:\Users\User\OneDrive\Desktop\C#\QuizMaster`. Paths below are relative to that folder. Create one new project, **`QuizMaster.Web`**: an ASP.NET Core **Razor Pages** app. In a browser, students and lecturers must be able to do everything the console app (`QuizMaster.UI`) lets them do. It reuses the existing Domain, Application and Infrastructure projects and the same SQL Server database (`QuizMasterDb` on `.\SQLEXPRESS`).

**Keep the console app.** The assignment requires a console app, so `QuizMaster.UI` stays and must work exactly as before. The web app is an addition. `../QuizMaster-Prompt.md` says "No Web project"; this prompt overrides that one point only.

Before writing any code, read:
1. `CLAUDE.md`: conventions, design decisions, commands and seed logins. Follow it.
2. `QuizMaster.UI/Program.cs` (configuration, DI, migrate and seed), `QuizMaster.UI/QuizMasterSystem.cs`, everything in `QuizMaster.UI/Menus/`, and `QuizMaster.UI/Helpers/SafeExecutor.cs`. The web app needs the same features and the same error handling.
3. The service interfaces in `QuizMaster.Application/Services/Interfaces` and the DTOs in `QuizMaster.Domain/Models/DTOs`.
4. The reference web project `../FitZone/Fitzone.Web` (Razor Pages). Its `Program.cs` registers the DbContext, repositories, services and AutoMapper. Page models such as `Pages/Member/Index.cshtml.cs` take services through the constructor and call them in `OnGet`. Copy that structure. Don't modify FitZone.

---

## 1. Project setup

- From the `QuizMaster` folder: `dotnet new webapp -n QuizMaster.Web -o QuizMaster.Web -f net11.0`, then `dotnet sln QuizMaster.slnx add QuizMaster.Web/QuizMaster.Web.csproj`.
- Reference `QuizMaster.Application`, `QuizMaster.Domain` and `QuizMaster.Infrastructure`, like `QuizMaster.UI` does.
- `net11.0` with `Nullable` and `ImplicitUsings` on. Same code style as the rest of the solution: block-scoped namespaces, tabs, `_camelCase` private fields, constructor injection.
- Page handlers are synchronous (`OnGet`, `OnPost…`) because the services are. The only exceptions are login and logout, which `await HttpContext.SignInAsync` / `SignOutAsync`.
- Use the name `QuizMaster.Web` everywhere, including namespaces. FitZone's `Fitzone.Web` has a casing typo; don't copy it.
- Delete template pages you don't use, such as `Privacy` and its navbar link. Keep `Error`.
- No new NuGet packages should be needed: cookie authentication is built into ASP.NET Core, and EF Core and AutoMapper come through the project references. If you do need one, use the latest stable version (see CLAUDE.md). AutoMapper 17 may log a license-key warning at startup; that's expected.
- **Don't add ASP.NET Core Identity**, a second user table, React/Angular or an npm build. Use the template's Bootstrap, plus a little plain JavaScript where this prompt mentions it (countdown, confirm dialogs).

## 2. `Program.cs`

- **Configuration**: the same sources as the console, in this order: `appsettings.json`, the optional `appsettings.Local.json` (secrets; the solution's `.gitignore` already ignores it), then environment variables with the `QUIZMASTER_` prefix. The web builder already loads `appsettings.json`; add the other two.
- **`appsettings.json`**: the same `ConnectionStrings:QuizMasterDb` and `Smtp` section as `QuizMaster.UI/appsettings.json`. If the connection string is empty, fall back to `QuizMasterDbContext.DefaultConnectionString`, like the console.
- **Services**: register exactly what `BuildServices` in `QuizMaster.UI/Program.cs` registers:
  - `AddDbContextFactory<QuizMasterDbContext>` with SQL Server and split queries, plus the scoped context created from the factory. `DbLogger` needs the factory, so don't replace it with plain `AddDbContext`.
  - `UnitOfWork`, the four repositories, `DbLogger` as the singleton `IAppLogger`, `SmtpSettings`, and `SmtpEmailService` or `ConsoleEmailService` depending on `SmtpSettings.IsConfigured`.
  - The six services, and AutoMapper with `cfg.AddMaps(typeof(UserService).Assembly)`.
  - Copying these registrations into the web `Program.cs` is fine. FitZone does the same in each UI project.
- **Database start-up**: before `app.Run()`, in a scope, call `Database.Migrate()` and `DbSeeder.Seed(context)` like the console does, so either app can be started first. If the database can't be opened, write a clear message to the console and stop.
- If SMTP isn't configured, write one line at start-up saying that result emails are printed in this console window (that's what `ConsoleEmailService` does).
- **Culture**: the console uses the invariant culture. Use it for requests too (for example `UseRequestLocalization` with the invariant culture as the only culture), so numbers look the same on any machine.
- **Pipeline**: keep the template's pipeline, add cookie authentication (section 3), and call `UseAuthentication()` before `UseAuthorization()`.
- Migrations keep using `QuizMaster.UI` as the startup project. No model change or new migration is needed.

## 3. Login, roles and the current user

- Use **cookie authentication** on top of `IUserService`. Users, BCrypt hashes and roles already live in the `Users` table.
- `Account/Login`: username and password → `IUserService.Login(LoginDTO)`. On success, sign in with the claims `ClaimTypes.NameIdentifier` (user id), `ClaimTypes.Name` (username) and `ClaimTypes.Role` (`"Lecturer"` or `"Student"`, from `UserRole`). Then go to `returnUrl` if it's a local URL, otherwise to the role's start page. On `InvalidCredentialsException`, show its message.
- `Account/Register`: username (max 30), email (max 256), password (at least 6 characters, asked once) and role (Student or Lecturer) → `IUserService.Register(RegisterUserDTO)`. Then go to Login with "Account created for X. You can log in now.", like the console.
  - **Email format checking is deliberately off for now** (CLAUDE.md, "Temporary simplifications"). Use a plain text input: no `type="email"` and no `[EmailAddress]`. There is no "repeat password" field and no lecturer code.
- `Account/Logout`: POST only (a form button in the navbar), then back to the home page.
- `Account/AccessDenied`: a simple page.
- **Authorization**: everything under `Pages/Lecturer` needs the Lecturer role and everything under `Pages/Student` needs the Student role (folder conventions with role policies, or `[Authorize(Roles = …)]`). `Leaderboards` needs any logged-in user. Home, Login, Register, AccessDenied and Error are open to everyone.
- **The current user id always comes from the auth cookie.** Put a `CurrentUserId` property on a base page model. Never read the acting user's id from a form field, query string or route.
- The services still check roles and ownership through `AccessGuard`. The web checks are an extra layer, not a replacement. Keep all business rules in Domain and Application: pages only collect input, call a service and show the result.

## 4. Pages

The web app must cover every console menu item. The tables list the service methods each page uses.

**Shared**

| Page | Content | Services |
|---|---|---|
| `Index` | Logged out: a short welcome with Login and Register. Logged in: redirect to the role's start page. | none |
| `Account/*` | See section 3. | `IUserService` |
| `Leaderboards` | The overall leaderboard (rank, student, total best score, average %, quizzes, attempts). Below it, a quiz picker that shows that quiz's leaderboard (rank, student, score/max, percent, time, finished). Students pick from published quizzes, lecturers from their own quizzes, as in the console. | `GetOverallLeaderboard`, `GetQuizLeaderboard`, `GetPublishedQuizzes` or `GetLecturerQuizzes` |

**Student** (start page: `Student/Quizzes`)

| Page | Content | Services |
|---|---|---|
| `Student/Quizzes` | Catalog of published quizzes: title, topic, questions, max score, time limit, pass %. A Start button per quiz (POST), with a confirm dialog that shows the question count, minutes and pass mark. | `GetPublishedQuizzes`, `StartAttempt` |
| `Student/Take/{attemptId}` | One question at a time. See section 5. | `GetAttemptSession` (new), `SubmitAnswer`, `FinishAttempt`, `CancelAttempt` |
| `Student/Result/{attemptId}` | Score/max, percent with the pass mark, correct and incorrect counts, PASSED or FAILED, the certificate code on a pass, "The time limit was reached." if `TimedOut`, and the review table (#, question, your answer, Correct/Incorrect, points). Right after finishing, also "Your result was emailed to you." or "The result email couldn't be sent. The problem was logged." Never show the correct options. | `GetAttemptResult` |
| `Student/Attempts` | My attempts: quiz, started, finished, status, score, percent, result. Finished attempts link to their result; in-progress ones link to `Take` to continue. | `GetStudentAttempts` |

**Lecturer** (start page: `Lecturer/Quizzes`)

| Page | Content | Services |
|---|---|---|
| `Lecturer/Quizzes` | My quizzes: title, topic, questions, max score, time limit, pass % and status (Published or Draft/closed), each with a Manage link, plus a "Create quiz" button. | `GetLecturerQuizzes` |
| `Lecturer/CreateQuiz` | Title (max 200), topic (max 100), time limit (1–300 minutes), pass percent (1–100). Then go to the new quiz's Manage page. | `CreateQuiz` |
| `Lecturer/Manage/{quizId}` | The quiz details and its questions (number, type, points, text, options in position order), with a note that correct answers are private, so they aren't shown. Buttons: Edit details, Add question, Edit and Remove for each question, Publish or Close, Delete quiz. Remove, Close and Delete ask for confirmation. While the quiz is published, the edit buttons are disabled with the console's hint to close it first (the domain also refuses those changes). If Publish fails, list every validation error. | `GetQuizDetails`, `PublishQuiz`, `CloseQuiz`, `DeleteQuiz`, `RemoveQuestion` |
| `Lecturer/EditQuiz/{quizId}` | The CreateQuiz fields, prefilled. | `GetQuizDetails`, `UpdateQuiz` |
| `Lecturer/Question` | Add or edit a question. See section 6. | `GetQuizDetails`, `AddQuestion`, `UpdateQuestion` |
| `Lecturer/Statistics` | Average score, average percent, best student (username, total best score, average %), a "Results by quiz" table (quiz, topic, attempts, avg score, avg %, pass rate, best student) and a "Results by topic" table (topic, quizzes, attempts, avg %, pass rate). | `IStatisticsService` |
| `Lecturer/Logs` | The 30 most recent log entries, newest first: time, event, message. | `GetRecentLogs(lecturerId, 30)` |

- A navbar with the role's links, "Signed in as X (Role)" and the Logout button.
- Render questions from `Text`, `Points` and `Options`. `DisplayText` is pre-formatted console text, so don't print it raw.
- Format percents, dates and durations like the console's `ConsoleHelper` (`FormatPercent`, `FormatDate`, `FormatDuration`). The web project must not reference `QuizMaster.UI`, so write small equivalents in `QuizMaster.Web/Helpers`.
- Every change is a POST followed by a redirect (Post/Redirect/Get), so Refresh and Back never repeat an action. GET handlers only read.

## 5. Taking a quiz

This part differs most from the console, which keeps the session in memory. In the web app each step is a separate request, so the attempt's state comes from the database every time. The `Attempt` already stores its question order, answers and deadline.

**Add one read-only service method.** `StartAttempt(studentId, quizId)` resumes an active attempt, but it cancels an expired one and starts a new attempt. So it may only run from the explicit Start button, never on a GET. Add this to `IAttemptService` and `AttemptService`:

```csharp
AttemptSessionDTO GetAttemptSession(int studentId, int attemptId);
```

It loads the attempt through the existing `GetOwnedAttempt`, throws `InvalidAttemptStateException` unless the status is `InProgress`, and returns `BuildSession(attempt, attempt.Quiz, resumed: false)`. It writes nothing and logs nothing. Add Moq tests next to the existing ones in `QuizMaster.Tests/Services/AttemptServiceTests.cs`:
- the student's own in-progress attempt returns only the unanswered questions;
- another student's attempt throws `UnauthorizedActionException`;
- a finished or cancelled attempt throws `InvalidAttemptStateException`.

**Flow:**
1. **Start** (POST on the catalog) → `StartAttempt`. If `Resumed` is true, show "You already had an unfinished attempt, so it continues (X of Y answered)." Redirect to `Student/Take/{attemptId}`.
2. **Take** (GET) → `GetAttemptSession`.
   - Deadline passed: show "Time is up!" and a button that POSTs Finish.
   - No questions left: show a button that POSTs Finish.
   - Otherwise, show the first question in `Questions`: "Question {AnsweredCount + 1} of {TotalQuestions}", its text and points, and its options in the order given (they're already shuffled). Use radio buttons for Single and TrueFalse and checkboxes for Multi. **Each input's value is the option's `Position`, not its place on the screen.** Also show a countdown and a Quit button.
3. **Answer** (POST) → accept it only if the posted question is the one the page should show now (the first in `Questions`). A stale page, such as one reached with Back, must not overwrite an earlier answer; show the current question with a short note instead. Then call `SubmitAnswer` with a `SubmitAnswerDTO` (`AttemptId`, `QuestionId`, `SelectedOptionPositions`).
   - Saved and no questions are left → `FinishAttempt`, then go to the result. Saved and more are left → redirect back to Take.
   - `DomainValidationException` (nothing selected, two choices on a Single question, …) → show the same question again with the errors. Nothing is saved.
   - `TimeLimitExceededException` → `FinishAttempt`, then the result page with "Time is up! The quiz is finished with the answers you gave." Catch it before the general `InvalidAttemptStateException`, because it derives from it.
4. **Finish** (POST) → `FinishAttempt` → result page.
5. **Quit** (POST, after the confirm "Quit now? Your attempt will be cancelled.") → `CancelAttempt` → the catalog with "The attempt was cancelled."

**Result after finishing**: `FinishAttempt` returns `EmailSent`, but `GetAttemptResult` doesn't fill it in. Pass `EmailSent` to the result page through TempData, and show the email line only when it's there (like the console's `justFinished`).

**Countdown**: the services compare times with `DateTime.Now` on the server. Compute the seconds left on the server (`Deadline - DateTime.Now`) and give that number to the page. Don't make the browser parse the deadline, because its clock or time zone may differ. The countdown is only a display; the server enforces the limit. When it reaches zero, submit the Finish form.

## 6. Question form (add and edit)

- Type: Single, Multi or TrueFalse. It's chosen when adding and fixed when editing (the console keeps the type).
- Text (max 1000) and points (1–100).
- **Single and Multi**: up to `Question.MaxOptions` (10) option inputs of up to 300 characters, at least 2 filled. Rendering 10 rows and ignoring the blank ones is fine, or add rows with a little JavaScript. Mark the correct answer with a radio button (Single) or checkboxes (Multi) next to each option. `CorrectOptionIndexes` are 0-based indexes **into the option list after blank rows are removed**, so map them carefully.
- **TrueFalse**: only a True/False choice. Send an empty `Options` list and `CorrectOptionIndexes = [TrueFalseQuestion.TrueIndex]` or `[TrueFalseQuestion.FalseIndex]`.
- **Edit**: prefill the text, points and option texts (ordered by `Position`), but never the correct answer: it's private and isn't in any DTO. Show the console's note: "The correct answer is private, so enter it again."
- The services and the domain decide what's valid and report every problem in `DomainValidationException.Errors`. Show them all. HTML or data-annotation checks (required, lengths, ranges) are fine as a convenience, but they must match the limits above.

## 7. Errors and logging (a web version of `SafeExecutor`)

Handle errors in one place, such as a helper on the base page model, not with try/catch copied into every handler:

- `DomainValidationException` → every message in `Errors` goes into the validation summary; log it as `Error`.
- `InvalidCredentialsException` → show the message. Don't log it again; the service already logged `LoginFailed`.
- `NotFoundException`, `UnauthorizedActionException`, `DuplicateUserException`, `InvalidAttemptStateException`, `ArgumentException` → show the message and log it.
- Anything else → log it with `IAppLogger.Log(LogEventType.Error, …, userId)` and show "Something went wrong, and the error was logged." Never show a stack trace outside Development.

Success, warning and error messages that must survive a redirect go through TempData, and the layout shows them.

## 8. Limits

- Don't change Domain, Application or Infrastructure apart from `GetAttemptSession` and its tests. If you really need another change there, keep it small, test it, and list it in your summary.
- Don't change how `QuizMaster.UI` behaves.
- Keep every design decision in CLAUDE.md: private correct answers, answers stored as original option positions, attempt state changed only through the service, passwords hashed and checked only by `UserService` (BCrypt), anyone can register as a student or a lecturer, and email format checking stays off.
- Don't initialize git, push, or install global tools without asking the user first.

## 9. Docs

- `README.md`: add the project to the solution structure and add a "Web app" section. It covers:
  - how to run it: `dotnet run --project QuizMaster.Web` and the URL from `Properties/launchSettings.json`, or set `QuizMaster.Web` as the startup project in Visual Studio;
  - that it shares the database with the console app;
  - the seeded logins;
  - that without SMTP, result emails appear in the web app's console window.
  Mention the web pages in the requirement table where they apply.
- `CLAUDE.md`:
  - add `QuizMaster.Web` to the project list and the commands;
  - note that services are registered in both `Program.cs` files;
  - record the attempt-flow decision (a GET never starts an attempt; `GetAttemptSession` reads it);
  - add a third out-of-date point about `../QuizMaster-Prompt.md`: it says there is no web project.

## 10. Acceptance criteria

1. `dotnet build QuizMaster.slnx` reports 0 errors and no new warnings.
2. `dotnet test --project QuizMaster.Tests/QuizMaster.Tests.csproj` passes the 91 existing tests and the new ones.
3. The console app still starts and works.
4. An end-to-end check of the web app against a throwaway database: set `QUIZMASTER_ConnectionStrings__QuizMasterDb` to a connection string for `QuizMasterDb_WebSmokeTest`, run the app, and drop that database afterwards. Use a browser tool if you have one; otherwise use curl with a cookie jar and read `__RequestVerificationToken` from each form.
   - Register a new student, log in, start "C# OOP Basics", answer a few questions, reload the page (same progress, nothing lost), finish, and see the result (with the certificate code on a pass), My attempts and both leaderboards.
   - Leave a quiz halfway and start it again: it continues, with the resume message. Quit a quiz: it's cancelled.
   - Submit with nothing selected: an error is shown and nothing is saved. Post an earlier question again from a stale page: it isn't saved.
   - Create a quiz with a 1-minute limit and let the countdown run out: the attempt finishes with the answers given. An answer posted after the deadline is rejected and the attempt finishes.
   - As `lela_lecturer` / `securePass`: create a quiz, try to publish it while it's empty (the errors are shown), add a Single, a Multi and a TrueFalse question, edit one (the correct answer must be entered again), publish, close and delete it, and open Statistics and Logs.
   - A student who opens a `/Lecturer/...` page gets Access denied. A logged-out user is sent to Login, then back to the page after logging in.
5. README and CLAUDE.md are updated.

**Work order**
1. Read the files listed at the top.
2. Create the project, add it to the solution, and write `Program.cs` (configuration, services, migrate and seed, authentication). Build.
3. The layout and navbar, the base page model with error handling, and the Account pages.
4. Student pages: the catalog, `GetAttemptSession` and its tests, Take, Result, Attempts.
5. Lecturer pages: quizzes, create and edit, Manage, the question form, Statistics, Logs.
6. Leaderboards.
7. README and CLAUDE.md.
8. Build, test, and run the end-to-end check on the throwaway database.

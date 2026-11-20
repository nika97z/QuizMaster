# QuizMaster

A console quiz platform (OOP project, assignment 6). Lecturers create quizzes with three question types. Students take them, get a score, a Passed/Failed result, an email and (on a pass) a certificate code. Leaderboards and statistics are built with LINQ.

It uses the same layered architecture as FitZone, with **DTOs** and **SQL Server (EF Core)** instead of CSV/TXT files. Besides the console app (which the assignment requires), an **ASP.NET Core Razor Pages** web app offers the same features in a browser, on the same database. In both apps a lecturer can also let **AI (OpenAI)** write a quiz on a chosen or random subject.

## Solution structure

| Project | Contents |
|---|---|
| `QuizMaster.Domain` | Entities with behaviour, enums, DTOs (`Models/DTOs/<X>DTOs`), repository/service interfaces, `QuestionFactory`, domain exceptions |
| `QuizMaster.Application` | Services (`Services/Interfaces` + `Implementations`, including `QuizGenerationService` for AI quizzes), AutoMapper `Profiles`, exceptions |
| `QuizMaster.Infrastructure` | `QuizMasterDbContext`, EF configurations, repositories, `UnitOfWork`, `DbLogger`, email services, `Ai/OpenAiQuizGenerator`, seeder, migrations |
| `QuizMaster.UI` | Console app: `Program.cs` (DI, config, migrate, seed), `QuizMasterSystem`, lecturer/student menus |
| `QuizMaster.Web` | Web app (Razor Pages): `Program.cs` (the same DI, config, migrate and seed, plus cookie login), `Pages/Account`, `Pages/Student`, `Pages/Lecturer`, `Pages/Leaderboards`, `Helpers` |
| `QuizMaster.Tests` | 113 tests: domain rules, services (Moq), repositories (EF Core InMemory), the OpenAI generator (fake HTTP handler, no real calls) |

As in FitZone: repositories never call `SaveChanges()` (services do, through `IUnitOfWork`), services map entities to DTOs with AutoMapper, and everything is wired with `ServiceCollection`. Both apps register the same services in their own `Program.cs`.

## Requirements

- .NET 11 SDK (targets `net11.0`) and Visual Studio 2026
- SQL Server Express (`.\SQLEXPRESS`). LocalDB also works: `Server=(localdb)\MSSQLLocalDB;...`

## Run the console app

1. Check the connection string in `QuizMaster.UI/appsettings.json`.
2. In Visual Studio, open `QuizMaster.slnx`, set **QuizMaster.UI** as the startup project, and press F5. Or from this folder:

   ```bash
   dotnet run --project QuizMaster.UI
   ```

On startup the app creates the `QuizMasterDb` database, applies the migrations and adds sample data.

**Seeded logins**

| User | Password | Role |
|---|---|---|
| `lela_lecturer` | `securePass` | Lecturer |
| `luka_student` | `pass123` | Student (has the sample attempt: 16/20, 80%, Passed) |

New users can register as a student or a lecturer.

## Web app

```bash
dotnet run --project QuizMaster.Web
```

Then open http://localhost:5209 (the `http` profile in `QuizMaster.Web/Properties/launchSettings.json`; the `https` profile uses https://localhost:7081). In Visual Studio, set **QuizMaster.Web** as the startup project and press F5.

- It uses the same `QuizMasterDb` database as the console app (connection string in `QuizMaster.Web/appsettings.json`), and it also migrates and seeds it on startup, so either app can be started first and both can run at the same time.
- Log in with the seeded logins above, or register as a student or a lecturer.
- Students: quiz catalog, taking a quiz one question at a time with a countdown, result (with the certificate code on a pass), my attempts, leaderboards. Lecturers: my quizzes, create and edit quizzes, add/edit/remove questions, publish/close/delete, statistics, leaderboards, recent log entries.
- Without SMTP settings, result emails are printed in the web app's console window. SMTP settings for the web app go in `QuizMaster.Web/appsettings.Local.json` (same format as below).
- A started attempt is kept in the database, so a student can reload the page, close it, or come back later from **My attempts** while there is time left.

## AI quiz generation (OpenAI)

A lecturer can let AI write a quiz: on the web app's **Create quiz** page ("Let AI create it"), or with option **7) Create a quiz with AI** in the console's lecturer menu. Choose a subject or a random subject (picked from a list of about 25 subjects), the number of questions (1-10), the time limit and the pass percent.

- The AI writes the title and the questions (single choice, multiple choice and true/false) with their correct answers. Every question goes through the same checks as a lecturer's own; questions that fail are left out, and the page says how many.
- The quiz is saved as a **draft**. Check the questions and answers on the Manage page (the AI can be wrong), edit or remove what needs it, then publish.
- Setup: put an OpenAI API key in `appsettings.Local.json` (ignored by git), in `QuizMaster.Web` for the web app and in `QuizMaster.UI` for the console:

  ```json
  {
    "OpenAI": {
      "ApiKey": "sk-..."
    }
  }
  ```

  The environment variable `QUIZMASTER_OpenAI__ApiKey` also works. The model is `OpenAI:Model` in `appsettings.json` (default `gpt-5.4-mini`, about 4 seconds for 5 questions). Without a key, both apps say that AI quiz generation isn't set up, and everything else works as before.
- Each generation is one OpenAI API request, billed to the key's account.

## Email (SMTP)

When `Smtp:Host` is empty, result emails are printed in the console, so the app works without any setup. To send real emails, put the settings in `QuizMaster.UI/appsettings.Local.json` (ignored by git), so the password never gets committed. The web app reads `QuizMaster.Web/appsettings.Local.json` the same way:

```json
{
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "you@gmail.com",
    "Password": "your-app-password",
    "FromAddress": "you@gmail.com"
  }
}
```

Gmail needs an [App Password](https://myaccount.google.com/apppasswords), not your normal password. Environment variables also work, for example `QUIZMASTER_Smtp__Password`.

## Migrations

`dotnet-ef` is installed as a local tool for this folder (`dotnet-tools.json`):

```bash
dotnet tool restore
```

```bash
dotnet ef migrations add <Name> --project QuizMaster.Infrastructure --startup-project QuizMaster.UI
```

Or in Visual Studio's Package Manager Console:

```
Add-Migration <Name> -Project QuizMaster.Infrastructure -StartupProject QuizMaster.UI
```

The app applies new migrations itself on the next start.

## Tests

```bash
dotnet test --project QuizMaster.Tests/QuizMaster.Tests.csproj
```

Or use Test Explorer in Visual Studio. `global.json` switches `dotnet test` to the Microsoft Testing Platform, which xunit.v3 needs on the .NET 10+ SDK.

## Where each requirement is implemented

| Requirement | Implementation |
|---|---|
| Registration and login, unique username and email | `UserService.Register` / `Login`, unique indexes in `UserConfiguration`, BCrypt hashes (`BCrypt.Net-Next`, salt kept inside the hash, never plain text); web: `Account/Register`, `Account/Login` (cookie login) |
| Quiz catalog (title, topic, questions, max score, time limit, pass %) | `QuizService.GetPublishedQuizzes` → `QuizDTO`, `StudentMenu.ShowCatalog`; web: `Student/Quizzes` |
| Start quiz → Attempt with start time, `InProgress` | `AttemptService.StartAttempt`, `Attempt.Start`; web: the Start button on `Student/Quizzes` |
| Show Single/Multi/TrueFalse questions | `Question.DisplayQuestion()` (overridden per type), `StudentMenu.AskQuestion`; web: `Student/Take` (reads the attempt with `AttemptService.GetAttemptSession`) |
| Check with `CheckAnswer()`, answer hidden until the end | `CheckAnswer()` overrides; `QuestionDTO` has no answer field |
| Score, percent, correct/incorrect, Passed/Failed | `Attempt.Finish`, `Attempt.CalculatePercent`, `AttemptService.BuildResult`; web: `Student/Result` |
| Own attempts, leaderboard by best scores | `AttemptService.GetStudentAttempts`, `StatisticsService.GetOverallLeaderboard` / `GetQuizLeaderboard`; web: `Student/Attempts`, `Leaderboards` |
| Data storage (CSV/TXT in the assignment) | **SQL Server**: `QuizMasterDbContext`, repositories, `UnitOfWork`, `Migrations` |
| Roles User → Lecturer, Student | `User` (abstract) → `Lecturer`, `Student` (TPH, `Role` column); `AccessGuard` checks the role in every service; web: `Lecturer` and `Student` folders need that role |
| Lecturer: create, edit, publish/close, delete quizzes; add questions with points | `QuizService`, `QuestionService`, `QuizEditor`; web: `Lecturer/Quizzes`, `CreateQuiz`, `EditQuiz`, `Manage`, `Question` |
| Question validation before publishing | `Quiz.ValidateForPublish` / `Publish`, `Question.Validate()` overrides, `QuestionFactory` |
| LINQ: `OrderByDescending` leaderboard, `Average`, group by topic and quiz, best student | `StatisticsService`; web: `Lecturer/Statistics`, `Leaderboards` |
| SMTP result with certificate code | `AttemptService.TrySendResultEmail`, `ResultEmailBuilder`, `SmtpEmailService` / `ConsoleEmailService` |
| Logging (start, answer, finish, score, cancel, quiz changes, errors) | `IAppLogger` → `DbLogger` (`LogEntries` table); `SafeExecutor` logs errors (web: `QuizMasterPageModel.HandleError`); lecturer menu option 6 and the web page `Lecturer/Logs` show the log |
| Bonus: random question and option order | `AttemptService.StartAttempt` and `ToShuffledDto`; answers use original option positions, so grading stays correct |
| Extra: AI writes a quiz on a chosen or random subject | `QuizGenerationService`, `IQuizGenerator` → `OpenAiQuizGenerator`; web: `Lecturer/CreateQuiz`; console: `LecturerMenu.CreateQuizWithAi` |
| Inheritance | `Question` → 3 types, `User` → 2 roles |
| Encapsulation: private correct answer | `_correctOptionIndex`, `_correctOptionIndexes`, `_correctValue` are private fields mapped by EF Core; no getter exists |
| Polymorphism | abstract `DisplayQuestion()`, `CheckAnswer()`, `Validate()`, virtual `ValidateSelection()`, abstract `User.Role` |
| Attempt status protected | private setters; changes only via `Start()`, `SubmitAnswer()`, `Finish()`, `Cancel()` |
| `QuizMasterSystem` | `QuizMaster.UI/QuizMasterSystem.cs` (the web app's equivalent is the layout's navbar plus the role folders) |
| `FileManager` | replaced by `QuizMasterDbContext` + repositories |
| `EmailService`, `Logger` | `IEmailService` / `IAppLogger` (Domain) with implementations in Infrastructure |

**Validation rules (section 4)**

| Rule | Where |
|---|---|
| Single: exactly one correct; Multi: at least one; TrueFalse: only true/false | `QuestionFactory.Create`, `Validate()` overrides |
| Empty quiz can't be published; time limit 1-300; pass percent 1-100 | `Quiz.ValidateForPublish`, `Quiz.ValidateSettings`, check constraints |
| Chosen index must exist; Multi duplicates count once | `ValidateSelection()`, `Attempt.SubmitAnswer` |
| No answers after finish; one active attempt per quiz | `Attempt.EnsureInProgress`, `AttemptService.StartAttempt` (continues the active attempt) |
| Score between 0 and max; percent rounded to 2 decimals | `Attempt.Finish`, `CalculatePercent`, check constraint |
| Data and SMTP errors handled with try/catch and logged | `SafeExecutor` (web: `QuizMasterPageModel`), `AttemptService.TrySendResultEmail`, `DbLogger` |

## Rules worth knowing

- A question gives all of its points or none. A Multi answer must match the correct set exactly.
- If time runs out, the quiz finishes with the answers given so far. An unfinished attempt continues when the student starts the quiz again, as long as time is left. Otherwise it is cancelled.
- A published quiz must be closed before it can be edited. A quiz with attempts can't be deleted (close it instead), and a question that was answered in past attempts can't be removed.
- Correct answers are never shown, not even to the lecturer. When editing a question, the lecturer enters the correct answer again.
- **Temporary, for easier testing:** the email format isn't checked (any text is accepted, but it must still be unique), and registration asks for the password only once. See `UserService.Register` to switch the check back on.

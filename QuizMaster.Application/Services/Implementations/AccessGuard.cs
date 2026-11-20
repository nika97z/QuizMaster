using QuizMaster.Application.Exceptions;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.Entities;

namespace QuizMaster.Application.Services.Implementations
{
	/// <summary>
	/// Role-based authorization checks shared by the services. The UI hides menus by role,
	/// but the services check again, so a wrong call can never get through.
	/// </summary>
	internal static class AccessGuard
	{
		public static void RequireValidId(int id, string name = "Id")
		{
			if (id <= 0)
			{
				throw new ArgumentException($"{name} must be greater than 0.");
			}
		}

		public static Lecturer RequireLecturer(IUserRepository users, int userId)
		{
			RequireValidId(userId, "User id");
			return users.GetUserById(userId) as Lecturer
				?? throw new UnauthorizedActionException("Only lecturers can do this.");
		}

		public static Student RequireStudent(IUserRepository users, int userId)
		{
			RequireValidId(userId, "User id");
			return users.GetUserById(userId) as Student
				?? throw new UnauthorizedActionException("Only students can do this.");
		}

		public static Quiz RequireOwnedQuiz(IUserRepository users, IQuizRepository quizzes, int lecturerId, int quizId)
		{
			RequireLecturer(users, lecturerId);
			RequireValidId(quizId, "Quiz id");
			var quiz = quizzes.GetQuizWithQuestions(quizId)
				?? throw new NotFoundException($"Quiz {quizId} was not found.");
			if (quiz.LecturerId != lecturerId)
			{
				throw new UnauthorizedActionException("You can only manage your own quizzes.");
			}
			return quiz;
		}
	}
}

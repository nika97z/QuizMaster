using Microsoft.AspNetCore.Mvc;
using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.UserDTOs;
using QuizMaster.Web.Helpers;

namespace QuizMaster.Web.Pages.Lecturer
{
	public class StudentsModel : QuizMasterPageModel
	{
		private readonly IUserService _userService;

		public StudentsModel(IUserService userService, IAppLogger logger) : base(logger)
		{
			_userService = userService;
		}

		public List<UserDTO> Students { get; private set; } = new();

		public void OnGet()
		{
			Execute(() => Students = _userService.GetStudents(CurrentUserId));
		}

		public IActionResult OnPostDelete(int studentId)
		{
			if (Execute(() => _userService.DeleteStudent(CurrentUserId, studentId)))
			{
				TempData.AddSuccess("The student account was deleted.");
			}
			else
			{
				KeepErrorsForRedirect();
			}
			return RedirectToPage();
		}
	}
}

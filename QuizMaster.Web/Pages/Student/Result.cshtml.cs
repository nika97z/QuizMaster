using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.AttemptDTOs;
using System.Globalization;

namespace QuizMaster.Web.Pages.Student
{
	public class ResultModel : QuizMasterPageModel
	{
		private const string EmailOutcomeKey = "Result.Email";

		private readonly IAttemptService _attemptService;

		public ResultModel(IAttemptService attemptService, IAppLogger logger) : base(logger)
		{
			_attemptService = attemptService;
		}

		[BindProperty(SupportsGet = true)]
		public int AttemptId { get; set; }

		public AttemptResultDTO? Result { get; private set; }

		/// <summary>
		/// Set only right after finishing (like the console's justFinished): true when the result email was sent.
		/// </summary>
		public bool? EmailSent { get; private set; }

		public void OnGet()
		{
			Execute(() => Result = _attemptService.GetAttemptResult(CurrentUserId, AttemptId));
			EmailSent = ReadEmailOutcome();
		}

		/// <summary>
		/// FinishAttempt says whether the result email was sent, but GetAttemptResult doesn't, so the Take page passes
		/// it to this page once, through TempData.
		/// </summary>
		public static void RememberEmailOutcome(ITempDataDictionary tempData, int attemptId, bool emailSent)
		{
			tempData[EmailOutcomeKey] = $"{attemptId.ToString(CultureInfo.InvariantCulture)}:{(emailSent ? "sent" : "failed")}";
		}

		private bool? ReadEmailOutcome()
		{
			if (TempData[EmailOutcomeKey] is not string value)
			{
				return null;
			}
			var parts = value.Split(':');
			if (parts.Length != 2 || parts[0] != AttemptId.ToString(CultureInfo.InvariantCulture))
			{
				return null;
			}
			return parts[1] == "sent";
		}
	}
}

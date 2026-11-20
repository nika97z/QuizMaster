using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace QuizMaster.Web.Helpers
{
	/// <summary>
	/// Success, warning and error messages that must survive a redirect (Post/Redirect/Get). They travel in TempData,
	/// and the layout shows them once.
	/// </summary>
	public static class FlashMessages
	{
		public const string SuccessKey = "Flash.Success";
		public const string WarningKey = "Flash.Warning";
		public const string ErrorKey = "Flash.Error";

		private const char Separator = '\n';

		public static void AddSuccess(this ITempDataDictionary tempData, string message)
		{
			Add(tempData, SuccessKey, message);
		}

		public static void AddWarning(this ITempDataDictionary tempData, string message)
		{
			Add(tempData, WarningKey, message);
		}

		public static void AddErrors(this ITempDataDictionary tempData, IEnumerable<string> messages)
		{
			foreach (var message in messages)
			{
				Add(tempData, ErrorKey, message);
			}
		}

		/// <summary>
		/// Reads the messages of one kind. Reading marks them as shown, so they don't appear again.
		/// </summary>
		public static IReadOnlyList<string> Read(this ITempDataDictionary tempData, string key)
		{
			return tempData[key] is string text
				? text.Split(Separator, StringSplitOptions.RemoveEmptyEntries)
				: Array.Empty<string>();
		}

		private static void Add(ITempDataDictionary tempData, string key, string message)
		{
			if (string.IsNullOrWhiteSpace(message))
			{
				return;
			}
			// Peek doesn't mark an earlier message of this request as read, so both are kept.
			var existing = tempData.Peek(key) as string;
			tempData[key] = string.IsNullOrEmpty(existing) ? message : existing + Separator + message;
		}
	}
}

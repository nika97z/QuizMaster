namespace QuizMaster.Domain.Helpers
{
	/// <summary>
	/// Stores lists of indexes or ids in one text column, e.g. "0;2" or "12;9;14".
	/// </summary>
	public static class IndexList
	{
		private const char Separator = ';';

		public static string Join(IEnumerable<int> values)
		{
			return string.Join(Separator, values ?? Enumerable.Empty<int>());
		}

		public static string JoinDistinctSorted(IEnumerable<int> values)
		{
			return Join((values ?? Enumerable.Empty<int>()).Distinct().OrderBy(v => v));
		}

		/// <summary>
		/// Parses the stored text, keeping the original order. Returns false when the text is corrupted.
		/// </summary>
		public static bool TryParse(string? text, out List<int> values)
		{
			values = new List<int>();
			if (string.IsNullOrWhiteSpace(text))
			{
				return true;
			}

			foreach (var part in text.Split(Separator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
			{
				if (!int.TryParse(part, out var value))
				{
					values = new List<int>();
					return false;
				}
				values.Add(value);
			}
			return true;
		}

		public static List<int> Parse(string? text)
		{
			if (!TryParse(text, out var values))
			{
				throw new FormatException($"Corrupted index list: '{text}'.");
			}
			return values;
		}
	}
}

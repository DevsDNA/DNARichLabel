namespace RichLabel.iOS
{
	using System.Text.RegularExpressions;
	using Foundation;

	public static class StringExtensions
	{

		public static string GetParsedMarkdownString(string unparsedString)
		{
			// remove markdown links
			var parsedString = Regex.Replace(unparsedString, @"\@\[([^\]]+)\]\(([^)]+)\)", m => string.Format("{0}", m.Groups[1].Value));
			parsedString = Regex.Replace(parsedString, @"\[([^\]]+)\]\(([^)]+)\)", m => string.Format("{0}", m.Groups[1].Value));
			return parsedString;
		}
	}
}

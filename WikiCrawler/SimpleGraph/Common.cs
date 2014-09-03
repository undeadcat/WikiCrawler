namespace SimpleGraph
{
	public static class Common
	{
		public static int? ParseIntOrNull(string s)
		{
			int res;
			if (int.TryParse(s, out res))
				return res;
			return null;
		}
	}
}
using InnerTube.Models;

namespace LightTube.Contexts
{
	public class SearchContext : BaseContext
	{
		public YoutubeSearchResults Results;
		public string Query;
		public string ContinuationKey;
	}
}
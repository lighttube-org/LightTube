using YTProxy.Models;

namespace LightTube.Contexts
{
	public class SearchContext : BaseContext
	{
		public YoutubeSearch Results;
		public string Query;
		public string ContinuationToken;
	}
}
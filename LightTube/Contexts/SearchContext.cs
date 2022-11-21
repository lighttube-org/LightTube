using InnerTube;

namespace LightTube.Contexts;

public class SearchContext : BaseContext
{
	public string Query;
	public string? Filter;
	public InnerTubeSearchResults Search;

	public SearchContext(string query, string? filter, InnerTubeSearchResults search)
	{
		Query = query;
		Filter = filter;
		Search = search;
	}

}
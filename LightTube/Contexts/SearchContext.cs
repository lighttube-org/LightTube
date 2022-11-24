using InnerTube;
using InnerTube.Renderers;

namespace LightTube.Contexts;

public class SearchContext : BaseContext
{
	public string Query;
	public string? Filter;
	public InnerTubeSearchResults? Search;
	public IEnumerable<IRenderer> Results;
	public string? Continuation;

	public SearchContext(string query, string? filter, InnerTubeSearchResults search)
	{
		Query = query;
		Filter = filter;
		Search = search;
		Results = Search.Results;
		Continuation = Search.Continuation;
	}

	public SearchContext(string query, string? filter, InnerTubeContinuationResponse search)
	{
		Query = query;
		Filter = filter;
		Search = null;
		Results = search.Contents;
		Continuation = search.Continuation;
	}
}
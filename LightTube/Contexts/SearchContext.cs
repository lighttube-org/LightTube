using InnerTube;
using InnerTube.Renderers;

namespace LightTube.Contexts;

public class SearchContext : BaseContext
{
    public string Query;
    public SearchParams? Filter;
    public InnerTubeSearchResults? Search;
    public IEnumerable<IRenderer> Results;
    public string? Continuation;

    public SearchContext(HttpContext context, string query, SearchParams? filter, InnerTubeSearchResults search) : base(context)
    {
        Query = query;
        Filter = filter;
        Search = search;
        Results = Search.Results;
        Continuation = Search.Continuation;
    }

    public SearchContext(HttpContext context, string query, SearchParams? filter, InnerTubeContinuationResponse search) : base(context)
    {
        Query = query;
        Filter = filter;
        Search = null;
        Results = search.Contents;
        Continuation = search.Continuation;
    }
}
﻿using InnerTube;
using InnerTube.Models;
using InnerTube.Protobuf.Params;
using InnerTube.Renderers;

namespace LightTube.Contexts;

public class SearchContext : BaseContext
{
    public string Query;
    public SearchParams? Filter;
    public InnerTubeSearchResults? Search;
    public IEnumerable<RendererContainer> Results;
    public IEnumerable<RendererContainer> Chips;
    public string? Continuation;

    public SearchContext(HttpContext context, string query, SearchParams? filter, InnerTubeSearchResults search) : base(context)
    {
        Query = query;
        Filter = filter;
        Search = search;
        Results = Search.Results;
        Continuation = Search.Continuation;
        Chips = Search.Chips;
    }

    public SearchContext(HttpContext context, string query, SearchParams? filter, SearchContinuationResponse search) : base(context)
    {
        Query = query;
        Filter = filter;
        Search = null;
        Results = search.Results;
        Continuation = search.ContinuationToken;
        Chips = search.Chips ?? [];
    }
}
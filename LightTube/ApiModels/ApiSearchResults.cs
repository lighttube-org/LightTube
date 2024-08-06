using InnerTube;
using InnerTube.Models;
using InnerTube.Protobuf.Params;
using InnerTube.Renderers;

namespace LightTube.ApiModels;

public class ApiSearchResults
{
	public RendererContainer[] Results { get; }
	public ShowingResultsFor? QueryCorrecter { get; }
	public RendererContainer[] Chips { get; }
	public string? Continuation { get; }
	public string[] Refinements { get; }
	public long EstimatedResults { get; }
	public SearchParams? SearchParams { get; }

	public ApiSearchResults(InnerTubeSearchResults results, SearchParams searchParams)
	{
		Results = results.Results;
		QueryCorrecter = results.QueryCorrecter;
		Chips = results.Chips;
		Continuation = results.Continuation;
		Refinements = results.Refinements;
		EstimatedResults = results.EstimatedResults;
		SearchParams = searchParams;
	}

	public ApiSearchResults(ContinuationResponse continuationResults)
	{
		Results = continuationResults.Results;
		QueryCorrecter = null;
		Chips = [];
		Continuation = continuationResults.ContinuationToken;
		Refinements = [];
		EstimatedResults = 0;
		SearchParams = null;
	}
}
using InnerTube;
using InnerTube.Renderers;

namespace LightTube.ApiModels;

public class ApiSearchResults
{
	public IEnumerable<IRenderer> SearchResults { get; }
	public IEnumerable<InnerTubeSearchResults.Options.Group>? SearchFilters { get; }
	public long? EstimatedResultCount { get; }
	public string? ContinuationKey { get; }
	public InnerTubeSearchResults.TypoFixer? TypoFixer { get; }

	public ApiSearchResults(InnerTubeSearchResults results)
	{
		SearchResults = results.Results;
		ContinuationKey = results.Continuation;
		SearchFilters = results.SearchOptions?.Groups;
		EstimatedResultCount = results.EstimatedResults;
		TypoFixer = results.DidYouMean;
	}

	public ApiSearchResults(InnerTubeContinuationResponse continuationResults)
	{
		SearchResults = continuationResults.Contents;
		ContinuationKey = continuationResults.Continuation;
		SearchFilters = null;
		EstimatedResultCount = null;
		TypoFixer = null;
	}
}
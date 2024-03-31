namespace LightTube.Contexts;

public class ImportContext : BaseContext
{
	public string? Message { get; }
	public bool IsError { get; }

	public ImportContext(HttpContext context, string? message = null, bool isError = false) : base(context)
	{
		Message = message;
		IsError = isError;
	}
}
namespace LightTube.Contexts;

public class ImportContext(HttpContext context, string? message = null, bool isError = false) : BaseContext(context)
{
    public string? Message { get; } = message;
    public bool IsError { get; } = isError;
}
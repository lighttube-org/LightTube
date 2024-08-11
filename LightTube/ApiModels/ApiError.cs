namespace LightTube.ApiModels;

public class ApiError(string errorMessage, int errorCode)
{
    public string Message { get; } = errorMessage;
    public int Code { get; } = errorCode;
}
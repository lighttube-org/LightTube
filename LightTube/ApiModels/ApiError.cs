namespace LightTube.ApiModels;

public class ApiError
{
	public string Message { get; }
	public int Code { get; }

	public ApiError(string errorMessage, int errorCode)
	{
		Message = errorMessage;
		Code = errorCode;
	}
}
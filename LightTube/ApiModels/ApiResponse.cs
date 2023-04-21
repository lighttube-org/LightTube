namespace LightTube.ApiModels;

public class ApiResponse<T>
{
	public string Status { get; }
	public ApiError? Error { get; }
	public T? Data { get; }
	public ApiUserData? UserData { get; }

	public ApiResponse(T data, ApiUserData? userData)
	{
		Status = "OK";
		Error = null;
		Data = data;
		UserData = userData;
	}

	public ApiResponse(string status, string errorMessage, int errorCode)
	{
		Status = status;
		Error = new(errorMessage, errorCode);
		Data = default;
		UserData = null;
	}
}
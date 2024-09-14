using InnerTube;
using Serilog;

namespace LightTube.PoToken;

public static class PoTokenManager
{
	private static Timer refreshTimer;
	private static SimpleInnerTubeClient innerTube;
	private static HttpClient httpClient = new();
	private static string apiUrl = "";

	private static List<RequestClient> requiredClients =
	[
		RequestClient.WEB,
		RequestClient.TV_EMBEDDED
	];

	public static void Init(string apiUrl, SimpleInnerTubeClient innerTubeClient)
	{
		Log.Information("[PoTokenManager] Initializing PoToken manager...");
		PoTokenManager.apiUrl = apiUrl;
		innerTube = innerTubeClient;
		refreshTimer = new Timer(RefreshPoToken, null, TimeSpan.Zero, TimeSpan.FromMinutes(15));
	}

	private static async void RefreshPoToken(object? _)
	{
		foreach (RequestClient client in requiredClients)
		{
			try
			{
				PoTokenResponse? response = await httpClient.GetFromJsonAsync<PoTokenResponse>($"{apiUrl}/generate");

				if (response == null) throw new Exception("Response is null");
				if (!response.Success) throw new Exception(response.Error);
				if (response.Response == null) throw new Exception("response.Response is null? what???");

				innerTube.ProvideSecrets(client, response.Response.VisitorData, response.Response.PoToken);
				Log.Information("[PoTokenManager] Loaded secrets for client {0}\nVisitorInfo: {1}\nPoToken: {2}", client,
					response.Response.VisitorData, response.Response.PoToken);
			}
			catch (Exception e)
			{
				Log.Error(e, "[PoTokenManager] Failed to get PoToken for client {0}.", client);
			}
		}
	}
}
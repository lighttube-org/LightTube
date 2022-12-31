using InnerTube.Renderers;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace LightTube.Database.Models;

[BsonIgnoreExtraElements]
public class DatabaseUser
{
	private const string INNERTUBE_GRID_RENDERER_TEMPLATE = "{\"items\": [%%CONTENTS%%]}";

	private const string INNERTUBE_MESSAGE_RENDERER_TEMPLATE = "{\"messageRenderer\":{\"text\":{\"simpleText\":\"%%MESSAGE%%\"}}}";
	private const string ID_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
	public string UserID { get; set; }
	public string PasswordHash { get; set; }
	public Dictionary<string, SubscriptionType> Subscriptions { get; set; }
	public string LTChannelID { get; set; }

	[BsonIgnoreIfNull] [Obsolete("Use Subscriptions dictionary instead")]
	public string[]? SubscribedChannels;

	[BsonIgnoreIfNull] [Obsolete("Use UserID instead")]
	public string? Email;

	public static DatabaseUser CreateUser(string userId, string password) =>
		new()
		{
			UserID = userId,
			PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
			Subscriptions = new Dictionary<string, SubscriptionType>(),
			LTChannelID = GetChannelId(userId)
		};

	public void Migrate()
	{
#pragma warning disable CS0618
		if (SubscribedChannels is not null)
		{
			Subscriptions ??= new Dictionary<string, SubscriptionType>();
			foreach (string id in SubscribedChannels)
				if (!Subscriptions.ContainsKey(id))
					Subscriptions.Add(id, SubscriptionType.NOTIFICATIONS_ON);
			SubscribedChannels = null;
		}

		if (Email is not null && UserID is null)
		{
			UserID = Email;
			Email = null;
		}
#pragma warning restore CS0618

		LTChannelID ??= GetChannelId(UserID);
	}

	public static string GetChannelId(string userId)
	{
		Random rng = new(userId.GetHashCode());
		string channelId = "LT_UC";
		while (channelId.Length < 24) 
			channelId += ID_ALPHABET[rng.Next(0, ID_ALPHABET.Length)];
		return channelId;
	}

	public GridRenderer PlaylistRenderers()
	{
		IEnumerable<DatabasePlaylist> playlists =
			DatabaseManager.Playlists.GetUserPlaylists(UserID, PlaylistVisibility.VISIBLE);
		string playlistsJson = playlists.Any()
			? string.Join(',', playlists.Select(x => x.GetInnerTubeGridPlaylistJson()))
			: INNERTUBE_MESSAGE_RENDERER_TEMPLATE.Replace("%%MESSAGE%%", "This user doesn't have any public playlists.");

		string json = INNERTUBE_GRID_RENDERER_TEMPLATE
			.Replace("%%CONTENTS%%", playlistsJson);
		return new GridRenderer(JObject.Parse(json));
	}
}
using InnerTube.Protobuf;
using InnerTube.Renderers;
using LightTube.Localization;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace LightTube.Database.Models;

[BsonIgnoreExtraElements]
public class DatabaseUser
{
    private const string ID_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
    public string UserId { get; set; }
    [JsonIgnore] public string PasswordHash { get; set; }
    [JsonIgnore] public Dictionary<string, SubscriptionType> Subscriptions { get; set; }
    public string LTChannelId { get; set; }

    public static DatabaseUser CreateUser(string userId, string password) =>
        new()
        {
            UserId = userId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Subscriptions = [],
            LTChannelId = GetChannelId(userId)
        };

    public static string GetChannelId(string userId)
    {
        Random rng = new(userId.GetHashCode());
        string channelId = "LT_UC";
        while (channelId.Length < 24)
            channelId += ID_ALPHABET[rng.Next(0, ID_ALPHABET.Length)];
        return channelId;
    }

    public List<RendererContainer> PlaylistRenderers(LocalizationManager localization, PlaylistVisibility minVisibility = PlaylistVisibility.Visible)
    {
        DatabasePlaylist[] playlists =
            DatabaseManager.Playlists.GetUserPlaylists(UserId, minVisibility).ToArray();
        if (playlists.Length == 0)
        {
            return
            [
                new RendererContainer
                {
                    Type = "message",
                    OriginalType = "messageRenderer",
                    Data = new MessageRendererData(localization.GetRawString("channel.noplaylists"))
                }
            ];
        }

        return playlists.Select(x => new RendererContainer
        {
            Type = "playlist",
            OriginalType = "gridPlaylistRenderer",
            Data = new PlaylistRendererData
            {
                PlaylistId = x.Id,
                Thumbnails = [
                    new Thumbnail
                    {
                        Url = $"https://i.ytimg.com/vi/{x.VideoIds.FirstOrDefault()}/hqdefault.jpg",
                        Width = 480,
                        Height = 360
                    }
                ],
                Title = x.Name,
                VideoCountText = string.Format(localization.GetRawString("playlist.videos.count"), x.VideoIds.Count),
                SidebarThumbnails = [],
                Author = null
            }
        }).ToList();
    }
}
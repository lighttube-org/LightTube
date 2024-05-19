using InnerTube;
using InnerTube.Models;
using Newtonsoft.Json.Linq;

namespace LightTube.Database.Models;

public class DatabasePlaylist
{
    private const string ID_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
    public string Id;
    public string Name;
    public string Description;
    public PlaylistVisibility Visibility;
    public List<string> VideoIds;
    public string Author;
    public DateTimeOffset LastUpdated;

    public static string GenerateId()
    {
        Random rng = new();
        string playlistId = "LT-PL";
        while (playlistId.Length < 24)
            playlistId += ID_ALPHABET[rng.Next(0, ID_ALPHABET.Length)];
        return playlistId;
    }
}

public enum PlaylistVisibility
{
    Private,
    Unlisted,
    Visible
}
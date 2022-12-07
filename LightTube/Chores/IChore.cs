namespace LightTube.Chores;

public interface IChore
{
	public string Id { get; }
	public Task<string> RunChore(Action<string> updateStatus, Guid id);
}
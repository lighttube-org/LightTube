using System.Reflection;
using MongoDB.Driver.Linq;

namespace LightTube.Chores;

public static class ChoreManager
{
	public static bool ChoreExecuting => _queue.Any(x => x.Value.Running);

	private static Dictionary<string, Type> _chores = new();
	private static Dictionary<Guid, QueueChore> _queue = new();

	public static void RegisterChores()
	{
		foreach (Type choreType in Assembly.GetAssembly(typeof(ChoreManager))!
			         .GetTypes()
			         .Where(x => x.IsAssignableTo(typeof(IChore))))
		{
			try
			{
				IChore chore = (IChore)Activator.CreateInstance(choreType)!;
				_chores.Add(chore.Id, choreType);
			}
			catch { }
		}
	}

	public static Guid QueueChore(string choreId)
	{
		if (!_chores.ContainsKey(choreId))
			throw new KeyNotFoundException($"Unknown chore '{choreId}'");

		QueueChore chore = new(_chores[choreId]);
		_queue.Add(chore.Id, chore);
		NextChore();
		return chore.Id;
	}

	public static void NextChore()
	{
		if (ChoreExecuting) return;
		_queue.Values.FirstOrDefault(x => !x.Running && !x.Complete)?.Start();
	}
}
using System.Diagnostics;

namespace LightTube.Chores;

public class QueueChore
{
	public IChore Chore;
	public Guid Id;
	public Stopwatch Stopwatch;
	public string Status = "";
	public bool Running;
	public bool Complete;

	public QueueChore(Type chore)
	{
		Chore = (IChore)Activator.CreateInstance(chore)!;
		Id = Guid.NewGuid();
		Stopwatch = new Stopwatch();
	}

	public void Start()
	{
		Console.WriteLine($"[CHORE] [{Chore.Id}] Chore started");
		Running = true;
		Stopwatch.Start();
		Chore.RunChore(s => Status = s, Id)
			.ContinueWith(task =>
			{
				Stopwatch.Stop();
				Running = false;
				Complete = true;
				Console.WriteLine($"[CHORE] [{Chore.Id}] Chore complete in {Stopwatch.Elapsed}\n{task.Result}");
				ChoreManager.NextChore();
			});
	}
}
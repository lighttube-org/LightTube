using System.Diagnostics;
using Serilog;

namespace LightTube.Chores;

public class QueueChore(Type chore)
{
    public IChore Chore = (IChore)Activator.CreateInstance(chore)!;
    public Guid Id = Guid.NewGuid();
    public Stopwatch Stopwatch = new();
    public string Status = "";
    public bool Running;
    public bool Complete;

    public void Start()
    {
        Log.Information($"[CHORE] [{Chore.Id}] Chore started");
        Running = true;
        Stopwatch.Start();
        Chore.RunChore(s => Status = s, Id)
            .ContinueWith(task =>
            {
                Stopwatch.Stop();
                Running = false;
                Complete = true;
                Log.Information($"[CHORE] [{Chore.Id}] Chore complete in {Stopwatch.Elapsed}\n{task.Result}");
                ChoreManager.NextChore();
            });
    }
}
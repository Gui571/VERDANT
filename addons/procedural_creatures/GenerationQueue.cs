namespace Verdant;

// At most one generation runs and one request waits. CPU data only; Godot resources
// are created by the caller on the main thread. New requests invalidate old results.
public sealed class GenerationQueue : IDisposable
{
    readonly object gate = new();
    (long revision, Genome genome)? pending;
    (long revision, CreatureRenderer renderer)? ready;
    long revision;
    bool disposed, running;
    public void Request(Genome genome) { genome.Validate(); lock (gate) { ObjectDisposedException.ThrowIf(disposed, this); pending = (++revision, genome); ready = null; if (!running) { running = true; _ = Task.Run(Work); } } }
    void Work() { while (true) { (long revision, Genome genome) job; lock (gate) { if (disposed || pending == null) { running = false; return; } job = pending.Value; pending = null; } var renderer = new CreatureRenderer(job.genome); renderer.Render(Pose.At(0, MotionState.Idle)); lock (gate) { if (!disposed && job.revision == revision) ready = (job.revision, renderer); } } }
    public CreatureRenderer? TakeLatest() { lock (gate) { if (ready == null) return null; var result = ready.Value; ready = null; return !disposed && result.revision == revision ? result.renderer : null; } }
    public void Cancel() { lock (gate) { revision++; pending = null; ready = null; } }
    public void Dispose() { lock (gate) { disposed = true; revision++; pending = null; ready = null; } }
}

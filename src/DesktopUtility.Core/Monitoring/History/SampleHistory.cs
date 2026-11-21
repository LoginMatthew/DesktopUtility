using System.Collections;

namespace DesktopUtility.Core.Monitoring;

public sealed class SampleHistory<T> : IReadOnlyCollection<T>
{
    private readonly Queue<T> samples;

    public SampleHistory(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        Capacity = capacity;
        samples = new Queue<T>(capacity);
    }

    public int Capacity { get; }
    public int Count => samples.Count;

    public void Add(T sample)
    {

        if (samples.Count == Capacity)
        {
            samples.Dequeue();
        }

        samples.Enqueue(sample);
    }

    public void Clear() => samples.Clear();

    public IEnumerator<T> GetEnumerator() => samples.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

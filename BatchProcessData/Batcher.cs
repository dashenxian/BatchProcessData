using Microsoft.Extensions.Primitives;

namespace BatchProcessData;

public sealed class Batcher<T> : IDisposable where T : class
{
    private readonly Action<Batch<T>> _processor;
    private T[] _data;
    private BatchChangeToken _changeToken = default!;
    private readonly int _batchSize;
    private int _index = -1;
    private readonly IDisposable _scheduler;

    public Batcher(Action<Batch<T>> processor, int batchSize, TimeSpan interval)
    {
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _batchSize = batchSize;
        _data = Batch<T>.CreatePooledArray(batchSize);
        _scheduler = ChangeToken.OnChange(() => _changeToken = new BatchChangeToken(_batchSize, interval), OnChange);

        void OnChange()
        {
            var data = Interlocked.Exchange(ref _data, Batch<T>.CreatePooledArray(batchSize));
            if (data[0] is not null)
            {
                Interlocked.Exchange(ref _index, -1);
                _ = Task.Run(() => _processor.Invoke(new Batch<T>(data)));
            }
        }
    }

    public void Add(T item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        var index = Interlocked.Increment(ref _index);
        if (index >= _batchSize)
        {
            SpinWait.SpinUntil(() => _index < _batchSize - 1);
            Add(item);
        }
        _data[index] = item;
        _changeToken.Increase();
    }

    public void Dispose() => _scheduler.Dispose();
}
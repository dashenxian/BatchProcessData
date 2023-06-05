using Microsoft.Extensions.Primitives;

namespace BatchProcessData;

internal sealed class BatchChangeToken : IChangeToken
{
    private readonly IChangeToken _innerToken;
    private readonly int _countThreshold;
    private readonly CancellationTokenSource _expirationTokenSource;
    private readonly CancellationTokenSource _countTokenSource;
    private int _counter;

    public BatchChangeToken(int countThreshold, TimeSpan timeThreshold)
    {
        _countThreshold = countThreshold;
        _countTokenSource = new CancellationTokenSource();
        _expirationTokenSource = new CancellationTokenSource(timeThreshold);
        var countToken = new CancellationChangeToken(_countTokenSource.Token);
        var expirationToken = new CancellationChangeToken(_expirationTokenSource.Token);
        _innerToken = new CompositeChangeToken(new IChangeToken[] { countToken, expirationToken });
    }

    public bool HasChanged => _innerToken.HasChanged;
    public bool ActiveChangeCallbacks => _innerToken.ActiveChangeCallbacks;
    public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => _innerToken.RegisterChangeCallback(s =>
    {
        callback(s);
        _countTokenSource.Dispose();
        _expirationTokenSource.Dispose();
    }, state);
    public void Increase()
    {
        Interlocked.Increment(ref _counter);
        if (_counter >= _countThreshold)
        {
            _countTokenSource.Cancel();
        }
    }
}
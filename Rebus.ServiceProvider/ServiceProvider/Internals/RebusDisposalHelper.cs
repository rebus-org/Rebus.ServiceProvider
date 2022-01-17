using System;
using System.Collections.Concurrent;

namespace Rebus.ServiceProvider.Internals;

class RebusDisposalHelper : IDisposable
{
    readonly ConcurrentStack<IDisposable> _disposables = new();

    public void Add(IDisposable disposable) => _disposables.Push(disposable);

    public void Dispose()
    {
        while (_disposables.TryPop(out var disposable))
        {
            disposable.Dispose();
        }
    }
}
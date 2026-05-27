using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Plugins.RUtils.Scripts.Core.Updater
{
    public class IntervalUpdater
    {
        private CancellationTokenSource _tokenSource;

        private readonly TimeSpan _interval;

        private readonly Action _onUpdate;

        public IntervalUpdater(Action action, float interval = 1f)
        {
            _onUpdate = action;
            _interval = TimeSpan.FromSeconds(interval);
        }

        ~IntervalUpdater()
        {
            Reset();
        }

        public void Start()
        {
            Reset();
            _tokenSource = new CancellationTokenSource();
            Update().Forget();
        }

        public void Stop()
        {
            Reset();
        }

        private void Reset()
        {
            _tokenSource?.Cancel();
            _tokenSource?.Dispose();
        }

        private async UniTaskVoid Update()
        {
            while (true)
            {
                _onUpdate?.Invoke();
                if (await UniTask.Delay(_interval, cancellationToken: _tokenSource.Token).SuppressCancellationThrow())
                {
                    return;
                }
            }
        }
    }
}
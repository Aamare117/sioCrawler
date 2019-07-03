using System;
using System.Threading;
using Xamarin.Forms;

namespace FindMe.Services
{
    public class MyTimer
    {
        private readonly TimeSpan _timespan;
        private readonly Action _callback;

        private CancellationTokenSource _cancellationToken;

        public MyTimer(TimeSpan timespan, Action callback)
        {
            _timespan = timespan;
            _callback = callback;
            _cancellationToken = new CancellationTokenSource();
        }

        public void Start()
        {
            CancellationTokenSource cts = _cancellationToken;

            Device.StartTimer(_timespan, () =>
            {
                if (cts.IsCancellationRequested)
                    return false;

                _callback.Invoke();
                return true;
            });
        }

        public void Stop()
        {
            Interlocked.Exchange(ref this._cancellationToken, new CancellationTokenSource()).Cancel();
        }
    }
}
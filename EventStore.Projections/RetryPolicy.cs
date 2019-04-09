namespace EventStore.Projections
{
    using System;
    using System.Threading;

    class RetryPolicy
    {
        readonly Func<int, TimeSpan> _strategy;

        public int RetryLimit { get; }

        internal static RetryPolicy Default => new RetryPolicy(attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)), 5);
        
        internal static RetryPolicy None => new RetryPolicy(attempt => TimeSpan.FromSeconds(0), 0);

        internal RetryPolicy(Func<int, TimeSpan> strategy, int retryLimit)
        {
            Guard.AgainstNullArgument(nameof(strategy), strategy);
    
            _strategy = strategy;
            RetryLimit = retryLimit;
        }

        public TimeSpan CurrentWaitTime(int retryCount) => _strategy(retryCount);
        
        public void Wait(int retryCount)
        {
            Thread.Sleep(_strategy(retryCount));
        }
    }
}
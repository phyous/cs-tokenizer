using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace CsTokenizer.Diagnostics
{
    public class TokenizerMetrics
    {
        private readonly ILogger<TokenizerMetrics> _logger;
        private readonly ConcurrentDictionary<string, long> _tokenCounts;
        private readonly ConcurrentDictionary<string, Stopwatch> _operationTimers;
        private long _totalTokensProcessed;
        private long _cacheHits;
        private long _cacheMisses;

        public TokenizerMetrics(ILogger<TokenizerMetrics> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tokenCounts = new ConcurrentDictionary<string, long>();
            _operationTimers = new ConcurrentDictionary<string, Stopwatch>();
        }

        public void IncrementTokenCount(string token)
        {
            _tokenCounts.AddOrUpdate(token, 1, (_, count) => count + 1);
            Interlocked.Increment(ref _totalTokensProcessed);
        }

        public void RecordCacheHit() => Interlocked.Increment(ref _cacheHits);
        public void RecordCacheMiss() => Interlocked.Increment(ref _cacheMisses);

        public IDisposable MeasureOperation(string operationName)
        {
            var timer = new Stopwatch();
            _operationTimers.TryAdd(operationName, timer);
            timer.Start();

            return new OperationTimer(() =>
            {
                timer.Stop();
                _logger.LogInformation(
                    "{Operation} took {ElapsedMs}ms",
                    operationName,
                    timer.ElapsedMilliseconds);
            });
        }

        public void LogMetrics()
        {
            var cacheHitRate = _cacheHits + _cacheMisses > 0
                ? (double)_cacheHits / (_cacheHits + _cacheMisses)
                : 0;

            _logger.LogInformation(
                "Tokenizer Metrics:\n" +
                "Total Tokens Processed: {TotalTokens}\n" +
                "Cache Hit Rate: {CacheHitRate:P2}\n" +
                "Unique Tokens: {UniqueTokens}",
                _totalTokensProcessed,
                cacheHitRate,
                _tokenCounts.Count);

            foreach (var (operation, timer) in _operationTimers)
            {
                _logger.LogInformation(
                    "{Operation} - Total Time: {TotalMs}ms",
                    operation,
                    timer.ElapsedMilliseconds);
            }
        }

        private class OperationTimer : IDisposable
        {
            private readonly Action _onDispose;

            public OperationTimer(Action onDispose)
            {
                _onDispose = onDispose;
            }

            public void Dispose()
            {
                _onDispose?.Invoke();
            }
        }
    }
} 
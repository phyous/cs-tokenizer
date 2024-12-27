using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CsTokenizer.Models;
using CsTokenizer.Diagnostics;
using CsTokenizer.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading;

namespace CsTokenizer.Implementation
{
    public class TokenCache
    {
        private readonly ConcurrentDictionary<string, CachedTokenSequence> _cache;
        private readonly TokenizerMetrics _metrics;
        private readonly int _maxSize;
        private readonly ILogger<TokenCache> _logger;
        private readonly object _evictionLock = new();

        public TokenCache(TokenizerMetrics metrics, ILogger<TokenCache>? logger = null, int maxSize = 10000)
        {
            _cache = new ConcurrentDictionary<string, CachedTokenSequence>();
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _maxSize = maxSize;
            _logger = logger ?? NullLogger<TokenCache>.Instance;
        }

        public bool TryGetTokens(string text, out IReadOnlyList<Token> tokens)
        {
            tokens = Array.Empty<Token>();

            if (string.IsNullOrEmpty(text))
                return false;

            if (_cache.TryGetValue(text, out var cached))
            {
                cached.IncrementHits();
                tokens = cached.Tokens;
                _metrics.RecordCacheHit();
                return true;
            }

            _metrics.RecordCacheMiss();
            return false;
        }

        public void CacheTokens(string text, IReadOnlyList<Token> tokens)
        {
            if (string.IsNullOrEmpty(text) || tokens == null)
                return;

            if (_cache.Count >= _maxSize)
            {
                lock (_evictionLock)
                {
                    if (_cache.Count >= _maxSize)
                    {
                        // Create a snapshot of the cache entries
                        var entries = _cache.ToArray();
                        var leastUsed = entries
                            .OrderBy(kvp => kvp.Value.HitCount)
                            .Take(entries.Length - _maxSize + 1)
                            .ToArray();

                        foreach (var entry in leastUsed)
                        {
                            _cache.TryRemove(entry.Key, out _);
                        }

                        _logger.LogInformation(
                            "Cache cleanup: removed {Count} entries",
                            leastUsed.Length);
                    }
                }
            }

            _cache.TryAdd(text, new CachedTokenSequence(tokens));
        }

        public void Clear()
        {
            _cache.Clear();
            _logger.LogInformation("Token cache cleared");
        }

        private class CachedTokenSequence
        {
            public IReadOnlyList<Token> Tokens { get; }
            private int _hitCount;
            public int HitCount => _hitCount;
            public DateTime LastAccessed { get; private set; }

            public CachedTokenSequence(IReadOnlyList<Token> tokens)
            {
                Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
                _hitCount = 1;
                LastAccessed = DateTime.UtcNow;
            }

            public void IncrementHits()
            {
                Interlocked.Increment(ref _hitCount);
                LastAccessed = DateTime.UtcNow;
            }
        }
    }
} 
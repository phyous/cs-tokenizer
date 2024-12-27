using System;
using System.Collections.Generic;
using System.Linq;
using CsTokenizer.Diagnostics;
using CsTokenizer.Implementation;
using CsTokenizer.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CsTokenizer.Tests
{
    [TestClass]
    public class TokenCacheTests
    {
        private TokenCache _cache = null!;
        private TokenizerMetrics _metrics = null!;
        private const int MaxSize = 5;

        [TestInitialize]
        public void Setup()
        {
            _metrics = new TokenizerMetrics(NullLogger<TokenizerMetrics>.Instance);
            _cache = new TokenCache(_metrics, NullLogger<TokenCache>.Instance, MaxSize);
        }

        [TestMethod]
        public void TestCacheHit()
        {
            // Arrange
            var text = "hello";
            var tokens = new[] { new Token("hello", 1) };
            _cache.CacheTokens(text, tokens);

            // Act
            var success = _cache.TryGetTokens(text, out var cachedTokens);

            // Assert
            Assert.IsTrue(success);
            Assert.IsNotNull(cachedTokens);
            Assert.AreEqual(1, cachedTokens.Count);
            Assert.AreEqual(tokens[0].Value, cachedTokens[0].Value);
        }

        [TestMethod]
        public void TestCacheMiss()
        {
            // Arrange
            var text = "hello";

            // Act
            var success = _cache.TryGetTokens(text, out var tokens);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNotNull(tokens);
            Assert.AreEqual(0, tokens.Count);
        }

        [TestMethod]
        public void TestEvictionPolicy()
        {
            // Arrange - Fill cache beyond max size
            for (int i = 0; i < MaxSize + 2; i++)
            {
                var text = $"text{i}";
                var tokens = new[] { new Token(text, i) };
                _cache.CacheTokens(text, tokens);

                // Access some items more frequently to affect LFU order
                if (i < 2)
                {
                    _cache.TryGetTokens(text, out _);
                    _cache.TryGetTokens(text, out _);
                }
            }

            // Act & Assert
            // Most frequently accessed items should still be in cache
            Assert.IsTrue(_cache.TryGetTokens("text0", out _));
            Assert.IsTrue(_cache.TryGetTokens("text1", out _));
            // Some items should be evicted due to cache size limit
            var evictedCount = 0;
            for (int i = 2; i < MaxSize + 2; i++)
            {
                if (!_cache.TryGetTokens($"text{i}", out _))
                    evictedCount++;
            }
            Assert.IsTrue(evictedCount > 0, "No items were evicted from cache");
        }

        [TestMethod]
        public void TestClearCache()
        {
            // Arrange
            var text = "hello";
            var tokens = new[] { new Token(text, 1) };
            _cache.CacheTokens(text, tokens);

            // Act
            _cache.Clear();
            var success = _cache.TryGetTokens(text, out _);

            // Assert
            Assert.IsFalse(success);
        }

        [TestMethod]
        public void TestNullOrEmptyInput()
        {
            // Act & Assert
            Assert.IsFalse(_cache.TryGetTokens(null!, out _));
            Assert.IsFalse(_cache.TryGetTokens(string.Empty, out _));
            
            // Should not throw
            _cache.CacheTokens(null!, Array.Empty<Token>());
            _cache.CacheTokens(string.Empty, Array.Empty<Token>());
        }

        [TestMethod]
        public void TestCacheHitUpdatesMetrics()
        {
            // Arrange
            var text = "hello";
            var tokens = new[] { new Token(text, 1) };
            _cache.CacheTokens(text, tokens);

            // Act
            _cache.TryGetTokens(text, out _);
            _cache.TryGetTokens(text, out _);
            _cache.TryGetTokens("missing", out _);

            // Assert - Check metrics through LogMetrics
            _metrics.LogMetrics(); // This will log the hit rate which we can't directly assert
        }

        [TestMethod]
        public void TestMultipleAccesses()
        {
            // Arrange
            var text = "hello";
            var tokens = new[] { new Token(text, 1) };
            _cache.CacheTokens(text, tokens);

            // Act - Access multiple times
            for (int i = 0; i < 5; i++)
            {
                var success = _cache.TryGetTokens(text, out var cachedTokens);
                Assert.IsTrue(success);
                Assert.IsNotNull(cachedTokens);
                Assert.AreEqual(1, cachedTokens.Count);
            }
        }

        [TestMethod]
        public void TestConcurrentAccess()
        {
            // Arrange
            var texts = Enumerable.Range(0, 100)
                .Select(i => $"text{i}")
                .ToList();

            // Act - Simulate concurrent access
            Parallel.ForEach(texts, text =>
            {
                var tokens = new[] { new Token(text, 1) };
                _cache.CacheTokens(text, tokens);
                _cache.TryGetTokens(text, out _);
            });

            // No assertions needed - just verifying no exceptions occur
        }
    }
} 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CsTokenizer.Configuration;
using CsTokenizer.Implementation;
using CsTokenizer.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CsTokenizer.Tests
{
    [TestClass]
    public class VocabularyBuilderTests
    {
        private VocabularyBuilder _builder = null!;
        private TokenizerConfiguration _config = null!;
        private BytePairEncoder _encoder = null!;

        [TestInitialize]
        public void Setup()
        {
            _config = new TokenizerConfiguration
            {
                MaxVocabularySize = 1000,
                SpecialTokens = "<|endoftext|>"
            };
            var vocabulary = new VocabularyManager();
            _encoder = new BytePairEncoder(vocabulary, _config);
            _builder = new VocabularyBuilder(_config, _encoder);
        }

        [TestMethod]
        public async Task TestBuildFromCorpus()
        {
            // Arrange
            var corpus = new[] { "hello world", "hello there", "world peace" }
                .ToAsyncEnumerable();
            var targetSize = 10;

            // Act
            var vocabulary = await _builder.BuildFromCorpusAsync(corpus, targetSize);

            // Assert
            Assert.IsNotNull(vocabulary);
            Assert.IsTrue(vocabulary.Size > 0);
            Assert.IsTrue(vocabulary.Size <= targetSize);
            Assert.IsTrue(vocabulary.Contains("<|endoftext|>"));
        }

        [TestMethod]
        public async Task TestBuildWithEmptyCorpus()
        {
            // Arrange
            var corpus = Array.Empty<string>().ToAsyncEnumerable();
            var targetSize = 10;

            // Act
            var vocabulary = await _builder.BuildFromCorpusAsync(corpus, targetSize);

            // Assert
            Assert.IsNotNull(vocabulary);
            Assert.IsTrue(vocabulary.Size > 0); // Should at least contain special tokens
            Assert.IsTrue(vocabulary.Contains("<|endoftext|>"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task TestBuildWithInvalidTargetSize()
        {
            // Arrange
            var corpus = new[] { "test" }.ToAsyncEnumerable();
            var targetSize = 0;

            // Act
            await _builder.BuildFromCorpusAsync(corpus, targetSize);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task TestBuildExceedingMaxSize()
        {
            // Arrange
            var corpus = new[] { "test" }.ToAsyncEnumerable();
            var targetSize = _config.MaxVocabularySize + 1;

            // Act
            await _builder.BuildFromCorpusAsync(corpus, targetSize);
        }

        [TestMethod]
        public void TestUpdateVocabulary()
        {
            // Arrange
            var vocabulary = new VocabularyManager();
            var newTokens = new[] { "hello", "world" };

            // Act
            _builder.UpdateVocabulary(vocabulary, newTokens);

            // Assert
            Assert.IsTrue(vocabulary.Contains("hello"));
            Assert.IsTrue(vocabulary.Contains("world"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestUpdateWithNullVocabulary()
        {
            _builder.UpdateVocabulary(null!, new[] { "test" });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestUpdateWithNullTokens()
        {
            _builder.UpdateVocabulary(new VocabularyManager(), null!);
        }

        [TestMethod]
        public void TestUpdateWithEmptyTokens()
        {
            // Arrange
            var vocabulary = new VocabularyManager();
            var initialSize = vocabulary.Size;

            // Act
            _builder.UpdateVocabulary(vocabulary, Array.Empty<string>());

            // Assert
            Assert.AreEqual(initialSize, vocabulary.Size);
        }

        [TestMethod]
        public void TestUpdateWithWhitespaceTokens()
        {
            // Arrange
            var vocabulary = new VocabularyManager();
            var newTokens = new[] { " ", "\t", "\n" };

            // Act
            _builder.UpdateVocabulary(vocabulary, newTokens);

            // Assert
            Assert.IsTrue(vocabulary.Contains(" "));
            Assert.IsTrue(vocabulary.Contains("\t"));
            Assert.IsTrue(vocabulary.Contains("\n"));
        }
    }

    internal static class AsyncEnumerableExtensions
    {
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
            this IEnumerable<T> source,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var item in source)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1, cancellationToken);
                yield return item;
            }
        }
    }
} 
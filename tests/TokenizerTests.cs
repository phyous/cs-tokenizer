using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CsTokenizer.Configuration;
using CsTokenizer.Implementation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CsTokenizer.Tests
{
    [TestClass]
    public class TokenizerTests
    {
        private Tokenizer _tokenizer = null!;
        private TokenizerConfiguration _config = null!;

        [TestInitialize]
        public void Setup()
        {
            _config = new TokenizerConfiguration
            {
                MaxVocabularySize = 1000,
                MaxTokenLength = 1024,
                EnableCaching = true,
                ParallelizationThreshold = 100,
                SpecialTokens = "<|endoftext|>"
            };
            _tokenizer = new Tokenizer(_config);
        }

        [TestMethod]
        public async Task TestBasicTokenization()
        {
            // Arrange
            var input = "Hello, world!";

            // Act
            var tokens = await _tokenizer.EncodeAsync(input);
            var decoded = await _tokenizer.DecodeAsync(tokens);

            // Assert
            Assert.IsNotNull(tokens);
            Assert.IsTrue(tokens.Count > 0);
            Assert.AreEqual(input, decoded);
        }

        [TestMethod]
        public async Task TestEmptyInput()
        {
            // Arrange
            var input = string.Empty;

            // Act
            var tokens = await _tokenizer.EncodeAsync(input);
            var decoded = await _tokenizer.DecodeAsync(tokens);

            // Assert
            Assert.IsNotNull(tokens);
            Assert.AreEqual(0, tokens.Count);
            Assert.AreEqual(input, decoded);
        }

        [TestMethod]
        public async Task TestUnicodeHandling()
        {
            // Arrange
            var input = "Hello, 世界! 🌍";

            // Act
            var tokens = await _tokenizer.EncodeAsync(input);
            var decoded = await _tokenizer.DecodeAsync(tokens);

            // Assert
            Assert.IsNotNull(tokens);
            Assert.IsTrue(tokens.Count > 0);
            Assert.AreEqual(input, decoded);
        }

        [TestMethod]
        public async Task TestSpecialTokens()
        {
            // Arrange
            var input = "<|endoftext|>";

            // Act
            var tokens = await _tokenizer.EncodeAsync(input);
            var decoded = await _tokenizer.DecodeAsync(tokens);

            // Assert
            Assert.IsNotNull(tokens);
            Assert.AreEqual(1, tokens.Count);
            Assert.IsTrue(tokens[0].IsSpecial);
            Assert.AreEqual(input, decoded);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task TestMaxLengthExceeded()
        {
            // Arrange
            var input = new string('a', _config.MaxTokenLength + 1);

            // Act
            await _tokenizer.EncodeAsync(input);
        }

        [TestMethod]
        public async Task TestStreamProcessing()
        {
            // Arrange
            var inputs = new[] { "Hello", ", ", "world", "!" };
            var stream = ToAsyncEnumerable(inputs);
            var expectedText = string.Concat(inputs);

            // Act
            var progress = await _tokenizer.EncodeStreamAsync(stream);
            var tokens = new List<Models.Token>();
            
            await foreach (var text in stream)
            {
                var textTokens = await _tokenizer.EncodeAsync(text);
                tokens.AddRange(textTokens);
            }

            var decoded = await _tokenizer.DecodeAsync(tokens);

            // Assert
            Assert.IsNotNull(progress);
            Assert.AreEqual(expectedText, decoded);
        }

        [TestMethod]
        public async Task TestParallelProcessing()
        {
            // Arrange
            var input = new string('a', _config.ParallelizationThreshold * 2);

            // Act
            var tokens = await _tokenizer.EncodeAsync(input);
            var decoded = await _tokenizer.DecodeAsync(tokens);

            // Assert
            Assert.IsNotNull(tokens);
            Assert.IsTrue(tokens.Count > 0);
            Assert.AreEqual(input, decoded);
        }

        private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
            IEnumerable<T> source,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var item in source)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1, cancellationToken); // Add minimal delay to make it truly async
                yield return item;
            }
        }
    }
} 
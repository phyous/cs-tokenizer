using System;
using System.Text;
using System.Threading.Tasks;
using CsTokenizer.Configuration;
using CsTokenizer.Implementation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CsTokenizer.Tests
{
    [TestClass]
    public class UnicodeHandlingTests
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
                PreserveWhitespace = true
            };
            _tokenizer = new Tokenizer(_config);
        }

        [TestMethod]
        public async Task TestSurrogatePairs()
        {
            // Arrange
            var input = "Hello 🌍 World 🚀"; // Earth globe and rocket emojis (surrogate pairs)

            // Act
            var tokens = await _tokenizer.EncodeAsync(input);
            var decoded = await _tokenizer.DecodeAsync(tokens);

            // Assert
            Assert.AreEqual(input, decoded);
            Assert.IsTrue(tokens.Count > 0);
        }

        [TestMethod]
        public async Task TestZeroWidthCharacters()
        {
            // Arrange
            var input = "Hello\u200BWorld"; // Zero-width space
            var inputWithJoiner = "Hello\u200DWorld"; // Zero-width joiner

            // Act
            var tokens1 = await _tokenizer.EncodeAsync(input);
            var decoded1 = await _tokenizer.DecodeAsync(tokens1);
            var tokens2 = await _tokenizer.EncodeAsync(inputWithJoiner);
            var decoded2 = await _tokenizer.DecodeAsync(tokens2);

            // Assert
            Assert.AreEqual(input, decoded1);
            Assert.AreEqual(inputWithJoiner, decoded2);
        }

        [TestMethod]
        public async Task TestMixedScripts()
        {
            // Arrange
            var input = "Hello 世界! Café"; // English, Chinese, French

            // Act
            var tokens = await _tokenizer.EncodeAsync(input);
            var decoded = await _tokenizer.DecodeAsync(tokens);

            // Assert
            Assert.AreEqual(input, decoded);
        }

        [TestMethod]
        public async Task TestCombiningCharacters()
        {
            // Arrange
            var input = "e\u0301"; // é composed of 'e' and combining acute accent
            var normalized = input.Normalize(NormalizationForm.FormC);

            // Act
            var tokens = await _tokenizer.EncodeAsync(input);
            var decoded = await _tokenizer.DecodeAsync(tokens);

            // Assert
            Assert.AreEqual(input, decoded);
            Assert.AreNotEqual(normalized.Length, input.Length);
        }

        [TestMethod]
        public async Task TestBidirectionalText()
        {
            // Arrange
            var input = "Hello! مرحبا"; // English and Arabic

            // Act
            var tokens = await _tokenizer.EncodeAsync(input);
            var decoded = await _tokenizer.DecodeAsync(tokens);

            // Assert
            Assert.AreEqual(input, decoded);
        }

        [TestMethod]
        public async Task TestSpecialUnicodeRanges()
        {
            // Arrange
            var builder = new StringBuilder();
            // Add characters from different Unicode blocks
            builder.Append("Hello "); // Basic Latin
            builder.Append("Γειά σας "); // Greek
            builder.Append("こんにちは "); // Hiragana
            builder.Append("안녕하세요 "); // Hangul
            builder.Append("שָׁלוֹם"); // Hebrew with points

            var input = builder.ToString();

            // Act
            var tokens = await _tokenizer.EncodeAsync(input);
            var decoded = await _tokenizer.DecodeAsync(tokens);

            // Assert
            Assert.AreEqual(input, decoded);
        }

        [TestMethod]
        public async Task TestEmojiSequences()
        {
            // Arrange
            var input = "Family: 👨‍👩‍👧‍👦 Activities: 🏃‍♀️ 🏊‍♂️ Flags: 🏳️‍🌈";

            // Act
            var tokens = await _tokenizer.EncodeAsync(input);
            var decoded = await _tokenizer.DecodeAsync(tokens);

            // Assert
            Assert.AreEqual(input, decoded);
        }

        [TestMethod]
        public async Task TestControlCharacters()
        {
            // Arrange
            var input = "Line1\r\nLine2\tTabbed\u0007Bell";

            // Act
            var tokens = await _tokenizer.EncodeAsync(input);
            var decoded = await _tokenizer.DecodeAsync(tokens);

            // Assert
            Assert.AreEqual(input, decoded);
        }
    }
} 
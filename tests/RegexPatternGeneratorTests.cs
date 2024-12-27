using System;
using System.Text.RegularExpressions;
using CsTokenizer.Configuration;
using CsTokenizer.Implementation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CsTokenizer.Tests
{
    [TestClass]
    public class RegexPatternGeneratorTests
    {
        private RegexPatternGenerator _generator = null!;
        private TokenizerConfiguration _config = null!;

        [TestInitialize]
        public void Setup()
        {
            _config = new TokenizerConfiguration
            {
                PreserveWhitespace = true
            };
            _generator = new RegexPatternGenerator(_config);
        }

        [TestMethod]
        public void TestNormalTokenPattern()
        {
            // Arrange
            var token = "hello";

            // Act
            var pattern = _generator.GetOrCreatePattern(token);

            // Assert
            Assert.IsNotNull(pattern);
            Assert.IsTrue(pattern.IsMatch("hello"));
            Assert.IsFalse(pattern.IsMatch("helo"));
        }

        [TestMethod]
        public void TestSpecialTokenPattern()
        {
            // Arrange
            var token = "<|endoftext|>";

            // Act
            var pattern = _generator.GetOrCreatePattern(token);

            // Assert
            Assert.IsNotNull(pattern);
            Assert.IsTrue(pattern.IsMatch("<|endoftext|>"));
            Assert.IsFalse(pattern.IsMatch("endoftext"));
            Assert.IsFalse(pattern.IsMatch("<|endoftext|>extra"));
        }

        [TestMethod]
        public void TestWhitespaceTokenPattern()
        {
            // Arrange
            var token = " ";

            // Act
            var pattern = _generator.GetOrCreatePattern(token);

            // Assert
            Assert.IsNotNull(pattern);
            Assert.IsTrue(pattern.IsMatch(" "));
            Assert.IsTrue(pattern.IsMatch("  "));
        }

        [TestMethod]
        public void TestPatternCaching()
        {
            // Arrange
            var token = "test";

            // Act
            var pattern1 = _generator.GetOrCreatePattern(token);
            var pattern2 = _generator.GetOrCreatePattern(token);

            // Assert
            Assert.IsTrue(ReferenceEquals(pattern1, pattern2), "Pattern should be cached and return same instance");
        }

        [TestMethod]
        public void TestClearCache()
        {
            // Arrange
            var token = "test";
            var pattern1 = _generator.GetOrCreatePattern(token);

            // Act
            _generator.ClearCache();
            var pattern2 = _generator.GetOrCreatePattern(token);

            // Assert
            Assert.IsFalse(ReferenceEquals(pattern1, pattern2), "After clearing cache, should get new instance");
        }

        [TestMethod]
        public void TestEscapeSpecialCharacters()
        {
            // Arrange
            var token = "hello.world*";

            // Act
            var pattern = _generator.GetOrCreatePattern(token);

            // Assert
            Assert.IsNotNull(pattern);
            Assert.IsTrue(pattern.IsMatch("hello.world*"));
            Assert.IsFalse(pattern.IsMatch("hello-world"));
            Assert.IsFalse(pattern.IsMatch("helloxworld"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestNullToken()
        {
            _generator.GetOrCreatePattern(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestEmptyToken()
        {
            _generator.GetOrCreatePattern(string.Empty);
        }
    }
} 
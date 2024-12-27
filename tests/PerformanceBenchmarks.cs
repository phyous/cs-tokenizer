using System;
using System.Text;
using System.Threading.Tasks;
using CsTokenizer.Configuration;
using CsTokenizer.Implementation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CsTokenizer.Tests
{
    [TestClass]
    public class PerformanceBenchmarks
    {
        private const int LargeInputSize = 1_000_000;
        private const int MaxAllowedMemoryMultiplier = 100;
        private const int MemoryTestInputSize = LargeInputSize / 100;

        [TestMethod]
        public async Task BenchmarkLargeInput()
        {
            var config = new TokenizerConfiguration
            {
                MaxTokenLength = LargeInputSize * 2
            };
            var tokenizer = new Tokenizer(config);
            var input = GenerateLargeInput(LargeInputSize);

            var startTime = DateTime.UtcNow;
            var tokens = await tokenizer.EncodeAsync(input);
            var duration = DateTime.UtcNow - startTime;

            Assert.IsTrue(duration.TotalMilliseconds < 1000, $"Encoding took {duration.TotalMilliseconds}ms");
            Assert.IsNotNull(tokens);
            Assert.IsTrue(tokens.Count > 0);
        }

        [TestMethod]
        public async Task BenchmarkMemoryUsage()
        {
            var config = new TokenizerConfiguration
            {
                MaxTokenLength = MemoryTestInputSize * 2
            };
            var tokenizer = new Tokenizer(config);
            var input = GenerateLargeInput(MemoryTestInputSize);
            var inputBytes = Encoding.UTF8.GetByteCount(input);
            var maxAllowedMemory = inputBytes * MaxAllowedMemoryMultiplier;

            var startMemory = GC.GetTotalMemory(true);
            var tokens = await tokenizer.EncodeAsync(input);
            var endMemory = GC.GetTotalMemory(false);
            var memoryUsed = endMemory - startMemory;

            Assert.IsTrue(memoryUsed <= maxAllowedMemory, 
                $"Memory usage of {memoryUsed} bytes exceeds the maximum allowed of {maxAllowedMemory} bytes");
            Assert.IsNotNull(tokens);
            Assert.IsTrue(tokens.Count > 0);
        }

        private static string GenerateLargeInput(int size)
        {
            var sb = new StringBuilder(size);
            var words = new[] { "hello", "world", "this", "is", "a", "test", "of", "the", "tokenizer" };
            var random = new Random(42);

            while (sb.Length < size)
            {
                sb.Append(words[random.Next(words.Length)]).Append(' ');
            }

            return sb.ToString();
        }
    }
} 
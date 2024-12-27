using System;

namespace CsTokenizer.Configuration
{
    public class TokenizerConfiguration
    {
        public int MaxVocabularySize { get; set; } = 100000;
        public int MaxTokenLength { get; set; } = 1024;
        public bool EnableCaching { get; set; } = true;
        public int ParallelizationThreshold { get; set; } = 1000;
        public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMinutes(5);
        public string SpecialTokens { get; set; } = "<|endoftext|>";
        public bool PreserveWhitespace { get; set; } = true;

        public static TokenizerConfiguration Default => new();

        public TokenizerConfiguration Clone()
        {
            return new TokenizerConfiguration
            {
                MaxVocabularySize = MaxVocabularySize,
                MaxTokenLength = MaxTokenLength,
                EnableCaching = EnableCaching,
                ParallelizationThreshold = ParallelizationThreshold,
                OperationTimeout = OperationTimeout,
                SpecialTokens = SpecialTokens,
                PreserveWhitespace = PreserveWhitespace
            };
        }
    }
} 
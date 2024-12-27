using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CsTokenizer.Models;

namespace CsTokenizer.Interfaces
{
    public record TokenizationProgress
    {
        public int ProcessedTokens { get; init; }
        public int TotalTokens { get; init; }
        public double PercentageComplete => TotalTokens > 0 ? (double)ProcessedTokens / TotalTokens * 100 : 0;
    }

    public interface ITokenizer
    {
        Task<IReadOnlyList<Token>> EncodeAsync(
            string text,
            CancellationToken cancellationToken = default);
            
        Task<string> DecodeAsync(
            IReadOnlyList<Token> tokens,
            CancellationToken cancellationToken = default);
            
        Task<IProgress<TokenizationProgress>> EncodeStreamAsync(
            IAsyncEnumerable<string> textStream,
            CancellationToken cancellationToken = default);
    }
} 
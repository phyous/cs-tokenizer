using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CsTokenizer.Models;

namespace CsTokenizer.Interfaces
{
    public interface IBytePairEncoder
    {
        Task<IReadOnlyList<Token>> EncodeAsync(
            string text,
            CancellationToken cancellationToken = default);

        IEnumerable<TokenPair> FindMostFrequentPairs(
            IEnumerable<Token> tokens);

        void ApplyMerges(
            List<Token> tokens,
            IEnumerable<MergeRule> rules);

        Task<IReadOnlyList<MergeRule>> LearnMergeRulesAsync(
            IAsyncEnumerable<string> corpus,
            int numMerges,
            CancellationToken cancellationToken = default);
    }
} 
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CsTokenizer.Models;

namespace CsTokenizer.Interfaces
{
    public interface IVocabulary
    {
        int Size { get; }
        bool Contains(string token);
        bool TryGetTokenId(string token, out int id);
        bool TryGetToken(int id, out Token token);
        IReadOnlyDictionary<string, int> GetVocabulary();
        Task SaveAsync(string path, CancellationToken cancellationToken = default);
        Task LoadAsync(string path, CancellationToken cancellationToken = default);
    }

    public interface IVocabularyBuilder
    {
        Task<IVocabulary> BuildFromCorpusAsync(
            IAsyncEnumerable<string> corpus,
            int targetSize,
            CancellationToken cancellationToken = default);
        
        void UpdateVocabulary(
            IVocabulary vocabulary,
            IEnumerable<string> newTokens);
    }
} 
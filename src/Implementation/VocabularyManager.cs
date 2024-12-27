using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CsTokenizer.Interfaces;
using CsTokenizer.Models;

namespace CsTokenizer.Implementation
{
    public class VocabularyManager : IVocabulary
    {
        private readonly ConcurrentDictionary<string, int> _tokenToId;
        private readonly ConcurrentDictionary<int, Token> _idToToken;
        private readonly List<MergeRule> _mergeRules;
        private readonly object _mergeRulesLock = new();
        private int _nextId;

        public VocabularyManager()
        {
            _tokenToId = new ConcurrentDictionary<string, int>();
            _idToToken = new ConcurrentDictionary<int, Token>();
            _mergeRules = new List<MergeRule>();
            _nextId = 0;
        }

        public int Size => _tokenToId.Count;

        public bool Contains(string token) =>
            !string.IsNullOrEmpty(token) && _tokenToId.ContainsKey(token);

        public bool TryGetTokenId(string token, out int id)
        {
            id = 0;
            return !string.IsNullOrEmpty(token) && _tokenToId.TryGetValue(token, out id);
        }

        public bool TryGetToken(int id, out Token token)
        {
            token = null!;
            return _idToToken.TryGetValue(id, out var foundToken) && (token = foundToken) != null;
        }

        public IReadOnlyDictionary<string, int> GetVocabulary() => _tokenToId;

        public async Task SaveAsync(string path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            var vocabulary = _tokenToId.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            var json = JsonSerializer.Serialize(vocabulary);
            await File.WriteAllTextAsync(path, json, cancellationToken);
        }

        public async Task LoadAsync(string path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            if (!File.Exists(path))
                throw new FileNotFoundException("Vocabulary file not found", path);

            var json = await File.ReadAllTextAsync(path, cancellationToken);
            var vocabulary = JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                ?? throw new InvalidOperationException("Failed to deserialize vocabulary");

            _tokenToId.Clear();
            _idToToken.Clear();

            foreach (var (token, id) in vocabulary)
            {
                if (!string.IsNullOrEmpty(token))
                {
                    _tokenToId[token] = id;
                    _idToToken[id] = new Token(token, id);
                    _nextId = Math.Max(_nextId, id + 1);
                }
            }
        }

        public IReadOnlyList<MergeRule> GetMergeRules()
        {
            lock (_mergeRulesLock)
            {
                return _mergeRules.ToList();
            }
        }

        public void AddMergeRule(MergeRule rule)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            lock (_mergeRulesLock)
            {
                _mergeRules.Add(rule);
            }
        }

        public void ClearMergeRules()
        {
            lock (_mergeRulesLock)
            {
                _mergeRules.Clear();
            }
        }

        internal int AddToken(string token, bool isSpecial = false)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token));

            return _tokenToId.GetOrAdd(token, _ =>
            {
                var id = Interlocked.Increment(ref _nextId) - 1;
                var newToken = new Token(token, id, isSpecial);
                if (_idToToken.TryAdd(id, newToken))
                    return id;

                throw new InvalidOperationException("Failed to add token to vocabulary");
            });
        }
    }
} 
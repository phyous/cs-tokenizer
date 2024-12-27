using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsTokenizer.Interfaces;
using CsTokenizer.Models;
using CsTokenizer.Configuration;

namespace CsTokenizer.Implementation
{
    public class BytePairEncoder : IBytePairEncoder
    {
        private readonly VocabularyManager _vocabulary;
        private readonly TokenizerConfiguration _config;

        public BytePairEncoder(VocabularyManager vocabulary, TokenizerConfiguration config)
        {
            _vocabulary = vocabulary ?? throw new ArgumentNullException(nameof(vocabulary));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task<IReadOnlyList<Token>> EncodeAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<Token>();

            // Check for special tokens first
            if (_vocabulary.TryGetTokenId(text, out var specialId) && _vocabulary.TryGetToken(specialId, out var specialToken) && specialToken.IsSpecial)
            {
                return new[] { specialToken };
            }

            // Initial tokenization: each byte becomes a token
            var tokens = await Task.Run(() => InitialTokenize(text), cancellationToken);

            // Apply existing merge rules
            await Task.Run(() => ApplyMerges(tokens, _vocabulary.GetMergeRules()), cancellationToken);

            return tokens;
        }

        private List<Token> InitialTokenize(string text)
        {
            var tokens = new List<Token>();
            var enumerator = System.Globalization.StringInfo.GetTextElementEnumerator(text);

            while (enumerator.MoveNext())
            {
                var grapheme = enumerator.GetTextElement();
                if (_vocabulary.TryGetTokenId(grapheme, out var id))
                {
                    if (_vocabulary.TryGetToken(id, out var token))
                        tokens.Add(token);
                }
                else
                {
                    var newId = _vocabulary.AddToken(grapheme);
                    if (_vocabulary.TryGetToken(newId, out var token))
                        tokens.Add(token);
                }
            }

            return tokens;
        }

        public IEnumerable<TokenPair> FindMostFrequentPairs(IEnumerable<Token> tokens)
        {
            var pairFrequencies = new Dictionary<(string, string), TokenPair>();
            var tokenList = tokens.ToList();

            for (var i = 0; i < tokenList.Count - 1; i++)
            {
                var pair = (tokenList[i].Value, tokenList[i + 1].Value);
                if (!pairFrequencies.TryGetValue(pair, out var tokenPair))
                {
                    tokenPair = new TokenPair(tokenList[i], tokenList[i + 1]);
                    pairFrequencies[pair] = tokenPair;
                }
                tokenPair.Frequency++;
            }

            return pairFrequencies.Values
                .OrderByDescending(p => p.Frequency)
                .ThenBy(p => p.First.Value)
                .ThenBy(p => p.Second.Value);
        }

        public void ApplyMerges(List<Token> tokens, IEnumerable<MergeRule> rules)
        {
            var changed = true;
            while (changed && tokens.Count > 1)
            {
                changed = false;
                foreach (var rule in rules)
                {
                    for (var i = 0; i < tokens.Count - 1; i++)
                    {
                        if (tokens[i].Value == rule.Pair.First.Value &&
                            tokens[i + 1].Value == rule.Pair.Second.Value)
                        {
                            var mergedValue = tokens[i].Value + tokens[i + 1].Value;
                            if (_vocabulary.TryGetTokenId(mergedValue, out var id))
                            {
                                if (_vocabulary.TryGetToken(id, out var mergedToken))
                                {
                                    tokens[i] = mergedToken;
                                    tokens.RemoveAt(i + 1);
                                    changed = true;
                                    i--;
                                }
                            }
                        }
                    }
                }
            }
        }

        public async Task<IReadOnlyList<MergeRule>> LearnMergeRulesAsync(
            IAsyncEnumerable<string> corpus,
            int numMerges,
            CancellationToken cancellationToken = default)
        {
            _vocabulary.ClearMergeRules();
            var mergeRules = new List<MergeRule>();
            var allTokens = new List<Token>();

            await foreach (var text in corpus.WithCancellation(cancellationToken))
            {
                var tokens = InitialTokenize(text);
                allTokens.AddRange(tokens);
            }

            while (mergeRules.Count < numMerges && allTokens.Count > 1)
            {
                var pairs = FindMostFrequentPairs(allTokens);
                var mostFrequent = pairs.FirstOrDefault();
                
                if (mostFrequent == null || mostFrequent.Frequency < 2)
                    break;

                var mergedValue = mostFrequent.First.Value + mostFrequent.Second.Value;
                var newId = _vocabulary.AddToken(mergedValue);

                if (_vocabulary.TryGetToken(newId, out var newToken))
                {
                    var rule = new MergeRule(mostFrequent, mergeRules.Count);
                    mergeRules.Add(rule);
                    _vocabulary.AddMergeRule(rule);
                    ApplyMerges(allTokens, new[] { rule });
                }
            }

            return mergeRules;
        }
    }
} 
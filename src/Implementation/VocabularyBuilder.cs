using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CsTokenizer.Configuration;
using CsTokenizer.Interfaces;
using CsTokenizer.Models;

namespace CsTokenizer.Implementation
{
    public class VocabularyBuilder : IVocabularyBuilder
    {
        private readonly TokenizerConfiguration _config;
        private readonly BytePairEncoder _encoder;

        public VocabularyBuilder(TokenizerConfiguration config, BytePairEncoder encoder)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
        }

        public async Task<IVocabulary> BuildFromCorpusAsync(
            IAsyncEnumerable<string> corpus,
            int targetSize,
            CancellationToken cancellationToken = default)
        {
            if (targetSize <= 0)
                throw new ArgumentException("Target size must be positive", nameof(targetSize));

            if (targetSize > _config.MaxVocabularySize)
                throw new ArgumentException($"Target size exceeds maximum vocabulary size of {_config.MaxVocabularySize}", nameof(targetSize));

            var vocabulary = new VocabularyManager();

            // Learn merge rules from corpus
            var mergeRules = await _encoder.LearnMergeRulesAsync(corpus, targetSize, cancellationToken);

            // Add special tokens first
            if (!string.IsNullOrEmpty(_config.SpecialTokens))
            {
                foreach (var token in _config.SpecialTokens.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        vocabulary.AddToken(token.Trim(), isSpecial: true);
                    }
                }
            }

            // Add merge rules to vocabulary
            foreach (var rule in mergeRules)
            {
                var mergedValue = rule.Pair.First.Value + rule.Pair.Second.Value;
                vocabulary.AddToken(mergedValue);
            }

            return vocabulary;
        }

        public void UpdateVocabulary(IVocabulary vocabulary, IEnumerable<string> newTokens)
        {
            if (vocabulary == null)
                throw new ArgumentNullException(nameof(vocabulary));

            if (newTokens == null)
                throw new ArgumentNullException(nameof(newTokens));

            var vocabManager = vocabulary as VocabularyManager 
                ?? throw new ArgumentException("Vocabulary must be a VocabularyManager instance", nameof(vocabulary));

            foreach (var token in newTokens)
            {
                if (token != null) // Allow empty strings but not null
                {
                    vocabManager.AddToken(token, isSpecial: false);
                }
            }
        }
    }
} 
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using CsTokenizer.Configuration;
using CsTokenizer.Interfaces;

namespace CsTokenizer.Implementation
{
    public class RegexPatternGenerator : IRegexPatternGenerator
    {
        private readonly ConcurrentDictionary<string, Regex> _patternCache;
        private readonly TokenizerConfiguration _config;

        public RegexPatternGenerator(TokenizerConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _patternCache = new ConcurrentDictionary<string, Regex>();
        }

        public Regex GetOrCreatePattern(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token));

            return _patternCache.GetOrAdd(token, CreatePattern);
        }

        private Regex CreatePattern(string token)
        {
            // Escape special regex characters
            var escaped = Regex.Escape(token);

            // Handle special tokens differently
            if (token.StartsWith("<|") && token.EndsWith("|>"))
            {
                // Special tokens must match exactly
                return new Regex($"^{escaped}$", RegexOptions.Compiled);
            }

            // For whitespace tokens, preserve whitespace if configured
            if (_config.PreserveWhitespace && string.IsNullOrWhiteSpace(token))
            {
                return new Regex($"({escaped})", RegexOptions.Compiled);
            }

            // For normal tokens, create a pattern that matches the token
            // with optional boundaries for better matching
            return new Regex($"({escaped})", RegexOptions.Compiled);
        }

        public void ClearCache()
        {
            _patternCache.Clear();
        }
    }
} 
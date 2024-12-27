using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using CsTokenizer.Configuration;
using CsTokenizer.Interfaces;
using CsTokenizer.Models;
using CsTokenizer.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CsTokenizer.Implementation
{
    public class Tokenizer : ITokenizer
    {
        private readonly TokenizerConfiguration _config;
        private readonly VocabularyManager _vocabulary;
        private readonly BytePairEncoder _encoder;
        private readonly TokenizerMetrics _metrics;
        private readonly ILogger<Tokenizer> _logger;
        private readonly object _lock = new();

        public Tokenizer(
            TokenizerConfiguration? config = null,
            ILogger<Tokenizer>? logger = null)
        {
            _config = config ?? TokenizerConfiguration.Default;
            _logger = logger ?? NullLogger<Tokenizer>.Instance;
            _metrics = new TokenizerMetrics(NullLogger<TokenizerMetrics>.Instance);
            _vocabulary = new VocabularyManager();
            _encoder = new BytePairEncoder(_vocabulary, _config);

            InitializeSpecialTokens();
        }

        private void InitializeSpecialTokens()
        {
            if (!string.IsNullOrEmpty(_config.SpecialTokens))
            {
                foreach (var token in _config.SpecialTokens.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        _vocabulary.AddToken(token.Trim(), isSpecial: true);
                    }
                }
            }
        }

        public async Task<IReadOnlyList<Token>> EncodeAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<Token>();

            using var operation = _metrics.MeasureOperation("Encode");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_config.OperationTimeout);

            try
            {
                if (text.Length > _config.MaxTokenLength)
                    throw new ArgumentException($"Input text exceeds maximum length of {_config.MaxTokenLength} characters");

                var tokens = new List<Token>();

                if (_config.ParallelizationThreshold > 0 && text.Length >= _config.ParallelizationThreshold)
                {
                    using var parallelOperation = _metrics.MeasureOperation("ParallelEncode");
                    var chunks = SplitIntoChunks(text);
                    var tasks = new List<Task<IReadOnlyList<Token>>>();

                    foreach (var chunk in chunks)
                    {
                        tasks.Add(_encoder.EncodeAsync(chunk, cts.Token));
                    }

                    var results = await Task.WhenAll(tasks);
                    foreach (var result in results)
                    {
                        tokens.AddRange(result);
                        foreach (var token in result)
                        {
                            _metrics.IncrementTokenCount(token.Value);
                        }
                    }
                }
                else
                {
                    var result = await _encoder.EncodeAsync(text, cts.Token);
                    tokens.AddRange(result);
                    foreach (var token in result)
                    {
                        _metrics.IncrementTokenCount(token.Value);
                    }
                }

                return tokens;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encode text");
                throw new TokenizationException("Failed to encode text", ex);
            }
        }

        public async Task<string> DecodeAsync(IReadOnlyList<Token> tokens, CancellationToken cancellationToken = default)
        {
            if (tokens == null || tokens.Count == 0)
                return string.Empty;

            using var operation = _metrics.MeasureOperation("Decode");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_config.OperationTimeout);

            try
            {
                return await Task.Run(() =>
                {
                    var result = new StringBuilder();
                    foreach (var token in tokens)
                    {
                        cts.Token.ThrowIfCancellationRequested();
                        result.Append(token.Value);
                    }
                    return result.ToString();
                }, cts.Token);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decode tokens");
                throw new TokenizationException("Failed to decode tokens", ex);
            }
        }

        public async Task<IProgress<TokenizationProgress>> EncodeStreamAsync(
            IAsyncEnumerable<string> textStream,
            CancellationToken cancellationToken = default)
        {
            if (textStream == null)
                throw new ArgumentNullException(nameof(textStream));

            var progress = new Progress<TokenizationProgress>();
            var processedTokens = 0;
            var totalTokens = 0;

            try
            {
                using var operation = _metrics.MeasureOperation("EncodeStream");
                await foreach (var text in textStream.WithCancellation(cancellationToken))
                {
                    var tokens = await EncodeAsync(text, cancellationToken);
                    processedTokens += tokens.Count;
                    totalTokens = Math.Max(totalTokens, processedTokens);

                    ((IProgress<TokenizationProgress>)progress).Report(new TokenizationProgress
                    {
                        ProcessedTokens = processedTokens,
                        TotalTokens = totalTokens
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encode stream");
                throw new TokenizationException("Failed to encode stream", ex);
            }

            return progress;
        }

        private IEnumerable<string> SplitIntoChunks(string text)
        {
            for (int i = 0; i < text.Length; i += _config.ParallelizationThreshold)
            {
                var length = Math.Min(_config.ParallelizationThreshold, text.Length - i);
                yield return text.Substring(i, length);
            }
        }

        public void LogMetrics() => _metrics.LogMetrics();
    }

    public class TokenizationException : Exception
    {
        public TokenizationException(string message) : base(message) { }
        public TokenizationException(string message, Exception innerException) : base(message, innerException) { }
    }
} 
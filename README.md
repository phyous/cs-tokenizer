# cs-tokenizer

A high-performance, thread-safe C# tokenizer library implementing Byte Pair Encoding (BPE) with support for Unicode, special tokens, and streaming operations.

## Features

- 🚀 High-performance BPE tokenization
- 🔤 Full Unicode support
- 🔒 Thread-safe operations
- 📊 Built-in metrics and diagnostics
- 🔄 Streaming support for large inputs
- ⚡ Parallel processing for large texts
- 💾 Vocabulary persistence
- 🎯 Configurable parameters

## Installation

Add the package to your project:

```bash
dotnet add package cs_tokenizer
```

## Quick Start

```csharp
using CsTokenizer.Implementation;
using CsTokenizer.Configuration;

// Create a tokenizer with default configuration
var tokenizer = new Tokenizer();

// Encode text to tokens
var tokens = await tokenizer.EncodeAsync("Hello, world! 👋");

// Decode tokens back to text
var text = await tokenizer.DecodeAsync(tokens);
```

## Configuration

Customize the tokenizer behavior:

```csharp
var config = new TokenizerConfiguration
{
    MaxVocabularySize = 50000,
    MaxTokenLength = 2048,
    EnableCaching = true,
    ParallelizationThreshold = 1000,
    SpecialTokens = "<|endoftext|>,<|pad|>,<|mask|>",
    OperationTimeout = TimeSpan.FromSeconds(30)
};

var tokenizer = new Tokenizer(config);
```

## Advanced Usage

### Streaming Large Inputs

Process large texts using streaming:

```csharp
async IAsyncEnumerable<string> GetTextChunks()
{
    yield return "This is ";
    yield return "a large ";
    yield return "text document.";
}

var progress = await tokenizer.EncodeStreamAsync(GetTextChunks());
progress.ProgressChanged += (s, e) =>
{
    Console.WriteLine($"Processed {e.ProcessedTokens} of {e.TotalTokens} tokens");
};
```

### Vocabulary Management

Save and load vocabularies:

```csharp
// Get the vocabulary manager
var vocabulary = tokenizer.GetVocabulary();

// Save vocabulary to file
await vocabulary.SaveAsync("vocabulary.json");

// Load vocabulary from file
await vocabulary.LoadAsync("vocabulary.json");

// Check if token exists
if (vocabulary.Contains("hello"))
{
    Console.WriteLine("Token 'hello' exists in vocabulary");
}
```

### Special Tokens

Handle special tokens:

```csharp
var config = new TokenizerConfiguration
{
    SpecialTokens = "<|endoftext|>,<|pad|>"
};
var tokenizer = new Tokenizer(config);

// Special tokens are encoded as single tokens
var tokens = await tokenizer.EncodeAsync("<|endoftext|>");
Console.WriteLine(tokens[0].IsSpecial); // True
```

### Parallel Processing

The tokenizer automatically handles parallel processing for large inputs:

```csharp
var config = new TokenizerConfiguration
{
    ParallelizationThreshold = 1000 // Process texts larger than 1000 chars in parallel
};
var tokenizer = new Tokenizer(config);

// Large text will be processed in parallel automatically
var largeText = new string('a', 10000);
var tokens = await tokenizer.EncodeAsync(largeText);
```

### Error Handling

Handle tokenization errors gracefully:

```csharp
try
{
    var tokens = await tokenizer.EncodeAsync(veryLongText);
}
catch (ArgumentException ex)
{
    Console.WriteLine("Input text too long: " + ex.Message);
}
catch (TokenizationException ex)
{
    Console.WriteLine("Tokenization failed: " + ex.Message);
}
```

### Performance Monitoring

Monitor tokenizer performance:

```csharp
// Log metrics after processing
tokenizer.LogMetrics();
```

## Best Practices

1. **Reuse Tokenizer Instances**: Create one tokenizer instance and reuse it.
2. **Configure Appropriately**: Set appropriate thresholds based on your use case.
3. **Handle Large Inputs**: Use streaming for very large inputs.
4. **Monitor Performance**: Regularly check metrics for optimization opportunities.
5. **Error Handling**: Always handle potential exceptions in production code.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

MIT License

Copyright (c) 2024 CS Tokenizer Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. 
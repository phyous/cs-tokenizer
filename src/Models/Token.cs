using System;

namespace CsTokenizer.Models
{
    public record Token
    {
        public string Value { get; init; }
        public int Id { get; init; }
        public bool IsSpecial { get; init; }

        public Token(string value, int id, bool isSpecial = false)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Id = id;
            IsSpecial = isSpecial;
        }
    }

    public record TokenPair
    {
        public Token First { get; init; }
        public Token Second { get; init; }
        public int Frequency { get; set; }

        public TokenPair(Token first, Token second, int frequency = 0)
        {
            First = first ?? throw new ArgumentNullException(nameof(first));
            Second = second ?? throw new ArgumentNullException(nameof(second));
            Frequency = frequency;
        }
    }

    public record MergeRule
    {
        public TokenPair Pair { get; init; }
        public int Priority { get; init; }

        public MergeRule(TokenPair pair, int priority)
        {
            Pair = pair ?? throw new ArgumentNullException(nameof(pair));
            Priority = priority;
        }
    }
} 
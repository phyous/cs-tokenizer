using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using CsTokenizer.Models;

namespace CsTokenizer.Implementation
{
    public class ObjectPool<T> where T : class
    {
        private readonly ConcurrentBag<T> _objects;
        private readonly Func<T> _objectGenerator;
        private readonly Action<T>? _resetAction;
        private readonly int _maxSize;

        public ObjectPool(Func<T> objectGenerator, Action<T>? resetAction = null, int maxSize = 1000)
        {
            _objects = new ConcurrentBag<T>();
            _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
            _resetAction = resetAction;
            _maxSize = maxSize;
        }

        public T Rent()
        {
            return _objects.TryTake(out var item) ? item : _objectGenerator();
        }

        public void Return(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (_objects.Count >= _maxSize)
                return;

            _resetAction?.Invoke(item);
            _objects.Add(item);
        }
    }

    public class TokenListPool
    {
        private static readonly ObjectPool<List<Token>> Pool = new(
            () => new List<Token>(),
            list => list.Clear(),
            maxSize: 1000);

        public static List<Token> Rent() => Pool.Rent();

        public static void Return(List<Token> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            Pool.Return(list);
        }
    }

    public class StringBuilderPool
    {
        private static readonly ObjectPool<StringBuilder> Pool = new(
            () => new StringBuilder(),
            sb => sb.Clear(),
            maxSize: 1000);

        public static StringBuilder Rent() => Pool.Rent();

        public static void Return(StringBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            Pool.Return(builder);
        }
    }
} 
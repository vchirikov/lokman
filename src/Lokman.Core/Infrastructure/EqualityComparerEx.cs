using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lokman
{
    internal static class EqualityComparerEx<T>
    {
        public static IEqualityComparer<T> Create<TKey>(Func<T, TKey> keySelector)
        => Create(keySelector, default);

        internal static IEqualityComparer<T> Create<TKey>(Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
            => new KeyEqualityComparer<TKey>(keySelector, comparer);

        private class KeyEqualityComparer<TKey> : IEqualityComparer<T>
        {
            private readonly Func<T, TKey> _keySelector;
            private readonly IEqualityComparer<TKey> _comparer;

            public KeyEqualityComparer(Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
                => (_keySelector, _comparer) = (keySelector, comparer ?? EqualityComparer<TKey>.Default);

            public bool Equals([AllowNull] T x, [AllowNull] T y) => _comparer.Equals(_keySelector(x!), _keySelector(y!));
            public int GetHashCode([DisallowNull] T obj) => _comparer.GetHashCode(_keySelector(obj)!);
        }
    }
}

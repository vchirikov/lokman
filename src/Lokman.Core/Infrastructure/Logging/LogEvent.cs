using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Lokman
{
    public class LogEvent : IReadOnlyList<KeyValuePair<string, object>>
    {
        private readonly string _eventName;
        private readonly IReadOnlyList<KeyValuePair<string, object>> _data;

        private static readonly IReadOnlyList<KeyValuePair<string, object>> _emptyList
            = new List<KeyValuePair<string, object>>(0);

        public LogEvent(string eventName) : this(eventName, data: null) { }

        public LogEvent(string eventName, IReadOnlyDictionary<string, object>? data)
        {
            _eventName = eventName;
            _data = (data == null || data.Count == 0)
                ? _emptyList
                : data.ToList();
        }

        public KeyValuePair<string, object> this[int index] => _data[index];

        public int Count => _data.Count;

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

#if DEBUG
        private string? _cachedToString;

        private readonly static JsonSerializerOptions _jsonOpt = new JsonSerializerOptions() {
            AllowTrailingCommas = true,
            IgnoreNullValues = true,
            WriteIndented = false,
        };

        public override string ToString() => string.Concat(_eventName, " Data: ", _cachedToString ??= JsonSerializer.Serialize(_data, _jsonOpt));
#else
        public override string ToString() => _eventName;
#endif
    }
}

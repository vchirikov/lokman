using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Lokman.Client
{
    /// <summary>
    /// Code based on <see href="https://github.com/dotnet/extensions/blob/master/src/Logging/Logging.Abstractions/src/LogValuesFormatter.cs">LogValuesFormatter</see>
    /// implementation, but was changed for js interop.
    /// <seealso href="https://github.com/dotnet/extensions/blob/master/LICENSE.txt">The MIT License (MIT)</seealso> <br/>
    /// Copyright (c) .NET Foundation and Contributors
    /// </summary>
    internal sealed class JsLoggerValuesFormatter
    {
        internal const string NullValue = "(null)";
        private static readonly char[] _formatDelimiters = { ',', ':' };

        private static readonly TypeConverter _stringConverter = TypeDescriptor.GetConverter(typeof(string));

        public List<object> Parse(string format, IEnumerable<KeyValuePair<string, object>> keyValuePairs)
        {
            var scanIndex = 0;
            var endIndex = format.Length;
            var result = new List<object>();

            while (scanIndex < endIndex)
            {
                var openBraceIndex = FindBraceIndex(format, '{', scanIndex, endIndex);
                var closeBraceIndex = FindBraceIndex(format, '}', openBraceIndex, endIndex);

                if (closeBraceIndex == endIndex)
                {
                    result.Add(format[scanIndex..endIndex]);
                    scanIndex = endIndex;
                }
                else
                {
                    // Format item syntax : { index[,alignment][ :formatString] }.
                    // we don't support formatting, so just omit it
                    var formatDelimiterIndex = FindIndexOfAny(format, _formatDelimiters, openBraceIndex, closeBraceIndex);

                    if (openBraceIndex > scanIndex)
                        result.Add(format[scanIndex..openBraceIndex]);

                    var key = format[(openBraceIndex + 1)..formatDelimiterIndex];
                    var keyWithBraces = "{" + key + "}";
                    var value = keyValuePairs?.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.Ordinal) || string.Equals(x.Key, keyWithBraces, StringComparison.Ordinal)).Value ?? NullValue;
                    if (value is string strVal)
                    {
                        result.Add(strVal);
                    }
                    else if (value.GetType().IsPrimitive)
                    {
                        result.Add(_stringConverter.ConvertToInvariantString(value));
                    }
                    else
                    {
                        result.Add(value);
                    }
                    scanIndex = closeBraceIndex + 1;
                }
            }
            return result;
        }

        private static int FindBraceIndex(string format, char brace, int startIndex, int endIndex)
        {
            // Example: {{prefix{{{Argument}}}suffix}}.
            var braceIndex = endIndex;
            var scanIndex = startIndex;
            var braceOccurrenceCount = 0;

            while (scanIndex < endIndex)
            {
                if (braceOccurrenceCount > 0 && format[scanIndex] != brace)
                {
                    if (braceOccurrenceCount % 2 == 0)
                    {
                        // Even number of '{' or '}' found. Proceed search with next occurrence of '{' or '}'.
                        braceOccurrenceCount = 0;
                        braceIndex = endIndex;
                    }
                    else
                    {
                        // An unescaped '{' or '}' found.
                        break;
                    }
                }
                else if (format[scanIndex] == brace)
                {
                    if (brace == '}')
                    {
                        if (braceOccurrenceCount == 0)
                        {
                            // For '}' pick the first occurrence.
                            braceIndex = scanIndex;
                        }
                    }
                    else
                    {
                        // For '{' pick the last occurrence.
                        braceIndex = scanIndex;
                    }

                    ++braceOccurrenceCount;
                }

                scanIndex++;
            }

            return braceIndex;
        }

        private static int FindIndexOfAny(string format, char[] chars, int startIndex, int endIndex)
        {
            var findIndex = format.IndexOfAny(chars, startIndex, endIndex - startIndex);
            return findIndex == -1 ? endIndex : findIndex;
        }
    }
}

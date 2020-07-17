using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Primitives;

namespace Lokman.Client
{
#pragma warning disable RCS1224 // Make method an extension method.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class UriHelperExtensions
    {
        /// <summary>
        /// Blazor doesn't support uri parameters yet (eg '?queryParam=value' don't parsed by blazor)
        /// See https://github.com/aspnet/AspNetCore/issues/5489
        /// So this helper can help with parsing
        /// </summary>
        public static Dictionary<string, StringValues>? ParseQuery(this NavigationManager navManager)
        {
            string url = navManager.Uri;
            int idxQueryString = url.IndexOf('?');
            if (idxQueryString <= 0)
                return null;

            return ParseQuery(url.Substring(idxQueryString));
        }

        /// <summary>
        /// Parse a query string into its component key and value parts.
        /// </summary>
        /// <param name="queryString">The raw query string value, with or without the leading '?'.</param>
        /// <returns>A collection of parsed keys and values, null if there are no entries.</returns>
        public static Dictionary<string, StringValues>? ParseQuery(string queryString)
        {
            if (string.IsNullOrEmpty(queryString) || queryString == "?")
                return null;

            var result = new Dictionary<string, StringValues>();
            int scanIndex = 0;
            if (queryString[0] == '?')
                scanIndex = 1;

            int textLength = queryString.Length;
            int equalIndex = queryString.IndexOf('=', StringComparison.Ordinal);
            if (equalIndex == -1)
                equalIndex = textLength;

            while (scanIndex < textLength)
            {
                int delimiterIndex = queryString.IndexOf('&', scanIndex);
                if (delimiterIndex == -1)
                    delimiterIndex = textLength;

                if (equalIndex < delimiterIndex)
                {
                    while (scanIndex != equalIndex && char.IsWhiteSpace(queryString[scanIndex]))
                    {
                        ++scanIndex;
                    }
                    string name = queryString[scanIndex..equalIndex];
                    string value = queryString.Substring(equalIndex + 1, delimiterIndex - equalIndex - 1);

                    result.Add(Uri.UnescapeDataString(name.Replace('+', ' ')), Uri.UnescapeDataString(value.Replace('+', ' ')));

                    equalIndex = queryString.IndexOf('=', delimiterIndex);
                    if (equalIndex == -1)
                        equalIndex = textLength;
                }
                else
                {
                    if (delimiterIndex > scanIndex)
                        result.Add(queryString[scanIndex..delimiterIndex], string.Empty);
                }
                scanIndex = delimiterIndex + 1;
            }
            return result;
        }

        /// <summary>
        /// Blazor doesn't support uri parameters yet (eg '?queryParam=value' don't parsed by blazor)
        /// See https://github.com/aspnet/AspNetCore/issues/5489
        /// So this helper can help with parsing one url parameter
        /// </summary>
        /// <param name="navManager"></param>
        /// <param name="parameterName">case insensitive key</param>
        /// <param name="value">first value of query parameter</param>
        /// <returns>true if parsed</returns>
        public static bool TryParseQueryParameter(this NavigationManager navManager, string parameterName, out string? value)
        {
            value = default;
            var dict = navManager.ParseQuery();
            if (dict == null)
                return false;

            if (dict.TryGetValue(parameterName, out var strVals) && strVals.Count == 1)
            {
                value = strVals[0];
                return true;
            }
            return false;
        }
    }
}

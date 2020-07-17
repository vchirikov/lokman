using System.Collections.Generic;
using FluentAssertions;
using Lokman.Client;
using Xunit;

namespace Lokman.Tests
{
    public class JsLoggerValuesFormatterTests
    {
        private static readonly object _anonObj = new { FooBar = "Hello" };

        [Theory]
        [MemberData(nameof(Parse_Input))]
        public void Parse(string format, Dictionary<string, object> values, List<object> expected, string errorMsg)
        {
            var formatter = new JsLoggerValuesFormatter();
            var result = formatter.Parse(format, values);

            result.Should().Equal(expected, because: errorMsg);
        }

        public static IEnumerable<object[]> Parse_Input() => new List<object[]>{
            new object[]
            {
               "Foo",
               null,
               new List<object>{"Foo"},
               "Should return original message if you don't use any token",
            },
            new object[]
            {
                "Foo {NonExist}",
                null,
                new List<object>{"Foo ", JsLoggerValuesFormatter.NullValue },
                $"Should return {nameof(JsLoggerValuesFormatter.NullValue)} message in case if token value is not found",
            },
            new object[]
            {
               "Foo {Bar} msg",
               new Dictionary<string, object> { {"{Bar}", 5} },
               new List<object>{"Foo ", "5", " msg" },
               "Should return original message and token replaced",
            },
            new object[]
            {
               "Foo {Bar} msg",
               new Dictionary<string, object> { {"Bar", 5} },
               new List<object>{"Foo ", "5", " msg" },
               "Should return original message and token replaced with key without braces",
            },
            new object[]
            {
                "Foo {Bar} msg",
                new Dictionary<string, object> { {"{Bar}", 5.1f} },
                new List<object>{"Foo ", "5.1", " msg" },
                "Should return original message and token replaced with Invariant culture",
            },
            new object[]
            {
                "Foo {Bar} msg",
                new Dictionary<string, object> { {"{Bar}", _anonObj } },
                new List<object>{"Foo ", _anonObj, " msg" },
                "Should return original message and token replaced with object",
            },
            new object[]
            {
                "{Bar} {Msg}",
                new Dictionary<string, object> { {"{Bar}", _anonObj }, {"{Msg}", "test" } },
                new List<object>{_anonObj, " ", "test" },
                "Should return original message with multiple token replaced",
            },
        };
    }
}

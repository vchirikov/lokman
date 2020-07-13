using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Lokman.Tests
{
    public class ObjectPropertiesReaderFactoryTests
    {
        private readonly ITestOutputHelper _logger;
        public ObjectPropertiesReaderFactoryTests(ITestOutputHelper output) => _logger = output;

        [Theory]
        [MemberData(nameof(ObjectPropertiesReaderFactory_Should_DelegateReadProperties_InputData))]
        public void ObjectPropertiesReaderFactory_Should_DelegateReadProperties(object obj, Dictionary<string, object> expected)
        {
            var dict = new Dictionary<string, object>();

            var factory = new ObjectPropertiesReaderFactory();
            factory.GetReader(obj.GetType())(dict, obj);
            _logger.WriteLine(JsonSerializer.Serialize(dict));

            dict.Should().BeEquivalentTo(expected);
        }

        public static IEnumerable<object[]> ObjectPropertiesReaderFactory_Should_DelegateReadProperties_InputData() => new List<object[]> {
            new object[]{ new { TestProp1 = "Foo", intValue = 5 }, new Dictionary<string, object> { { "TestProp1","Foo" }, { "intValue", 5 }, } },
            new object[]{ new { list = new List<string> { "foo", "bar" }}, new Dictionary<string, object> { { "list", new List<string> { "foo", "bar" } }} },
            new object[]{ new Foo(), new Dictionary<string, object> { { "Bar1", 4 }, } },
        };

#pragma warning disable IDE0051 // Remove unused private members
        private class Foo
        {
            private int PrivateProp { get; set; }
            public int this[int _] => 5;
            public int Bar1 => 4;
            public int Bar2 = 4;
        }
    }
}

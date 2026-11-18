using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Awakening.Core.Json;
using Xunit;

namespace Awakening.Core.Tests
{
    public class JsonTests
    {
        [Fact]
        public void RoundTripsNestedValuesInOrder()
        {
            const string text = "{\"b\":[1,2.5,-0.125,1e-7,null,true,false],\"a\":{\"s\":\"ñ \\\"q\\\" \\n 🐙\",\"n\":9007199254740991}}";
            object value = JsonReader.Parse(text);
            Assert.Equal("{\"b\":[1,2.5,-0.125,1E-07,null,true,false],\"a\":{\"s\":\"ñ \\\"q\\\" \\n 🐙\",\"n\":9007199254740991}}", JsonWriter.Write(value));
            Assert.Equal(value is Dictionary<string, object> map ? map.Count : -1, 2);
        }

        [Theory]
        [InlineData("{\"a\":1,\"a\":2}")]
        [InlineData("[1,]")]
        [InlineData("01")]
        [InlineData("\"\\x\"")]
        [InlineData("1e999")]
        [InlineData("{} {}")]
        public void RejectsMalformedText(string text)
        {
            Assert.False(JsonReader.TryParse(text, out _));
        }

        [Fact]
        public void IgnoresTheMachineCulture()
        {
            CultureInfo previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("es-CL");
                Assert.Equal(2.5, (double)JsonReader.Parse("2.5"));
                Assert.Equal("[0.1,1234567.891]", JsonWriter.Write(new List<object> { 0.1, 1234567.891 }));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [Fact]
        public void RefusesNonFiniteNumbers()
        {
            Assert.Throws<ArgumentException>(() => JsonWriter.Write(double.NaN));
        }
    }
}

using System;
using Xunit;

namespace NationalInstruments.Analyzers.Utilities.UnitTests
{
    /// <summary>
    /// Tests that <see cref="ExemptionCollection"/>s allow exemptions to be added and found via contains or matches.
    /// </summary>
    public sealed class ExemptionCollectionTests
    {
        [Fact]
        public void Add_SingleValue_OneExemption()
        {
            var exemptions = new ExemptionCollection { "foo" };

            Assert.Single(exemptions);
        }

        [Fact]
        public void Add_DuplicateValues_OneExemption()
        {
            var exemptions = new ExemptionCollection
            {
                "foo",
                "foo"
            };

            Assert.Single(exemptions);
        }

        [Fact]
        public void Add_DuplicateValues_MixedCase_OneExemption()
        {
            var exemptions = new ExemptionCollection
            {
                "foo",
                "FOO"
            };

            Assert.Single(exemptions);
        }

        [Fact]
        public void Add_DuplicateValues_DuplicateAttributes_OneExemptionWithOneAttribute()
        {
            var exemptions = new ExemptionCollection
            {
                { "foo", new AttributeCollection(Tuple.Create("Assembly", "A")) },
                { "foo", new AttributeCollection(Tuple.Create("Assembly", "A")) }
            };

            Assert.Single(exemptions);
            var foo = exemptions["foo"];
            Assert.NotNull(foo);
            Assert.Single(foo);
        }

        [Fact]
        public void Add_DuplicateValues_DistinctAttributeNames_OneExemptionWithOneAttribute()
        {
            var exemptions = new ExemptionCollection
            {
                { "foo", new AttributeCollection(Tuple.Create("Assembly", "A")) },
                { "foo", new AttributeCollection(Tuple.Create("Parameter", "B")) }
            };

            var attributes = exemptions["foo"];
            Assert.NotNull(attributes);

            Assert.Single(exemptions);
            Assert.Single(attributes);

            Assert.DoesNotContain("Assembly", attributes.Names);
            Assert.Contains("Parameter", attributes.Names);
            Assert.Single(attributes["Parameter"]);
        }

        [Fact]
        public void Add_DuplicateValues_DistinctAttributeValues_OneExemptionWithOneCombinedAttribute()
        {
            var exemptions = new ExemptionCollection
            {
                { "foo", new AttributeCollection(Tuple.Create("Assembly", "A")) },
                { "foo", new AttributeCollection(Tuple.Create("Assembly", "B")) }
            };

            var attributes = exemptions["foo"];
            Assert.NotNull(attributes);

            Assert.Single(exemptions);
            Assert.Single(attributes);

            Assert.Contains("Assembly", attributes.Names);

            var attributeValues = attributes["Assembly"];
            Assert.Contains("A", attributeValues);
            Assert.Contains("B", attributeValues);
        }

        [Fact]
        public void Add_DistinctValues_MultipleExemptions()
        {
            const int ExpectedCount = 2;
            var exemptions = new ExemptionCollection
            {
                "foo",
                "bar"
            };

            Assert.Equal(ExpectedCount, exemptions.Count);
        }

        [Fact]
        public void Contains_NoAttribute_Found()
        {
            var exemptions = new ExemptionCollection("foo");

            Assert.True(exemptions.Contains("foo"));
        }

        [Fact]
        public void Contains_NoAttribute_MixedCase_Found()
        {
            var exemptions = new ExemptionCollection("foo");

            Assert.True(exemptions.Contains("FOO"));
        }

        [Fact]
        public void Contains_NoAttribute_AttributeSpecified_Found()
        {
            var exemptions = new ExemptionCollection("foo");

            Assert.True(exemptions.Contains("foo", new AttributeCollection(Tuple.Create("Assembly", "A"))));
        }

        [Theory]
        [InlineData("Assembly", "B")]
        [InlineData("Parameter", "A")]
        public void Contains_WithAttribute_WrongAttributeSpecified_NotFound(string name, string value)
        {
            var exemption = Tuple.Create<string, AttributeCollection?>("foo", new AttributeCollection(Tuple.Create("Assembly", "A")));
            var exemptions = new ExemptionCollection(exemption);

            Assert.False(exemptions.Contains("foo", new AttributeCollection(Tuple.Create(name, value))));
        }

        [Fact]
        public void Contains_WithAttribute_AttributeSpecified_Found()
        {
            var exemption = Tuple.Create<string, AttributeCollection?>("foo", new AttributeCollection(Tuple.Create("Assembly", "A")));
            var exemptions = new ExemptionCollection(exemption);

            Assert.True(exemptions.Contains("foo", new AttributeCollection(Tuple.Create("Assembly", "A"))));
        }

        [Fact]
        public void Contains_WithAttribute_LessAttributesSpecified_NotFound()
        {
            var exemption = Tuple.Create<string, AttributeCollection?>("foo", new AttributeCollection(Tuple.Create("Assembly", "A")));
            var exemptions = new ExemptionCollection(exemption);

            Assert.False(exemptions.Contains("foo"));
        }

        [Fact]
        public void Contains_WithAttribute_MoreAttributesSpecified_Found()
        {
            var exemption = Tuple.Create<string, AttributeCollection?>("foo", new AttributeCollection(Tuple.Create("Assembly", "A")));
            var exemptions = new ExemptionCollection(exemption);

            Assert.True(exemptions.Contains("foo", new AttributeCollection(Tuple.Create("Assembly", "A"), Tuple.Create("Parameter", "B"))));
        }

        [Theory]
        [InlineData("Assembly", "B", "Parameter", "A")]
        [InlineData("Namespace", "A", "Scope", "B")]
        public void Contains_WithAttributes_WrongAttributesSpecified_NotFound(string name1, string value1, string name2, string value2)
        {
            var exemption = Tuple.Create<string, AttributeCollection?>("foo", new AttributeCollection(Tuple.Create("Assembly", "A"), Tuple.Create("Parameter", "B")));
            var exemptions = new ExemptionCollection(exemption);

            Assert.False(exemptions.Contains("foo", new AttributeCollection(Tuple.Create(name1, value1), Tuple.Create(name2, value2))));
        }

        [Fact]
        public void Contains_WithAttributes_AllAttributesSpecified_Found()
        {
            var exemption = Tuple.Create<string, AttributeCollection?>("foo", new AttributeCollection(Tuple.Create("Assembly", "A"), Tuple.Create("Parameter", "B")));
            var exemptions = new ExemptionCollection(exemption);

            Assert.True(exemptions.Contains("foo", new AttributeCollection(Tuple.Create("Assembly", "A"), Tuple.Create("Parameter", "B"))));
            Assert.True(exemptions.Contains("foo", new AttributeCollection(Tuple.Create("Parameter", "B"), Tuple.Create("Assembly", "A"))));    // order doesn't matter
        }

        [Fact]
        public void Contains_WithAttributes_LessAttributesSpecified_NotFound()
        {
            var exemption = Tuple.Create<string, AttributeCollection?>("foo", new AttributeCollection(Tuple.Create("Assembly", "A"), Tuple.Create("Parameter", "B")));
            var exemptions = new ExemptionCollection(exemption);

            Assert.False(exemptions.Contains("foo", new AttributeCollection(Tuple.Create("Assembly", "A"))));
        }

        [Fact]
        public void Contains_WithAttributes_MoreAttributesSpecified_Found()
        {
            var exemption = Tuple.Create<string, AttributeCollection?>("foo", new AttributeCollection(Tuple.Create("Assembly", "A"), Tuple.Create("Parameter", "B")));
            var exemptions = new ExemptionCollection(exemption);

            Assert.True(exemptions.Contains("foo", new AttributeCollection(Tuple.Create("Assembly", "A"), Tuple.Create("Parameter", "B"), Tuple.Create("Namespace", "C"))));
        }

        [Fact]
        public void Contains_WithMultiValuedAttribute_ValueNotSpecified_Found()
        {
            var exemptions = new ExemptionCollection(Tuple.Create<string, AttributeCollection?>("foo", new AttributeCollection(Tuple.Create("Assembly", "A|B"))));

            Assert.True(exemptions.Contains("foo", new AttributeCollection(Tuple.Create("Assembly", "A"))));
        }

        [Fact]
        public void Contains_WithMultiValuedAttribute_WrongValueSpecified_NotFound()
        {
            var exemptions = new ExemptionCollection(Tuple.Create<string, AttributeCollection?>("foo", new AttributeCollection(Tuple.Create("Assembly", "A|B"))));

            Assert.False(exemptions.Contains("foo", new AttributeCollection(Tuple.Create("Assembly", "D"))));
        }

        [Theory]
        [InlineData("*")]
        [InlineData("*o")]
        [InlineData("f*")]
        [InlineData("*o*")]
        [InlineData("f*o")]
        [InlineData("*foo")]
        [InlineData("foo*")]
        public void Matches_NoAttribute_Found(string pattern)
        {
            var exemptions = new ExemptionCollection(pattern);

            Assert.True(exemptions.Matches("foo"));
        }

        [Fact]
        public void Matches_WithAttribute_AttributeSpecified_Found()
        {
            var exemptions = new ExemptionCollection(Tuple.Create<string, AttributeCollection?>("*", new AttributeCollection(Tuple.Create("Assembly", "A"))));

            Assert.True(exemptions.Matches("foo", new AttributeCollection(Tuple.Create("Assembly", "A"))));
        }

        [Fact]
        public void Matches_WithAttribute_LessAttributesSpecified_NotFound()
        {
            var exemptions = new ExemptionCollection(Tuple.Create<string, AttributeCollection?>("*", new AttributeCollection(Tuple.Create("Assembly", "A"))));

            Assert.False(exemptions.Matches("foo"));
        }

        [Fact]
        public void Matches_WithAttribute_MoreAttributesSpecified_Found()
        {
            var exemptions = new ExemptionCollection(Tuple.Create<string, AttributeCollection?>("*", new AttributeCollection(Tuple.Create("Assembly", "A"))));

            Assert.True(exemptions.Matches("foo", new AttributeCollection(Tuple.Create("Assembly", "A"), Tuple.Create("Parameter", "B"))));
        }

        // Multiple attributes are assumed to pass if Contains and Matches pass
    }
}

using System;
using System.Linq;
using Xunit;

namespace NationalInstruments.Tools.Analyzers.Utilities.UnitTests
{
    /// <summary>
    /// Tests that <see cref="AttributeCollection"/>s allow attributes can be added, merged, and compared against other attributes.
    /// </summary>
    public sealed class AttributeCollectionTests
    {
        [Fact]
        public void Add_Single_OneAttribute()
        {
            var attributes = new AttributeCollection();
            attributes.Add("Assembly", "A");

            Assert.Single(attributes);
        }

        [Fact]
        public void Add_Multiple_MultipleAttributes()
        {
            const int ExpectedAttributeCount = 2;

            var attributes = new AttributeCollection();
            attributes.Add("Assembly", "A");
            attributes.Add("Parameter", "B");

            Assert.Equal(ExpectedAttributeCount, attributes.Count);
        }

        [Fact]
        public void Add_Duplicates_OneAttribute()
        {
            const string ExpectedName = "Assembly";
            const string ExpectedValue = "A";

            var attributes = new AttributeCollection();
            attributes.Add("Assembly", "A");
            attributes.Add("Assembly", "A");

            Assert.Single(attributes);

            var attribute = attributes.First();
            Assert.Single(attribute.Value);
            Assert.Equal(ExpectedName, attribute.Key);
            Assert.Equal(ExpectedValue, attributes[ExpectedName].First());
        }

        [Fact]
        public void Add_Duplicates_MixedCase_OneAttribute()
        {
            var attributes = new AttributeCollection();
            attributes.Add("assembly", "A");
            attributes.Add("ASSEMBLY", "a");

            Assert.Single(attributes);
            Assert.Single(attributes.First().Value);
        }

        [Fact]
        public void Add_Duplicates_DifferentValues_OneAttribute()
        {
            const int ExpectedValueCount = 2;
            const string ExpectedValue1 = "A";
            const string ExpectedValue2 = "B";

            var attributes = new AttributeCollection();
            attributes.Add("Assembly", "A");
            attributes.Add("Assembly", "B");

            Assert.Single(attributes);

            var attribute = attributes.First();
            Assert.Equal(ExpectedValueCount, attribute.Value.Count);
            Assert.Contains(ExpectedValue1, attribute.Value);
            Assert.Contains(ExpectedValue2, attribute.Value);
        }

        [Fact]
        public void Merge_SameAttributes_NoChange()
        {
            const string ExpectedName = "Assembly";
            const string ExpectedValue = "A";

            var attributes = new AttributeCollection(Tuple.Create("Assembly", "A"));
            var newAttributes = new AttributeCollection(Tuple.Create("Assembly", "A"));

            attributes.Merge(newAttributes);

            Assert.Single(attributes);
            Assert.Equal(ExpectedName, attributes.Names.First());
            Assert.Contains(ExpectedValue, attributes[ExpectedName]);
        }

        [Fact]
        public void Merge_SameNames_ValuesMerged()
        {
            const string ExpectedValue = "A";
            const string ExpectedNewValue = "B";

            var attributes = new AttributeCollection(Tuple.Create("Assembly", "A"));
            var newAttributes = new AttributeCollection(Tuple.Create("Assembly", "B"));

            attributes.Merge(newAttributes);

            Assert.Single(attributes);

            var attribute = attributes.First();
            Assert.Contains(ExpectedValue, attribute.Value);
            Assert.Contains(ExpectedNewValue, attribute.Value);
        }

        [Fact]
        public void Merge_NewAttributes_Intersection()
        {
            const int ExpectedAttributeCount = 2;
            const string ExpectedAttributeName1 = "Assembly";
            const string ExpectedAttributeValue1 = "A";
            const string ExpectedAttributeName2 = "Namespace";
            const string ExpectedAttributeValue2 = "C";

            var attributes = new AttributeCollection(Tuple.Create("Assembly", "A"), Tuple.Create("Parameter", "B"));
            var newAttributes = new AttributeCollection(Tuple.Create("Assembly", "A"), Tuple.Create("Namespace", "C"));

            attributes.Merge(newAttributes);

            Assert.Equal(ExpectedAttributeCount, attributes.Count);
            Assert.Contains(ExpectedAttributeName1, attributes.Names);
            Assert.Contains(ExpectedAttributeName2, attributes.Names);
            Assert.Contains(ExpectedAttributeValue1, attributes[ExpectedAttributeName1]);
            Assert.Contains(ExpectedAttributeValue2, attributes[ExpectedAttributeName2]);
        }

        [Fact]
        public void MoreSpecificThan_LessSpecificAttributes_True()
        {
            var attributes = new AttributeCollection(Tuple.Create("Assembly", "A"), Tuple.Create("Parameter", "B"));
            var lessSpecificAttributes = new AttributeCollection(Tuple.Create("Assembly", "A"));

            Assert.True(attributes.MoreSpecificThan(lessSpecificAttributes));
        }

        [Fact]
        public void MoreSpecificThan_MoreSpecificAttributes_False()
        {
            var attributes = new AttributeCollection(Tuple.Create("Assembly", "A"), Tuple.Create("Parameter", "B"));
            var moreSpecificAttributes = new AttributeCollection(
                Tuple.Create("Assembly", "A"),
                Tuple.Create("Parameter", "B"),
                Tuple.Create("Namespace", "C"));

            Assert.False(attributes.MoreSpecificThan(moreSpecificAttributes));
        }

        [Fact]
        public void MoreSpecificThan_EquallySpecificAttributes_False()
        {
            var attributes = new AttributeCollection(Tuple.Create("Assembly", "A"), Tuple.Create("Parameter", "B"));
            var equallySpecificAttriubtes = new AttributeCollection(Tuple.Create("Assembly", "A"), Tuple.Create("Parameter", "B"));

            Assert.False(attributes.MoreSpecificThan(equallySpecificAttriubtes));
        }
    }
}

using NationalInstruments.Analyzers.Correctness;
using NationalInstruments.Analyzers.TestUtilities;
using NationalInstruments.Analyzers.TestUtilities.TestFiles;
using NationalInstruments.Analyzers.TestUtilities.Verifiers;
using Xunit;

namespace NationalInstruments.Analyzers.UnitTests
{
    public class RecordWithEnumerablesShouldOverrideDefaultEqualityAnalyzerTests
        : NIDiagnosticAnalyzerTests<RecordWithEnumerablesShouldOverrideDefaultEqualityAnalyzer>
    {
        [Fact]
        public void RecordHasNoEnumerableProperties_NoDiagnostics()
        {
            var test = new AutoTestFile(
                @"public record TestRecord
                {
                    public int MyInt {get;}
                    public string MyString {get;}
                };");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void ClassHasEnumerableProperty_NoDiagnostics()
        {
            var test = new AutoTestFile(
                @"using System.Collections.Generic;

                public class TestClass
                {
                    public IEnumerable<int> MyInts {get;}
                };");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void RecordHasEnumerableProperties_Diagnostics()
        {
            var test = new AutoTestFile(
                @"using System.Collections.Generic;

                public record <?>TestRecord
                {
                    public IEnumerable<int> MyInts {get;}
                };",
                GetNI1019Rule("TestRecord"));

            VerifyDiagnostics(test);
        }

        [Fact]
        public void RecordHasDictionaryProperty_Diagnostics()
        {
            var test = new AutoTestFile(
                @"using System.Collections.Generic;

                public record <?>TestRecord
                {
                    public IDictionary<int, string> MyDictionary {get;}
                };",
                GetNI1019Rule("TestRecord"));

            VerifyDiagnostics(test);
        }

        [Fact]
        public void RecordHasEnumerablePropertiesAndCustomEqualsImplementation_NoDiagnostics()
        {
            var test = new AutoTestFile(
                @"using System.Linq;
                using System.Collections.Generic;

                public record TestRecord
                {
                    public IEnumerable<int> MyInts {get;}

                    public virtual bool Equals(TestRecord other)
                    {
                        return other is not null
                            && MyInts.SequenceEqual(other.MyInts);
                    }
                };");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void RecordHasEnumerablePropertiesAndCustomEqualsImplementationInOtherFile_NoDiagnostics()
        {
            var recordPropertySource = new AutoTestFile(
                @"using System.Collections.Generic;

                public partial record TestRecord
                {
                    public IEnumerable<int> MyInts {get;}
                };");
            var recordEqualsSource = new AutoTestFile(
                @"using System.Linq;
                public partial record TestRecord
                {
                    public virtual bool Equals(TestRecord other)
                    {
                        return other is not null
                            && MyInts.SequenceEqual(other.MyInts);
                    }
                }");

            VerifyDiagnostics(new[] { recordPropertySource, recordEqualsSource });
        }

        private Rule GetNI1019Rule(string typeName)
        {
            return new Rule(
                RecordWithEnumerablesShouldOverrideDefaultEqualityAnalyzer.Rule,
                typeName);
        }
    }
}

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
                }");

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
                }");

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
                }",
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
                }",
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
                }");

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
                }");
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

        [Fact]
        public void RecordHasEnumerableBaseTypePropertiesAndDoesNotImplementEquality_NoDiagnostics()
        {
            var baseRecord = new AutoTestFile(
                @"using System.Linq;
                using System.Collections.Generic;

                namespace Test;

                public record BaseRecord
                {
                    public IEnumerable<int> MyInts {get;}

                    public virtual bool Equals(BaseRecord other)
                    {
                        return other is not null
                            && MyInts.SequenceEqual(other.MyInts);
                    }
                }");
            var derivedRecord = new AutoTestFile(
                @"using Test;
                public record DerivedRecord : BaseRecord
                {
                }");

            VerifyDiagnostics(new[] { baseRecord, derivedRecord });
        }

        [Fact]
        public void NestedRecordWithEnumerablePropertyInClass_Diagnostics()
        {
            var test = new AutoTestFile(
                @"using System.Linq;
                using System.Collections.Generic;

                namespace Test;

                public class Test
                {
                    private record <?>TestRecord
                    {
                        public IEnumerable<int> MyInts {get;}
                    }
                }",
                GetNI1019Rule("TestRecord"));

            VerifyDiagnostics(test);
        }

        [Fact]
        public void MultipleRecordsWithEnumerablePropertyInSingleFile_Diagnostics()
        {
            var test = new AutoTestFile(
                @"using System.Linq;
                using System.Collections.Generic;

                namespace Test;

                public record <?>TestRecord
                {
                    public IEnumerable<int> MyInts {get;}
                }

                public record <?>TestRecord2
                {
                    public IEnumerable<int> MyInts {get;}
                }

                public record TestRecord3
                {
                    public int MyInt {get;}
                }
                ",
                GetNI1019Rule("TestRecord"),
                GetNI1019Rule("TestRecord2"));

            VerifyDiagnostics(test);
        }

        [Fact]
        public void DerivedRecordHidesBasePropertyWithEnumerableTypeAndDoesNotImplementEquality_Diagnostics()
        {
            var test = new AutoTestFile(
                @"using System.Linq;
                using System.Collections.Generic;

                namespace Test;

                public record BaseRecord
                {
                    public int MyInts { get; }
                }

                public record <?>TestRecord : BaseRecord
                {
                    public IEnumerable<int> MyInts { get; }
                }",
                GetNI1019Rule("TestRecord"));

            VerifyDiagnostics(test);
        }

        [Fact]
        public void RecordImplementEqualsWithUnexpectedSignature_Diagnostics()
        {
            var test = new AutoTestFile(
                @"using System.Linq;
                using System.Collections.Generic;

                namespace Test;

                public record <?>TestRecord
                {
                    public IEnumerable<int> MyInts { get; }

                    public bool Equals(int otherInt)
                    {
                        return false;
                    }
                }",
                GetNI1019Rule("TestRecord"));

            VerifyDiagnostics(test);
        }

        [Fact]
        public void DerivedRecordDeclaresDifferentlyNamedPropertyAndDoesNotImplementEquality_Diagnostics()
        {
            var test = new AutoTestFile(
                @"using System.Linq;
                using System.Collections.Generic;

                namespace Test;

                public record BaseRecord
                {
                    public IEnumerable<int> MyInts { get; }

                    public BaseRecord(IEnumerable<int> myInts)
                    {
                        MyInts = myInts;
                    }

                    public virtual bool Equals(BaseRecord? other)
                    {
                        return other is not null
                            && MyInts.SequenceEqual(other.MyInts);
                    }
                }

                public record <?>DerivedRecord : BaseRecord
                {
                    public IEnumerable<int> OtherInts { get; }

                    public DerivedRecord(IEnumerable<int> otherInts)
                        : base(otherInts)
                    {
                    }
                }",
                GetNI1019Rule("DerivedRecord"));

            VerifyDiagnostics(test);
        }

        private Rule GetNI1019Rule(string typeName)
        {
            return new Rule(
                RecordWithEnumerablesShouldOverrideDefaultEqualityAnalyzer.Rule,
                typeName);
        }
    }
}

using NationalInstruments.Tools.Analyzers.Style;
using NationalInstruments.Tools.Analyzers.TestUtilities;
using NationalInstruments.Tools.Analyzers.TestUtilities.TestFiles;
using NationalInstruments.Tools.Analyzers.TestUtilities.Verifiers;
using Xunit;

namespace NationalInstruments.Tools.Analyzers.UnitTests
{
    public sealed class FieldsCamelCasedWithUnderscoreAnalyzerTests : NIDiagnosticAnalyzerTests<FieldsCamelCasedWithUnderscoreAnalyzer>
    {
        [Fact]
        public void NI1001_PublicField_NoDiagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    public int Name;
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1001_InternalField_NoDiagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    internal int Name;
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1001_ProtectedField_NoDiagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    protected int Name;
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1001_PropertyImplicitBackingField_NoDiagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    public int Name { get; set; }
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1001_PrivateConst_NoDiagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    private const int Name = 0;
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1001_PrivateReadonlyField_NoDiagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    private readonly int Name;
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1001_UnderscoreAndBeginsLowercase_NoDiagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    private int _name;
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1001_UnderscoreOnly_NoDiagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    private int _;
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1001_InternalUnderscore_NoDiagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    private int _name_two;
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1001_TrailingUnderscore_NoDiagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    private int _name_;
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1001_NoLeadingUnderscore_Diagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    private int name;
}");

            VerifyDiagnostics(test, GetNI1001ResultAt(4, 17, "name"));
        }

        [Fact]
        public void NI1001_ExtraLeadingUnderscore_Diagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    private int __name;
}");

            VerifyDiagnostics(test, GetNI1001ResultAt(4, 17, "__name"));
        }

        [Fact]
        public void NI1001_UnderscoreUppercaseLetter_Diagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    private int _Name;
}");

            VerifyDiagnostics(test, GetNI1001ResultAt(4, 17, "_Name"));
        }

        [Fact]
        public void NI1001_UnderscoreNumeral_NoDiagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    private int _0Name;
}");

            VerifyDiagnostics(test);
        }

        private DiagnosticResult GetNI1001ResultAt(int line, int column, string fieldName)
        {
            return GetResultAt(line, column, FieldsCamelCasedWithUnderscoreAnalyzer.Rule, fieldName);
        }
    }
}

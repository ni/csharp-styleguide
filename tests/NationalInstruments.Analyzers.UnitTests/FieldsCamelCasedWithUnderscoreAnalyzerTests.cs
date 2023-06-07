using NationalInstruments.Analyzers.Style;
using NationalInstruments.Analyzers.TestUtilities;
using NationalInstruments.Analyzers.TestUtilities.TestFiles;
using NationalInstruments.Analyzers.TestUtilities.Verifiers;
using Xunit;

namespace NationalInstruments.Analyzers.UnitTests
{
    public sealed class FieldsCamelCasedWithUnderscoreAnalyzerTests : NIDiagnosticAnalyzerWithCodeFixTests<FieldsCamelCasedWithUnderscoreAnalyzer, FieldsCamelCasedWithUnderscoreCodeFixProvider>
    {
        private const string NoLeadingUnderscoreCodeMarkup = @"
class Foo
{
    private int <|name|>;
}";

        private const string ExtraLeadingUnderscoreCodeMarkup = @"
class Foo
{
    private int <|__name|>;
}";

        private const string UnderscoreUppercaseLetterCodeMarkup = @"
class Foo
{
    private int <|_Name|>;
}";

        private const string FixedCode = @"
class Foo
{
    private int _name;
}";

        [Fact]
        public void PublicField_Verify_NoDiagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    public int Name;
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void InternalField_Verify_NoDiagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    internal int Name;
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void ProtectedField_Verify_NoDiagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    protected int Name;
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void PropertyImplicitBackingField_Verify_NoDiagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    public int Name { get; set; }
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void PrivateConst_Verify_NoDiagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    private const int Name = 0;
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void PrivateReadonlyField_Verify_NoDiagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    private readonly int Name;
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void UnderscoreAndBeginsLowercase_Verify_NoDiagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    private int _name;
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void UnderscoreOnly_Verify_NoDiagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    private int _;
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void InternalUnderscore_Verify_NoDiagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    private int _name_two;
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void TrailingUnderscore_Verify_NoDiagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    private int _name_;
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NoLeadingUnderscore_Verify_Diagnostic()
        {
            var test = new AutoTestFile(
                NoLeadingUnderscoreCodeMarkup,
                GetNI1001FieldsCamelCasedWithUnderscoreRule());

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NoLeadingUnderscore_ApplyFix_FixedWithNoDiagnostic()
        {
            var testBeforeFix = new AutoTestFile(NoLeadingUnderscoreCodeMarkup);
            var testAfterFix = new TestFile(FixedCode);

            VerifyFix(testBeforeFix, testAfterFix);
            VerifyDiagnostics(testAfterFix);
        }

        [Fact]
        public void ExtraLeadingUnderscore_Verify_Diagnostic()
        {
            var test = new AutoTestFile(
                ExtraLeadingUnderscoreCodeMarkup,
                GetNI1001FieldsCamelCasedWithUnderscoreRule());

            VerifyDiagnostics(test);
        }

        [Fact]
        public void ExtraLeadingUnderscore_ApplyFix_FixedWithNoDiagnostic()
        {
            var testBeforeFix = new AutoTestFile(ExtraLeadingUnderscoreCodeMarkup);
            var testAfterFix = new TestFile(FixedCode);

            VerifyFix(testBeforeFix, testAfterFix);
            VerifyDiagnostics(testAfterFix);
        }

        [Fact]
        public void UnderscoreUppercaseLetter_Verify_Diagnostic()
        {
            var test = new AutoTestFile(
                UnderscoreUppercaseLetterCodeMarkup,
                GetNI1001FieldsCamelCasedWithUnderscoreRule());

            VerifyDiagnostics(test);
        }

        [Fact]
        public void UnderscoreUppercaseLetter_ApplyFix_FixedWithNoDiagnostic()
        {
            var testBeforeFix = new AutoTestFile(UnderscoreUppercaseLetterCodeMarkup);
            var testAfterFix = new TestFile(FixedCode);

            VerifyFix(testBeforeFix, testAfterFix);
            VerifyDiagnostics(testAfterFix);
        }

        [Fact]
        public void UnderscoreNumeral_Verify_NoDiagnostic()
        {
            var test = new TestFile(@"
class Foo
{
    private int _0Name;
}");

            VerifyDiagnostics(test);
        }

        private Rule GetNI1001FieldsCamelCasedWithUnderscoreRule() 
            => new Rule(FieldsCamelCasedWithUnderscoreAnalyzer.Rule);
    }
}

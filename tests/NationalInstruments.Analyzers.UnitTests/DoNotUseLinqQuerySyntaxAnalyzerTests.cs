using NationalInstruments.Analyzers.Style.DoNotUseLinqQuerySyntax;
using NationalInstruments.Analyzers.TestUtilities;
using NationalInstruments.Analyzers.TestUtilities.TestFiles;
using NationalInstruments.Analyzers.TestUtilities.Verifiers;
using Xunit;

namespace NationalInstruments.Analyzers.UnitTests
{
    /// <summary>
    /// Tests for <see cref="DoNotUseLinqQuerySyntaxAnalyzer"/>
    /// </summary>
    public sealed class DoNotUseLinqQuerySyntaxAnalyzerTests : NIDiagnosticAnalyzerWithCodeFixTests<DoNotUseLinqQuerySyntaxAnalyzer, DoNotUseLinqQuerySyntaxCodeFixProvider>
    {
        [Fact]
        public void LinqMethodSyntax_NoDiagnostic()
        {
            var test = new AutoTestFile(
                @"
using System.Linq;

class ClassUnderTest
{
    public void MethodUnderTest()
    {
        var enumerableItems = new[] { 1, 2, 3 };
        var linqQuery = enumerableItems.Select(item => item);
    }
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void LinqQuerySyntax_Diagnostic()
        {
            var test = new AutoTestFile(
                @"
using System.Linq;

class ClassUnderTest
{
    public void MethodUnderTest()
    {
        var enumerableItems = new[] { 1, 2, 3 };
        var linqQuery = <|>from item in enumerableItems
                        select item;
    }
}",
                new Rule(DoNotUseLinqQuerySyntaxAnalyzer.Rule));

            VerifyDiagnostics(test);
        }

        [Fact]
        public void LinqQuerySyntax_ApplyFix_NoDiagnostic()
        {
            var test = new AutoTestFile(
                @"
using System.Linq;

class ClassUnderTest
{
    public void MethodUnderTest()
    {
        var enumerableItems = new[] { 1, 2, 3 };
        var linqQuery = from item in enumerableItems select item;
    }
}");

            var testAfterFix = new TestFile(
                @"
using System.Linq;

class ClassUnderTest
{
    public void MethodUnderTest()
    {
        var enumerableItems = new[] { 1, 2, 3 };
        var linqQuery = enumerableItems.Select(item => item);
    }
}");

            VerifyFix(test, testAfterFix);
        }
    }
}

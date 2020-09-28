using NationalInstruments.Tools.Analyzers.Correctness;
using NationalInstruments.Tools.Analyzers.TestUtilities;
using NationalInstruments.Tools.Analyzers.TestUtilities.TestFiles;
using NationalInstruments.Tools.Analyzers.TestUtilities.Verifiers;
using Xunit;

namespace NationalInstruments.Tools.Analyzers.UnitTests
{
    /// <summary>
    /// Tests that the <see cref="ThereIsOnlyOneRestrictedNamespaceAnalyzer"/> emits a diagnostic is when a type's overall namespace
    /// contains the text 'Restricted' but does not begin with the text 'NationalInstruments.Restricted'.
    /// </summary>
    public sealed class ThereIsOnlyOneRestrictedNamespaceAnalyzerTests : NIDiagnosticAnalyzerTests<ThereIsOnlyOneRestrictedNamespaceAnalyzer>
    {
        [Fact]
        public void LRN001_IncorrectRestrictedNamespace_Diagnostic()
        {
            var test = new TestFile(@"
using System;

namespace MyApp.Restricted
{
    class Program
    {   
    }
}");

            VerifyDiagnostics(test, GetLRN001ResultAt(4, 5, "MyApp.Restricted"));
        }

        [Fact]
        public void LRN001_CorrectRestrictedNamespace_NoDiagnostic()
        {
            var test = new TestFile(@"
using System;

namespace NationalInstruments.Restricted.MyNamespace
{
    class Program
    {
    }
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void LRN001_CorrectRestrictedNamespaceNested_NoDiagnostic()
        {
            var test = new TestFile(@"
namespace NationalInstruments
{
    namespace Restricted
    {
        class Foo
        {
        }
    }
}");

            VerifyDiagnostics(test);
        }

        public DiagnosticResult GetLRN001ResultAt(int line, int column, string namespaceName)
        {
            return GetResultAt(line, column, ThereIsOnlyOneRestrictedNamespaceAnalyzer.Rule, namespaceName);
        }
    }
}

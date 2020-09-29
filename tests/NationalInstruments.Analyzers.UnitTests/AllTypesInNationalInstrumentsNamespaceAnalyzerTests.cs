using NationalInstruments.Analyzers.Correctness;
using NationalInstruments.Analyzers.TestUtilities;
using NationalInstruments.Analyzers.TestUtilities.TestFiles;
using NationalInstruments.Analyzers.TestUtilities.Verifiers;
using Xunit;

namespace NationalInstruments.Analyzers.UnitTests
{
    /// <summary>
    /// Tests that the <see cref="AllTypesInNationalInstrumentsNamespaceAnalyzer" /> emits a diagnostic when a type is found in
    /// a namespace that does not match the regex '^NationalInstruments(\s|\b)' unless that type's namespace is exempted.
    /// </summary>
    public sealed class AllTypesInNationalInstrumentsNamespaceAnalyzerTests : NIDiagnosticAnalyzerTests<AllTypesInNationalInstrumentsNamespaceAnalyzer>
    {
        private const string ExampleExemptNamespacesFileName = "MyExemptNamespaces.xml";

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("enum")]
        [InlineData("interface")]
        public void LRT001_TypeInNationalInstrumentsNamespace_NoDiagnostic(string type)
        {
            var test = new TestFile($@"
using System;

namespace NationalInstruments
{{
    {type} MyObject
    {{
    }}
}}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void LRT001_TypeInNestedNationalInstrumentsNamespace_NoDiagnostic()
        {
            var test = new TestFile(@"
namespace NationalInstruments
{
    namespace MyApp
    {
        class MyClass
        {
        }
    }
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void LRT001_TypesNotInNationalInstrumentsNamespace_Diagnostics()
        {
            var test = new TestFile(@"
namespace MyApp
{
    class MyClass
    {
    }

    struct MyStruct
    {
    }

    enum MyEnum
    {
    }

    interface IInterface
    {
    }
}");

            VerifyDiagnostics(
                test,
                GetLRT001ResultAt(4, 5, "MyClass"),
                GetLRT001ResultAt(8, 5, "MyStruct"),
                GetLRT001ResultAt(12, 5, "MyEnum"),
                GetLRT001ResultAt(16, 5, "IInterface"));
        }

        [Fact]
        public void LRT001_TypeNotInTrueNationalInstrumentsNamespace_Diagnostic()
        {
            var test = new TestFile(@"
namespace NationalInstrumentsFake
{
    class MyClass
    {
    }
}");

            VerifyDiagnostics(test, GetLRT001ResultAt(4, 5, "MyClass"));
        }

        [Fact]
        public void LRT001_NoTypeNotInNationalInstrumentsNamespace_NoDiagnostic()
        {
            var test = new TestFile(@"
namespace MyApp
{
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void LRT001_TypeInExemptedNamespace_NoDiagnostic()
        {
            var test = new TestFile(@"
namespace MyApp
{
    class MyClass
    {
    }
}");

            var exemptionFile = new TestAdditionalDocument(
                ExampleExemptNamespacesFileName,
                "<ExemptNamespaces><Entry>MyApp</Entry></ExemptNamespaces>");

            VerifyDiagnostics(test, new[] { exemptionFile });
        }

        [Fact]
        public void LRT001_TypeInNestedExemptedNamespace_NoDiagnostic()
        {
            var test = new TestFile(@"
namespace Foo
{
    namespace Bar
    {
        class MyClass
        {
        }
    }
}");

            var exemptionFile = new TestAdditionalDocument(
                ExampleExemptNamespacesFileName,
                "<ExemptNamespaces><Entry>Foo.Bar</Entry></ExemptNamespaces>");

            VerifyDiagnostics(test, new[] { exemptionFile });
        }

        [Fact]
        public void LRT001_TypeNearExemptedNamespace_Diagnostic()
        {
            var test = new TestFile(@"
namespace Foo
{
    namespace Bar
    {
        class BarsClass
        {
        }    
    }
    
    class FoosClass
    {
    }
}");

            var exemptionFile = new TestAdditionalDocument(
                ExampleExemptNamespacesFileName,
                "<ExemptNamespaces><Entry>Foo.Bar</Entry></ExemptNamespaces>");

            VerifyDiagnostics(test, additionalFiles: new[] { exemptionFile }, expectedDiagnostics: GetLRT001ResultAt(11, 5, "FoosClass"));
        }

        [Fact]
        public void LRT001_ExemptNamespacesFileHasInvalidXml_Diagnostic()
        {
            var test = new TestFile(@"
namespace MyApp
{
    class MyClass
    {
    }
}");

            var invalidExemptionFile = new TestAdditionalDocument(
                ExampleExemptNamespacesFileName,
                "<ExemptNamespaces><Entry>MyApp<Entry><ExemptNamespaces>");

            VerifyDiagnostics(
                test,
                new[] { invalidExemptionFile },
                GetLRT001FileParseErrorResultAt(
                    ExampleExemptNamespacesFileName,
                    "Unexpected end of file has occurred. The following elements are not closed: ExemptNamespaces, Entry, Entry, ExemptNamespaces. Line 1, position 56."),
                GetLRT001ResultAt(4, 5, "MyClass"));
        }

        private DiagnosticResult GetLRT001ResultAt(int line, int column, string typeName)
        {
            return GetResultAt(line, column, AllTypesInNationalInstrumentsNamespaceAnalyzer.Rule, typeName);
        }

        private DiagnosticResult GetLRT001FileParseErrorResultAt(string fileName, string exceptionMessage)
        {
            return GetResult(fileName, AllTypesInNationalInstrumentsNamespaceAnalyzer.FileParseRule, exceptionMessage);
        }
    }
}

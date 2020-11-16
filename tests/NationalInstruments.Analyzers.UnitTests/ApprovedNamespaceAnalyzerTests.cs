using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using NationalInstruments.Analyzers.TestUtilities;
using NationalInstruments.Analyzers.TestUtilities.TestFiles;
using NationalInstruments.Analyzers.TestUtilities.Verifiers;
using NationalInstruments.Tools.Analyzers.Namespaces;
using Xunit;

namespace NationalInstruments.Analyzers.UnitTests
{
    /// <summary>
    /// Tests that the <see cref="ApprovedNamespaceAnalyzer" /> emits two different violations when necessary:
    /// 1. Production namespace is unapproved and 2. Test or TestUtilities namespace is unapproved.
    /// </summary>
    public sealed class ApprovedNamespaceAnalyzerTests : NIDiagnosticAnalyzerWithCodeFixTests<ApprovedNamespaceAnalyzer, ApprovedNamespaceCodeFixProvider>
    {
        [Theory]
        [InlineData(
                @"
namespace NationalInstruments.Design
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}")]
        [InlineData(
                @"
namespace System.Windows
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}")]
        [InlineData(
                @"
namespace System
{
    namespace Windows
    {
        class Program
        {
            static void Main(string[] args)
            {
            }
        }
    }
}")]
        [InlineData(
            @"
namespace System
{
    namespace Windows
    {
        class Test
        {
            static void Func(string[] args)
            {
            }
        }
    }
    namespace NonWindows
    {
        class Program
        {
            static void Main(string[] args)
            {
            }
        }
    }
}")]
        [InlineData(
            @"
namespace SomeNamespace
{
    namespace Tool
    {
        class Test
        {
            static void Func(string[] args)
            {
            }
        }
    }
    namespace Teal
    {
        class Program
        {
            static void Main(string[] args)
            {
            }
        }
    }
}")]
        public void ApprovedNamespace_Verify_NoDiagnostic(string sampleCode)
        {
            using (var testState = new TestState())
            {
                var test = new AutoTestFile(sampleCode);
                VerifyDiagnostics(test, testState.GetApprovedNamespaces());
            }
        }

        [Theory]
        [InlineData(
            @"
namespace <?>NationalInstruments.Designer
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}", "NationalInstruments.Designer")]
        [InlineData(
            @"
namespace <?>Systematic.Windows
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}", "Systematic.Windows")]
        [InlineData(
            @"
namespace <?>NotAQualifiedNamespace
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}", "NotAQualifiedNamespace")]
        [InlineData(
            @"
namespace Systematic
{
    namespace <?>Windows
    {
        class Program
        {
            static void Main(string[] args)
            {
            }
        }
    }
}", "Systematic.Windows")]
        [InlineData(
            @"
namespace NationalInstruments
{
    namespace SourceModel
    {
        class Test
        {
            static void Func(string[] args)
            {
            }
        }
    }
    namespace <?>Bogus
    {
        class Program
        {
            static void Main(string[] args)
            {
            }
        }
    }
}", "NationalInstruments.Bogus")]
        [InlineData(
            @"
namespace SomeNamespace
{
    namespace Tool
    {
        class Test
        {
            static void Func(string[] args)
            {
            }
        }
    }
    namespace <?>Tong
    {
        class Program
        {
            static void Main(string[] args)
            {
            }
        }
    }
}", "SomeNamespace.Tong")]
        public void UnapprovedNamespace_Verify_EmitsDiagnostic(string sampleCode, string violatingNamespace)
        {
            using (var testState = new TestState())
            {
                var test = new AutoTestFile(sampleCode, new Rule(ApprovedNamespaceAnalyzer.ProductionRule, violatingNamespace));
                VerifyDiagnostics(test, testState.GetApprovedNamespaces());
            }
        }

        [Fact]
        public void UnapprovedNamespace_ApplyFix_NoDiagnostic()
        {
            var sampleCode = @"
namespace NationalInstruments.Design.Toolbar
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}";

            using (var testState = new TestState())
            {
                var approvedNamespaceFiles = testState.GetApprovedNamespaces();
                var test = new AutoTestFile(sampleCode);
                var testAfterFix = new TestFile(sampleCode);

                VerifyFix(test, testAfterFix, additionalTestFiles: null, additionalFiles: approvedNamespaceFiles);
                testState.AssertNamespaceExistsInApprovedNamespacesFile("NationalInstruments.Design.Toolbar");
                VerifyDiagnostics(test, approvedNamespaceFiles);
            }
        }

        [Fact]
        public void UnapprovedNamespace_ApproveNamespaceExternally_NoDiagnostic()
        {
            var sampleCode = @"
namespace <?>NationalInstruments.Design.Toolbar
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}";

            using (var testState = new TestState())
            {
                var approvedNamespaceFiles = testState.GetApprovedNamespaces();
                var test = new AutoTestFile(sampleCode, new Rule(ApprovedNamespaceAnalyzer.ProductionRule, "NationalInstruments.Design.Toolbar"));
                var testAfterFix = new AutoTestFile(sampleCode.Replace("<?>", string.Empty));

                VerifyDiagnostics(test, approvedNamespaceFiles);
                testState.AppendToApprovedNamespacesFile("\r\nNationalInstruments.Design.Toolbar");
                VerifyDiagnostics(testAfterFix, approvedNamespaceFiles);
            }
        }

        [Theory]
        [InlineData(
            @"
namespace NationalInstruments.Tests.Design
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}")]
        [InlineData(
            @"
namespace XUnit.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}")]
        public void ApprovedTestNamespace_Verify_NoDiagnostic(string sampleCode)
        {
            using (var testState = new TestState())
            {
                var test = new AutoTestFile(sampleCode);
                VerifyDiagnostics(test, testState.GetApprovedNamespaces());
            }
        }

        [Theory]
        [InlineData(
            @"
namespace <?>NationalInstruments.Tests.Designer
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}", "NationalInstruments.Tests.Designer")]
        [InlineData(
            @"
namespace <?>XUnity.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}", "XUnity.Tests")]
        public void UnapprovedTestNamespace_Verify_EmitsDiagnostic(string sampleCode, string violatingNamespace)
        {
            using (var testState = new TestState())
            {
                var test = new AutoTestFile(sampleCode, new Rule(ApprovedNamespaceAnalyzer.TestRule, violatingNamespace));
                VerifyDiagnostics(test, testState.GetApprovedNamespaces());
            }
        }

        [Fact]
        public void UnapprovedTestNamespace_ApplyFix_NoDiagnostic()
        {
            var sampleCode = @"
namespace NationalInstruments.Tests.Integration.SourceModel
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}";

            using (var testState = new TestState())
            {
                var approvedNamespaceFiles = testState.GetApprovedNamespaces();
                var test = new AutoTestFile(sampleCode);
                var testAfterFix = new TestFile(sampleCode);

                VerifyFix(test, testAfterFix, additionalTestFiles: null, additionalFiles: approvedNamespaceFiles);
                testState.AssertNamespaceExistsInApprovedTestNamespacesFile("NationalInstruments.Tests.Integration.SourceModel");
                VerifyDiagnostics(test, approvedNamespaceFiles);
            }
        }

        [Fact]
        public void UnapprovedTestNamespace_ApproveNamespaceExternally_NoDiagnostic()
        {
            var sampleCode = @"
namespace <?>NationalInstruments.Tests.Integration.SourceModel
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}";

            using (var testState = new TestState())
            {
                var approvedNamespaceFiles = testState.GetApprovedNamespaces();
                var test = new AutoTestFile(sampleCode, new Rule(ApprovedNamespaceAnalyzer.TestRule, "NationalInstruments.Tests.Integration.SourceModel"));
                var testAfterFix = new AutoTestFile(sampleCode.Replace("<?>", string.Empty));

                VerifyDiagnostics(test, approvedNamespaceFiles);
                testState.AppendToApprovedTestNamespacesFile("\r\nNationalInstruments.Tests.Integration.SourceModel");
                VerifyDiagnostics(testAfterFix, approvedNamespaceFiles);
            }
        }

        [Theory]
        [InlineData(
            @"
namespace NationalInstruments.TestUtilities.Mixins
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}")]
        [InlineData(
            @"
namespace XUnit.TestUtilities.Core
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}")]
        public void ApprovedTestUtilitiesNamespace_Verify_NoDiagnostic(string sampleCode)
        {
            using (var testState = new TestState())
            {
                var test = new AutoTestFile(sampleCode);
                VerifyDiagnostics(test, testState.GetApprovedNamespaces());
            }
        }

        [Theory]
        [InlineData(
            @"
namespace <?>NationalInstruments.TestUtilities.Designer
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}", "NationalInstruments.TestUtilities.Designer")]
        [InlineData(
            @"
namespace <?>XUnity.TestUtilities.Core
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}", "XUnity.TestUtilities.Core")]
        public void UnapprovedTestUtilitiesNamespace_Verify_EmitsDiagnostic(string sampleCode, string violatingNamespace)
        {
            using (var testState = new TestState())
            {
                var test = new AutoTestFile(sampleCode, new Rule(ApprovedNamespaceAnalyzer.TestRule, violatingNamespace));
                VerifyDiagnostics(test, testState.GetApprovedNamespaces());
            }
        }

        [Fact]
        public void UnapprovedTestUtilitiesNamespace_ApplyFix_NoDiagnostic()
        {
            var sampleCode = @"
namespace NationalInstruments.TestUtilities.SourceModel
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}";

            using (var testState = new TestState())
            {
                var approvedNamespaceFiles = testState.GetApprovedNamespaces();
                var test = new AutoTestFile(sampleCode);
                var testAfterFix = new TestFile(sampleCode);

                VerifyFix(test, testAfterFix, additionalTestFiles: null, additionalFiles: approvedNamespaceFiles);
                testState.AssertNamespaceExistsInApprovedTestNamespacesFile("NationalInstruments.TestUtilities.SourceModel");
                VerifyDiagnostics(test, approvedNamespaceFiles);
            }
        }

        [Fact]
        public void UnapprovedTestUtilitiesNamespace_ApproveNamespaceExternally_NoDiagnostic()
        {
            var sampleCode = @"
namespace <?>NationalInstruments.TestUtilities.SourceModel
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}";

            using (var testState = new TestState())
            {
                var approvedNamespaceFiles = testState.GetApprovedNamespaces();
                var test = new AutoTestFile(sampleCode, new Rule(ApprovedNamespaceAnalyzer.TestRule, "NationalInstruments.TestUtilities.SourceModel"));
                var testAfterFix = new AutoTestFile(sampleCode.Replace("<?>", string.Empty));

                VerifyDiagnostics(test, approvedNamespaceFiles);
                testState.AppendToApprovedTestNamespacesFile("\r\nNationalInstruments.TestUtilities.SourceModel");
                VerifyDiagnostics(testAfterFix, approvedNamespaceFiles);
            }
        }

        private class TestApprovedNamespaceDocument : TestAdditionalDocument
        {
            public TestApprovedNamespaceDocument(string filePath, string fileName, string text)
                : base(filePath, fileName, text)
            {
            }

            public override SourceText GetText(CancellationToken cancellationToken = default)
            {
                return SourceText.From(File.ReadAllText(Path));
            }
        }

        private class TestState : IDisposable
        {
            private const string ApprovedNamespacesFileName = "NI1017_ApprovedNamespaces.txt";
            private const string ApprovedTestNamespacesFileName = "NI1017_ApprovedTestNamespaces.txt";
            private const string ApprovedNamespaces = @"
                NationalInstruments.Design
                NationalInstruments.SourceModel
                System.*
                SomeNamespace.T??l";

            private const string ApprovedTestNamespaces = @"
                NationalInstruments.Tests.Design
                NationalInstruments.Tests.SourceModel
                NationalInstruments.TestUtilities.Mixins
                NationalInstruments.TestUtilities.VI
                XUnit.*";

            private readonly string _testFilesFolder;
            private TestAdditionalDocument _approvedNamespacesDocument;
            private TestAdditionalDocument _approvedTestNamespacesDocument;

            public TestState()
            {
                _testFilesFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(_testFilesFolder);
            }

            private string ApprovedNamespacesFilePath => Path.Combine(_testFilesFolder, ApprovedNamespacesFileName);

            private string ApprovedTestNamespacesFilePath => Path.Combine(_testFilesFolder, ApprovedTestNamespacesFileName);

            public void Dispose()
            {
                Directory.Delete(_testFilesFolder, recursive: true);
            }

            public TestAdditionalDocument[] GetApprovedNamespaces()
            {
                if (_approvedNamespacesDocument == null)
                {
                    _approvedNamespacesDocument = GetApprovedNamespacesFromString(ApprovedNamespacesFilePath, ApprovedNamespaces);
                }

                if (_approvedTestNamespacesDocument == null)
                {
                    _approvedTestNamespacesDocument = GetApprovedNamespacesFromString(ApprovedTestNamespacesFilePath, ApprovedTestNamespaces);
                }

                return new[] { _approvedNamespacesDocument, _approvedTestNamespacesDocument };

                TestAdditionalDocument GetApprovedNamespacesFromString(string filePath, string namespaces)
                {
                    File.WriteAllText(filePath, namespaces);
                    return new TestApprovedNamespaceDocument(filePath, Path.GetFileName(filePath), namespaces);
                }
            }

            public void AppendToApprovedNamespacesFile(string namespaceName)
            {
                File.AppendAllLines(_approvedNamespacesDocument.Path, new[] { namespaceName });
            }

            public void AppendToApprovedTestNamespacesFile(string namespaceName)
            {
                File.AppendAllLines(_approvedTestNamespacesDocument.Path, new[] { namespaceName });
            }

            public void AssertNamespaceExistsInApprovedNamespacesFile(string namespaceName)
            {
                AssertNamespaceExistsInFile(_approvedNamespacesDocument.Path, namespaceName);
            }

            public void AssertNamespaceExistsInApprovedTestNamespacesFile(string namespaceName)
            {
                AssertNamespaceExistsInFile(_approvedTestNamespacesDocument.Path, namespaceName);
            }

            private void AssertNamespaceExistsInFile(string filePath, string namespaceName)
            {
                Assert.NotNull(File
                    .ReadAllLines(filePath)
                    .Select(l => l.Trim())
                    .FirstOrDefault(n => n == namespaceName));
            }
        }
    }
}

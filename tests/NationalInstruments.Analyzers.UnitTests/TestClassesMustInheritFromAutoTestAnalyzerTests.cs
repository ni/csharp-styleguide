using NationalInstruments.Analyzers.Correctness;
using NationalInstruments.Analyzers.TestUtilities;
using NationalInstruments.Analyzers.TestUtilities.TestFiles;
using NationalInstruments.Analyzers.TestUtilities.Verifiers;
using Xunit;

namespace NationalInstruments.Analyzers.UnitTests
{
    /// <summary>
    /// Tests that the <see cref="TestClassesMustInheritFromAutoTestAnalyzer" /> emits a diagnostic when a class with the
    /// <c>Microsoft.VisualStudio.TestTools.UnitTesting.TestClass</c> attribute does not directly or indirectly inherit
    /// from <c>NationalInstruments.Core.TestUtilities.AutoTest</c>.
    /// </summary>
    public sealed class TestClassesMustInheritFromAutoTestAnalyzerTests : NIDiagnosticAnalyzerTests<TestClassesMustInheritFromAutoTestAnalyzer>
    {
        private const string AutoTest = @"
using System.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalInstruments.Core.TestUtilities;

namespace NationalInstruments.Core.TestUtilities
{
    public class AutoTest
    {
    }
}";

        [Fact]
        public void NI1007_TestClassInheritsFromAutoTestDirectly_NoDiagnostic()
        {
            var test = new TestFile(AutoTest + @"
[TestClass]
class MyUnitTests : AutoTest
{
    [TestMethod]
    public void MyTestMethod()
    {
    }
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1007_TestClassInheritsFromAutoTestIndirectly_NoDiagnostic()
        {
            var test = new TestFile(AutoTest + @"
abstract class MyTestBase : AutoTest
{
}

[TestClass]
class MyUnitTests : MyTestBase
{
    [TestMethod]
    public void MyTestMethod()
    {
    }
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1007_PartialTestClassInheritsFromAutoTest_NoDiagnostic()
        {
            var test = new TestFile(AutoTest + @"
partial class MyUnitTests : AutoTest
{
    [TestMethod]
    public void MyTestMethod1()
    {
    }
}

[TestClass]
partial class MyUnitTests
{
    [TestMethod]
    public void MyTestMethod2()
    {
    }
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1007_TestClassDoesNotInheritFromAutoTest_Diagnostic()
        {
            var test = new TestFile(@"
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
class MyUnitTests
{
    [TestMethod]
    public void MyTestMethod()
    {
    }
}");

            VerifyDiagnostics(test, GetNI1007ResultAt(4, 0, "MyUnitTests"));
        }

        private DiagnosticResult GetNI1007ResultAt(int line, int column, string className)
        {
            return GetResultAt(line, column, TestClassesMustInheritFromAutoTestAnalyzer.Rule, className);
        }
    }
}

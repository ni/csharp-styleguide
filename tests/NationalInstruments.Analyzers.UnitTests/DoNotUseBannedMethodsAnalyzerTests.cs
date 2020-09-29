using System;
using System.IO;
using System.Linq;
using NationalInstruments.Analyzers.Correctness;
using NationalInstruments.Analyzers.TestUtilities;
using NationalInstruments.Analyzers.TestUtilities.TestFiles;
using NationalInstruments.Analyzers.TestUtilities.Verifiers;
using Xunit;

namespace NationalInstruments.Analyzers.UnitTests
{
    /// <summary>
    /// Tests that the <see cref="DoNotUsedBannedMethodsAnalyzer" /> emits two different violations when necessary:
    /// 1. method is banned and 2. banned methods file has invalid xml.
    /// </summary>
    public sealed class DoNotUseBannedMethodsAnalyzerTests : NIDiagnosticAnalyzerTests<DoNotUseBannedMethodsAnalyzer>
    {
        private const string ExampleBannedMethodsFileName = "BannedMethods.xml";

        private static string _instanceMethodCallingFooMethod = @"
class Foo
{
    public void Method()
    {
    }
}

class Program
{
    static void Main(string[] args)
    {
        var foo = new Foo();
        <?>foo.Method();
    }
}";

        private static string _instanceMethodCallingFooMethodNoDiagnostic = @"
class Foo
{
    public void Method()
    {
    }
}

class Program
{
    static void Main(string[] args)
    {
        var foo = new Foo();
        foo.Method();
    }
}";

        [Fact]
        public void NI1006_MethodNotBanned_NoDiagnostic()
        {
            var test = new AutoTestFile(@"
using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(""foo"");
    }
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1006_InstanceMethodBanned_Diagnostic()
        {
            var test = new AutoTestFile(
                _instanceMethodCallingFooMethod,
                GetNI1006BannedMethodRule("Foo.Method()"));

            VerifyDiagnostics(test, GetBannedMethodsFile("Foo.Method"));
        }

        [Fact]
        public void NI1006_MethodBannedWithJustification_Diagnostic()
        {
            var test = new AutoTestFile(
                _instanceMethodCallingFooMethod,
                GetNI1006BannedMethodRule("Foo.Method()", ", Justification: abc"));

            var file = GetBannedMethodsFromXml(@"<BannedMethods><Entry Justification=""abc"">Foo.Method</Entry></BannedMethods>");
            VerifyDiagnostics(test, file);
        }

        [Fact]
        public void NI1006_MethodBannedWithJustificationAndAlternative_Diagnostic()
        {
            var test = new AutoTestFile(
                _instanceMethodCallingFooMethod,
                GetNI1006BannedMethodRule("Foo.Method()", ", Alternative: abc, Justification: xyz"));

            var file = GetBannedMethodsFromXml(@"<BannedMethods><Entry Justification=""xyz"" Alternative=""abc"">Foo.Method</Entry></BannedMethods>");
            VerifyDiagnostics(test, file);
        }

        [Fact]
        public void NI1006_MethodBannedWithJustificationAndAlternativeOnEntryGroup_Diagnostic()
        {
            var test = new AutoTestFile(
                _instanceMethodCallingFooMethod,
                GetNI1006BannedMethodRule("Foo.Method()", ", Alternative: abc, Justification: xyz"));

            var file = GetBannedMethodsFromXml(@"<BannedMethods><EntryGroup Justification=""xyz"" Alternative=""abc""><Entry>Foo.Method</Entry></EntryGroup></BannedMethods>");
            VerifyDiagnostics(test, file);
        }

        [Fact]
        public void NI1006_MethodBannedWithJustificationOnEntryGroupAndAlternativeOnEntry_Diagnostic()
        {
            var test = new AutoTestFile(
                _instanceMethodCallingFooMethod,
                GetNI1006BannedMethodRule("Foo.Method()", ", Alternative: abc, Justification: xyz"));

            var file = GetBannedMethodsFromXml(@"<BannedMethods><EntryGroup Justification=""xyz""><Entry Alternative=""abc"">Foo.Method</Entry></EntryGroup></BannedMethods>");
            VerifyDiagnostics(test, file);
        }

        [Fact]
        public void NI1006_MethodBannedWithJustificationOnEntryAndEntryGroup_Diagnostic()
        {
            var test = new AutoTestFile(
                _instanceMethodCallingFooMethod,
                GetNI1006BannedMethodRule("Foo.Method()", ", Justification: abc"));

            var file = GetBannedMethodsFromXml(@"<BannedMethods><EntryGroup Justification=""xyz""><Entry Justification=""abc"">Foo.Method</Entry></EntryGroup></BannedMethods>");
            VerifyDiagnostics(test, file);
        }

        [Fact]
        public void NI1006_MethodBannedWithJustificationAndAlternativeOnNestedEntryGroups_Diagnostic()
        {
            var test = new AutoTestFile(
                _instanceMethodCallingFooMethod,
                GetNI1006BannedMethodRule("Foo.Method()", ", Alternative: abc, Justification: xyz"));

            var file = GetBannedMethodsFromXml(@"<BannedMethods><EntryGroup><EntryGroup Justification=""xyz""><EntryGroup Alternative=""abc""><Entry>Foo.Method</Entry></EntryGroup></EntryGroup></EntryGroup></BannedMethods>");
            VerifyDiagnostics(test, file);
        }

        [Fact]
        public void NI1006_MethodBannedButNotInThisAssembly_NoDiagnostic()
        {
            var test = new AutoTestFile(
                "MyAssembly",
                _instanceMethodCallingFooMethodNoDiagnostic);

            var file = GetBannedMethodsFromXml(@"<BannedMethods><Entry Assemblies=""NotMyAssembly"">Foo.Method</Entry></BannedMethods>");
            VerifyDiagnostics(test, file);
        }

        [Fact]
        public void NI1006_MethodBannedInThisAssemblyAndOthers_Diagnostic()
        {
            var test = new AutoTestFile(
                "MyAssemblyName",
                _instanceMethodCallingFooMethod,
                GetNI1006BannedMethodRule("Foo.Method()", ", Banned in this assembly."));

            var file = GetBannedMethodsFromXml(@"<BannedMethods><Entry Assemblies=""SomethingUnrelated,MyAssemblyName"">Foo.Method</Entry></BannedMethods>");
            VerifyDiagnostics(test, file);
        }

        [Fact]
        public void NI1006_MethodBannedInThisAssemblyAndOthersOnEntryGroup_Diagnostic()
        {
            var test = new AutoTestFile(
                "MyAssemblyName",
                _instanceMethodCallingFooMethod,
                GetNI1006BannedMethodRule("Foo.Method()", ", Banned in this assembly."));

            var file = GetBannedMethodsFromXml(@"<BannedMethods><EntryGroup Assemblies=""SomethingUnrelated,MyAssemblyName""><Entry>Foo.Method</Entry></EntryGroup></BannedMethods>");
            VerifyDiagnostics(test, file);
        }

        [Fact]
        public void NI1006_MethodBannedInThisAssemblyWithOnePartSubstring_Diagnostic()
        {
            var test = new AutoTestFile(
                "AssemblyPart1.AssemblyPart2.Assembly",
                _instanceMethodCallingFooMethod,
                GetNI1006BannedMethodRule("Foo.Method()", ", Banned in this assembly."));

            var file = GetBannedMethodsFromXml(@"<BannedMethods><Entry Assemblies=""SomethingUnrelated,AssemblyPart1"">Foo.Method</Entry></BannedMethods>");
            VerifyDiagnostics(test, file);
        }

        [Fact]
        public void NI1006_MethodBannedInThisAssemblyWithTwoPartSubstring_Diagnostic()
        {
            var test = new AutoTestFile(
                "AssemblyPart1.AssemblyPart2.Assembly",
                _instanceMethodCallingFooMethod,
                GetNI1006BannedMethodRule("Foo.Method()", ", Banned in this assembly."));

            var file = GetBannedMethodsFromXml(@"<BannedMethods><Entry Assemblies=""SomethingUnrelated,AssemblyPart1.AssemblyPart2"">Foo.Method</Entry></BannedMethods>");
            VerifyDiagnostics(test, file);
        }

        [Fact]
        public void NI1006_MethodBannedInThisAssemblyWithFullSubstring_Diagnostic()
        {
            var test = new AutoTestFile(
                "AssemblyPart1.AssemblyPart2.Assembly",
                _instanceMethodCallingFooMethod,
                GetNI1006BannedMethodRule("Foo.Method()", ", Banned in this assembly."));

            var file = GetBannedMethodsFromXml(@"<BannedMethods><Entry Assemblies=""SomethingUnrelated,AssemblyPart1.AssemblyPart2.Assembly"">Foo.Method</Entry></BannedMethods>");
            VerifyDiagnostics(test, file);
        }

        [Fact]
        public void NI1006_MethodBannedInCallerAssemblyAndNotCallee_Diagnostic()
        {
            var testCalleeSource = @"
public class Foo
{
    public void Method()
    {
    }
}";
            var testCallee = new AutoTestFile("CalleeAssembly", testCalleeSource);

            var testCallerSource = @"
class Program
{
    static void Main(string[] args)
    {
        var foo = new Foo();
        <?>foo.Method();
    }
}";
            var testCaller = new AutoTestFile(
                null,
                "CallerAssembly",
                testCallerSource,
                new string[] { "CalleeAssembly" },
                GetNI1006BannedMethodRule("Foo.Method()", ", Banned in this assembly."));

            var file = GetBannedMethodsFromXml(@"<BannedMethods><Entry Assemblies=""CallerAssembly,Unrelated"">Foo.Method</Entry></BannedMethods>");
            VerifyDiagnostics(new[] { testCallee, testCaller }, file);
        }

        [Fact]
        public void NI1006_MethodBannedInCalleeAssemblyAndNotCaller_NoDiagnostic()
        {
            var testCalleeSource = @"
public class Foo
{
    public void Method()
    {
    }
}";
            var testCallee = new AutoTestFile("CalleeAssembly", testCalleeSource);

            var testCallerSource = @"
class Program
{
    static void Main(string[] args)
    {
        var foo = new Foo();
        foo.Method();
    }
}";
            var testCaller = new AutoTestFile(null, "CallerAssembly", testCallerSource, new string[] { "CalleeAssembly" });

            var file = GetBannedMethodsFromXml(@"<BannedMethods><Entry Assemblies=""CalleeAssembly,Unrelated"">Foo.Method</Entry></BannedMethods>");
            VerifyDiagnostics(new[] { testCallee, testCaller }, file);
        }

        [Fact]
        public void NI1006_StaticMethodBanned_Diagnostic()
        {
            var test = new AutoTestFile(
                @"
using System;

class Program
{   
    static void Main(string[] args) 
    {
        <?>Console.WriteLine(""foo"");
    }
}",
                GetNI1006BannedMethodRule("System.Console.WriteLine(System.String)"));

            VerifyDiagnostics(test, GetBannedMethodsFile("System.Console"));
        }

        [Fact]
        public void NI1006_ConstructorBanned_Diagnostic()
        {
            var test = new AutoTestFile(
                @"
class Foo
{
}

class Program
{
    private Foo _foo = <?>new Foo();
}",
                GetNI1006BannedMethodRule("Foo.Foo()"));

            VerifyDiagnostics(test, GetBannedMethodsFile("Foo."));
        }

        [Fact]
        public void NI1006_ExtensionMethodBanned_Diagnostic()
        {
            var test = new AutoTestFile(
                @"
static class Extensions
{
    public static string Reverse(this string value)
    {
        return value;
    }
}

class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine(<?>""hello"".Reverse());
    }
}",
                GetNI1006BannedMethodRule("Extensions.Reverse()"));

            VerifyDiagnostics(test, GetBannedMethodsFile("Extensions.Reverse"));
        }

        [Fact]
        public void NI1006_MethodNotBanned_NamespaceBeginningDiffers_NoDiagnostic()
        {
            var test = new AutoTestFile(@"
static class Console
{
    public static void WriteLine(string message)
    {        
    }
}

class Program
{   
    static void Main(string[] args) 
    {
        Console.WriteLine(""foo"");
    }
}");

            VerifyDiagnostics(test, new[] { GetBannedMethodsFile("System.Console") });
        }

        [Fact]
        public void NI1006_MethodNotBanned_NamespaceEndingDiffers_NoDiagnostic()
        {
            var test = new AutoTestFile(@"
namespace System
{
    public static class ConsoleHelper
    {
        public static void WriteLine(string message)
        {
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        System.ConsoleHelper.WriteLine(""foo"");
    }
}");

            VerifyDiagnostics(test, new[] { GetBannedMethodsFile("System.Console") });
        }

        [Fact]
        public void NI1006_MethodNotBanned_NameFoolsUnescapedRegex_NoDiagnostic()
        {
            var test = new AutoTestFile(@"
static class SystemAConsole
{
    public static void WriteLine(string message)
    {
    }
}

class Program
{
    static void Main(string[] args)
    {
        SystemAConsole.WriteLine(""foo"");
    }
}");

            VerifyDiagnostics(test, new[] { GetBannedMethodsFile("System.Console") });
        }

        [Fact]
        public void NI1006_MethodsBannedFromMultipleFiles_Diagnostics()
        {
            var test = new AutoTestFile(
                @"
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var sum = <?>new[] { 1, 2, 3 }.Sum();
        <?>System.Console.WriteLine(sum);
    }
}",
                GetNI1006BannedMethodRule("System.Linq.Enumerable.Sum()"),
                GetNI1006BannedMethodRule("System.Console.WriteLine(System.Int32)"));

            var additionalFiles = new[]
            {
                GetBannedMethodsFile(new FileInfo("BannedMethodsProjectA.xml"), "System.Console.WriteLine"),
                GetBannedMethodsFile(new FileInfo("BannedMethodsProjectB.xml"), "System.Linq.Enumerable"),
            };
            VerifyDiagnostics(test, additionalFiles);
        }

        [Fact]
        public void NI1006_BannedMethodsFileHasInvalidXml_Diagnostic()
        {
            var test = new TestFile(@"
class Program
{
    static void Main(string[] args)
    {
    }
}");

            var invalidBannedMethodsFileSource = @"
<BannedMethods>
    <Entry>System.Console</Entry>";
            var invalidBannedMethodsFile = new TestAdditionalDocument(ExampleBannedMethodsFileName, invalidBannedMethodsFileSource);

            VerifyDiagnostics(
                test,
                new[] { invalidBannedMethodsFile },
                expectedDiagnostics: GetNI1006XmlErrorResult(
                    ExampleBannedMethodsFileName,
                    "Unexpected end of file has occurred. The following elements are not closed: BannedMethods. Line 3, position 34."));
        }

        [Fact]
        public void NI1006_BannedMethodsFileHasXmlWithWrongChildElement_Diagnostic()
        {
            var test = new TestFile(@"
class Program
{
    static void Main(string[] args)
    {
    }
}");

            var invalidBannedMethodsFile = new TestAdditionalDocument(
                ExampleBannedMethodsFileName,
                @"<BannedMethods><Entry>System.Console</Entry><WrongElement>System.Diagnostics</WrongElement></BannedMethods>");

            VerifyDiagnostics(
                test,
                new[] { invalidBannedMethodsFile },
                expectedDiagnostics: GetNI1006XmlErrorResult(
                    ExampleBannedMethodsFileName,
                    "Unsupported element in BannedMethods.xml: WrongElement"));
        }

        [Fact]
        public void NI1006_BannedMethodsFileHasXmlWithWrongRootElement_Diagnostic()
        {
            var test = new TestFile(@"
class Program
{
    static void Main(string[] args)
    {
    }
}");

            var invalidBannedMethodsFile = new TestAdditionalDocument(
                ExampleBannedMethodsFileName,
                @"<WrongRootTag><Entry>System.Console</Entry></WrongRootTag>");

            VerifyDiagnostics(
                test,
                new[] { invalidBannedMethodsFile },
                expectedDiagnostics: GetNI1006XmlErrorResult(
                    ExampleBannedMethodsFileName,
                    "BannedMethods.xml must have a root element of <BannedMethods>"));
        }

        private Rule GetNI1006BannedMethodRule(string bannedMethodName)
        {
            return GetNI1006BannedMethodRule(bannedMethodName, string.Empty);
        }

        private Rule GetNI1006BannedMethodRule(string bannedMethodName, string additionalText)
        {
            return new Rule(DoNotUseBannedMethodsAnalyzer.Rule, bannedMethodName, additionalText);
        }

        private DiagnosticResult GetNI1006XmlErrorResult(string fileName, string exceptionMessage)
        {
            return GetResult(fileName, DoNotUseBannedMethodsAnalyzer.FileParseRule, exceptionMessage);
        }

        private TestAdditionalDocument GetBannedMethodsFile(params string[] bannedMethods)
        {
            return GetBannedMethodsFile(null, bannedMethods);
        }

        private TestAdditionalDocument GetBannedMethodsFile(FileInfo fileInfo, params string[] bannedMethods)
        {
            var xml = $@"
<BannedMethods>
    {string.Join(Environment.NewLine, bannedMethods.Select(x => $"<Entry>{x}</Entry>"))}
</BannedMethods>";
            return GetBannedMethodsFromXml(fileInfo, xml);
        }

        private TestAdditionalDocument GetBannedMethodsFromXml(string xml)
        {
            return GetBannedMethodsFromXml(null, xml);
        }

        private TestAdditionalDocument GetBannedMethodsFromXml(FileInfo fileInfo, string xml)
        {
            var defaultFileInfo = new FileInfo(ExampleBannedMethodsFileName);

            return new TestAdditionalDocument(
                (fileInfo ?? defaultFileInfo).Name,
                xml);
        }
    }
}

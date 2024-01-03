using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NationalInstruments.Analyzers.Correctness;
using NationalInstruments.Analyzers.TestUtilities;
using NationalInstruments.Analyzers.TestUtilities.TestFiles;
using NationalInstruments.Analyzers.TestUtilities.Verifiers;
using Xunit;

namespace NationalInstruments.Analyzers.UnitTests
{
    public sealed partial class StringsShouldBeInResourcesAnalyzerTests : NIDiagnosticAnalyzerTests<StringsShouldBeInResourcesAnalyzer>
    {
        private const string TestAssemblyName = "TestProject";

        private const string ExampleLiteral = "example";
        private const string ExampleLiteralExemptionsFileName = "LiteralExemptions.xml";

        private const string ExampleStaticMethodTestTemplate = @"
using System.Text;

class Program
{{
    private Encoding _encoding = Encoding.GetEncoding({1}""{0}"");
}}";

        private const string ExampleExtensionMethodTestTemplate = @"
namespace Example
{{
    public static class Extensions
    {{
        public static string NotLocalized(this string text)
        {{
            return text;
        }}
    }}

    class Program
    {{
        private string _field = {1}""{0}"".NotLocalized();
    }}
}}";

        private const string ExampleInstanceMethodTestTemplate = @"
namespace Numbers
{{
    public class ComplexDouble
    {{
        public string ToString(string format)
        {{
            return format;
        }}
    }}
}}

class Program
{{
    public Program()
    {{
        var complexDouble = new Numbers.ComplexDouble();
        var numString = complexDouble.ToString({1}""{0}"");
    }}
}}";

        private const string ExampleConstructorMethodTestTemplate = @"
using System.IO;

class Program
{{
    public Program()
    {{
        var reader = new StringReader({1}""{0}"");
    }}
}}";

        private const string ExampleGenericMethodTestTemplate = @"
class Program
{{
    public void Method<TElement>(TElement element)
    {{
    }}

    public Program()
    {{
        Method({1}""{0}"");
    }}
}}";

        private const string ExampleClassTestTemplate = @"
namespace My.Namespace
{{
    class ExemptClass
    {{
        public void Method(string name)
        {{
        }}
    }}

    class Program
    {{
        public Program()
        {{
            var type = new ExemptClass();
            type.Method({1}""{0}"");
        }}
    }}
}}";

        private const string ExampleStructTestTemplate = @"
namespace My.Namespace
{{
    struct ExemptStruct
    {{
        public ExemptStruct(string name)
        {{
        }}
    }}

    class Program
    {{
        public Program()
        {{
            var type = new ExemptStruct({1}""{0}"");
        }}
    }}
}}";

        private const string ExampleAbstractClassTestTemplate = @"
namespace My.Namespace
{{
    abstract class ExemptClassBase
    {{
        public abstract void Method(string name);
    }}

    class ExemptClass : ExemptClassBase
    {{
        public override void Method(string name)
        {{
        }}
    }}

    class Program
    {{
        public Program()
        {{
            var type = new ExemptClass();
            type.Method({1}""{0}"");
        }}
    }}
}}";

        private const string ExampleGenericMethodName = "Program.Method&lt;System.String&gt;(System.String)";

        private const string AcceptsStringLiteralArgumentsAttribute = @"
class AcceptsStringLiteralArgumentsAttribute : System.Attribute
{
    public string Scope { get; set; }
    public string Target { get; set; }
}";

        private const string ImplementationAllowedToUseStringLiteralsAttribute = @"
class ImplementationAllowedToUseStringLiteralsAttribute : System.Attribute
{
    public string Justification { get; set; }
}";

        private const string AllowThisNonLocalizedLiteralAttribute = @"
class AllowThisNonLocalizedLiteralAttribute : System.Attribute
{
    public string Literal { get; set; }
    public string Justification { get; set; }
}";

        private const string ExemptFromStringLiteralsRuleAttribute = @"
class ExemptFromStringLiteralsRuleAttribute : System.Attribute
{
    public string Scope { get; set; }
    public string Target { get; set; }
    public string Justification { get; set; }
}";

        private const string AllowExternalCodeToAcceptStringLiteralArgumentsAttribute = @"
class AllowExternalCodeToAcceptStringLiteralArgumentsAttribute : System.Attribute
{
    public string Scope { get; set; }
    public string Target { get; set; }
}";

        private const string AllExemptionTypesXml = @"
<Field>A</Field>
<String>A</String>
<Assembly>A</Assembly>
<Namespace>A</Namespace>
<Type>A</Type>
<Type>A</Type>
<Member>A</Member>
<FilenameMatcher>A</FilenameMatcher>";

        public static IEnumerable<object[]> AllModifications
        {
            get
            {
                foreach (var accessibility in new[] { "public", "protected", "internal", "protected internal", "private", string.Empty })
                {
                    foreach (var modifier in new[] { "readonly", "const", string.Empty })
                    {
                        yield return new[] { accessibility.Length > 0 ? $"{accessibility} {modifier}" : $"{accessibility}{modifier}" };
                    }
                }
            }
        }

        public static IEnumerable<object[]> ScopeExemptAttributes =>
            new[]
            {
                new object[]
                {
                    ImplementationAllowedToUseStringLiteralsAttribute,
                    "[ImplementationAllowedToUseStringLiterals]",
                },
                new object[]
                {
                    ExemptFromStringLiteralsRuleAttribute,
                    @"[ExemptFromStringLiteralsRule(Scope = ""Disabled"")]",
                },
                new object[]
                {
                    ExemptFromStringLiteralsRuleAttribute,
                    @"[ExemptFromStringLiteralsRule]",
                },
            };

        [Fact]
        public void NI1004_NoStringLiteral_NoDiagnostic()
        {
            var test = new AutoTestFile(@"
using System;

class Program
{   
    static void Main(string[] args) 
    {
        Console.WriteLine(1.ToString());
    }
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralsInAttribute_NoDiagnostics()
        {
            var test = new AutoTestFile(@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    [SuppressMessage(""SomeRule"", ""NIXXXX:SomeCustomRule"", Justification = ""TODO DOC"")]
    public void Method()
    {
    }
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralInDefaultParameterValue_Diagnostic()
        {
            var test = new AutoTestFile(
                @"
class Program
{
    public void Method(string name = <?>""exempt"")
    {
    }
}",
                GetNI1004LiteralRule("exempt"));

            VerifyDiagnostics(test);
        }

        [Theory]
        [InlineData("{0:G}")] // string substitutions
        [InlineData("777")] // digits
        [InlineData("  \t")] // whitespace
        [InlineData(".?")] // punctuation
        [InlineData("\u00200\u0080")] // unicode control/data
        [InlineData("+-*/%^&()@#")] // symbols
        [InlineData(" #FF0077\t")] // colors
        [InlineData("{586FF4E5-5B1A-4047-8906-B97594A9B91B}")] // guids
        public void NI1004_StringLiteralContainsExemptCharacters_NoDiagnostic(string literal)
        {
            var test = new AutoTestFile($@"
class Program
{{
    private string _field = ""{literal}"";
}}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralContainsNonExemptBraces_Diagnostic()
        {
            var test = new AutoTestFile(
                @"
class Program
{
    public string NonExemptField = <?>""{{0:G}}"";    // has to be a non-escaped string substitution
}",
                GetNI1004LiteralRule("{{0:G}}"));

            VerifyDiagnostics(test);
        }

        [Theory]
        [InlineData("Exempt", "exempt")]
        [InlineData("This is an exempt string with words", "This is an exempt string with words")]
        [InlineData("Exempt.exe", "Exempt.exe")]
        public void NI1004_StringLiteralMatchesExemptString_NoDiagnostic(string literal, string exemption)
        {
            var test = new AutoTestFile($@"
class Program
{{
    private string _field = ""{literal}"";
}}");

            VerifyDiagnostics(test, new[] { GetExemptionsFile($"<String>{exemption}</String>") });
        }

        [Theory]
        [InlineData("non-exempt", "non-exemp")]
        [InlineData("non-exempt", "on-exempt")]
        public void NI1004_StringLiteralDoesNotMatchExemptString_Diagnostic(string literal, string exemption)
        {
            var test = new AutoTestFile(
                $@"
class Program
{{
    private string _field = <?>""{literal}"";
}}",
                GetNI1004LiteralRule(literal));

            VerifyDiagnostics(test, GetExemptionsFile($"<String>{exemption}</String>"));
        }

        [Fact]
        public void NI1004_StringLiteralMatchesExemptString_ExemptFromAttribute_NoDiagnostic()
        {
            var test = new AutoTestFile(
                $@"
{ExemptFromStringLiteralsRuleAttribute}

[ExemptFromStringLiteralsRule(Scope = ""Constant"", Target = ""exempt"")]
class Program
{{
    private const string Name1 = <?>""{ExampleLiteral}"";
    private const string Name2 = ""exempt"";

    public Program()
    {{
        System.Console.WriteLine(""exempt"");
    }}
}}",
                GetNI1004LiteralRule(ExampleLiteral));

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralMatchesExemptString_ExemptFromAssemblyMetadata_NoDiagnostic()
        {
            var assemblyInfo = new AutoTestFile(@"[assembly: System.Reflection.AssemblyMetadata(""Localize.Constant"", ""exempt"")]");

            var test = new AutoTestFile(@"
public class Program
{
    private string _name = ""exempt"";

    public Program()
    {
        System.Console.WriteLine(""exempt"");
    }
}");

            VerifyDiagnostics(new[] { assemblyInfo, test });
        }

        [Theory]
        [InlineData("*")]
        [InlineData("exempt*")]
        [InlineData("*string")]
        [InlineData("ex*ring")]
        [InlineData("*pt-st*")]
        [InlineData("exempt-string*")]
        [InlineData("*exempt-string")]
        [InlineData("*exempt-string*")]
        public void NI1004_StringLiteralMatchesExemptWildcardString_NoDiagnostic(string exemptWildcard)
        {
            var test = new AutoTestFile(@"
class Program
{
    private string _field = ""exempt-string"";
}");

            VerifyDiagnostics(test, GetExemptionsFile($"<String>{exemptWildcard}</String>"));
        }

        [Theory]
        [InlineData("*not*exempt*")]
        [InlineData("exempt*")]
        [InlineData("*non")]
        public void NI1004_StringLiteralDoesNotMatchExemptWildcardString_Diagnostic(string exemptWildcard)
        {
            var test = new AutoTestFile(
                @"
class Program
{
    private string _field = <?>""non-exempt string"";
}",
                GetNI1004LiteralRule("non-exempt string"));

            VerifyDiagnostics(test, GetExemptionsFile($"<String>{exemptWildcard}</String>"));
        }

#pragma warning disable SA1124 // Do not use regions
        #region Constants

        [Fact]
        public void NI1004_StringLiteralInConstant_ExemptFromAttribute_NoDiagnostic()
        {
            var test = new AutoTestFile(AllowThisNonLocalizedLiteralAttribute + @"
[AllowThisNonLocalizedLiteral(Literal = Exempt)]
class Program
{
    private const string Exempt = ""exempt"";

    public Program()
    {
        System.Console.WriteLine(""exempt""); // prove it's not looking for the constant's value
    }
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralInConstant_ExemptFromAssemblyAttribute_NoDiagnostic()
        {
            var test1 = new AutoTestFile(@"
class Program
{
    public const string Exempt = ""exempt"";
}");

            var test2 = new AutoTestFile($@"
[assembly: AllowThisNonLocalizedLiteral(Literal = Program.Exempt)]

{AllowThisNonLocalizedLiteralAttribute}");

            VerifyDiagnostics(new[] { test1, test2 });
        }

        [Fact]
        public void NI1004_StringLiteralInConstant_NotExemptFromAssemblyAttribute_Diagnostic()
        {
            var test1Source = $@"
public class Program
{{
    public const string Exempt = <?>""{ExampleLiteral}"";
}}";
            var test1 = new AutoTestFile("ProjectA", test1Source, GetNI1004LiteralRule(ExampleLiteral));

            var test2Source = $@"
[assembly: AllowThisNonLocalizedLiteral(Literal = Program.Exempt)]

{AllowThisNonLocalizedLiteralAttribute}

public class Program
{{
    public const string Exempt = ""exempt"";
}}";
            var test2 = new AutoTestFile("ProjectB", test2Source);

            VerifyDiagnostics(new[] { test1, test2 });
            VerifyDiagnostics(new[] { test2, test1 });
        }

        #endregion  // Constants

        #region Fields

        [Theory]
        [MemberData(nameof(AllModifications))]
        public void NI1004_StringLiteralInFieldScope_Diagnostic(string modifiers)
        {
            var test = new AutoTestFile(
                $@"
class Program
{{
    {modifiers} string _field = <?>""{ExampleLiteral}"";
}}",
                GetNI1004LiteralRule(ExampleLiteral));

            VerifyDiagnostics(test);
        }

        [Theory]
        [InlineData("<Field>Program.Field</Field>")]
        [InlineData(@"<Field AppliesTo=""Scope"">Program.Field</Field>")]
        [InlineData("<Field>Program.*</Field>")]
        [InlineData(@"<Field AppliesTo=""Scope"">Program.*</Field>")]
        [InlineData("<Field>*.Field</Field>")]
        [InlineData(@"<Field AppliesTo=""Scope"">*.Field</Field>")]
        public void NI1004_StringLiteralInFieldScope_ExemptFromFile_NoDiagnostic(string exemption)
        {
            var test = new AutoTestFile(@"
class Program
{
    public string Field = ""exempt"";
}");

            VerifyDiagnostics(test, GetExemptionsFile(exemption));
        }

        [Fact]
        public void NI1004_StringLiteralInFieldScope_InvocationExemptFromFile_Diagnostic()
        {
            var test = new AutoTestFile(
                $@"
class Program
{{
    public string Field = <?>""{ExampleLiteral}"";
}}",
                GetNI1004LiteralRule(ExampleLiteral));

            VerifyDiagnostics(test, GetExemptionsFile(@"<Field AppliesTo=""Invocation"">Program.Field</Field>"));
        }

        [Theory]
        [MemberData(nameof(ScopeExemptAttributes))]
        public void NI1004_StringLiteralInFieldScope_ExemptFromAttribute_NoDiagnostic(string attributeDefintion, string attribute)
        {
            var test = new AutoTestFile($@"
{attributeDefintion}

class Program
{{
    {attribute}
    private string _field = ""exempt"";
}}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralInFieldScope_InvocationExemptFromAttribute_Diagnostic()
        {
            var test = new AutoTestFile(
                $@"
{AcceptsStringLiteralArgumentsAttribute}

class Program
{{
    [AcceptsStringLiteralArguments]
    private string _field = <?>""{ExampleLiteral}"";
}}",
                GetNI1004LiteralRule(ExampleLiteral));

            VerifyDiagnostics(test);
        }

        [Theory]
        [InlineData("<Field>Foo.Field</Field>")]
        [InlineData(@"<Field AppliesTo=""Invocation"">Foo.Field</Field>")]
        public void NI1004_StringLiteralInFieldInvocation_ExemptFromFile_NoDiagnostic(string exemption)
        {
            var test = new AutoTestFile(@"
class Foo
{
    public string Field;
}

class Program
{
    public Program()
    {
        var foo = new Foo();
        foo.Field = ""exempt"";
    }
}");

            VerifyDiagnostics(test, GetExemptionsFile(exemption));
        }

        [Fact]
        public void NI1004_StringLiteralInFieldInvocation_ScopeExemptFromFile_Diagnostic()
        {
            var test = new AutoTestFile(
                $@"
class Foo
{{
    public string Field;
}}

class Program
{{
    public Program()
    {{
        var foo = new Foo();
        foo.Field = <?>""{ExampleLiteral}"";
    }}
}}",
                GetNI1004LiteralRule(ExampleLiteral));

            VerifyDiagnostics(test, GetExemptionsFile(@"<Field AppliesTo=""Scope"">Foo.Field</Field>"));
        }

        [Fact]
        public void NI1004_StringLiteralInFieldInvocation_ExemptFromAttribute_NoDiagnostic()
        {
            var test = new AutoTestFile(AcceptsStringLiteralArgumentsAttribute + @"
class Foo
{
    [AcceptsStringLiteralArguments]
    public string Field;
}

class Program
{
    public Program()
    {
        var foo = new Foo();
        foo.Field = ""exempt"";
    }
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralInFieldInvocation_ScopeExemptFromAttribute_Diagnostic()
        {
            var test = new AutoTestFile(
                $@"
{ImplementationAllowedToUseStringLiteralsAttribute}

class Foo
{{
    [ImplementationAllowedToUseStringLiterals]
    public string Field;
}}

class Program
{{
    public Program()
    {{
        var foo = new Foo();
        foo.Field = <?>""{ExampleLiteral}"";
    }}
}}",
                GetNI1004LiteralRule(ExampleLiteral));

            VerifyDiagnostics(test);
        }

        #endregion // Fields

        #region Properties

        [Fact]
        public void NI1004_StringLiteralInPropertyScope_Diagnostic()
        {
            var test = new AutoTestFile(
                $@"
class Program
{{
    private string _property;
    public string Property 
    {{
        get {{ return <?>""{ExampleLiteral}1""; }}
        set {{ _property = <?>""{ExampleLiteral}2""; }}
    }}
}}",
                GetNI1004LiteralRule($"{ExampleLiteral}1"),
                GetNI1004LiteralRule($"{ExampleLiteral}2"));

            VerifyDiagnostics(test);
        }

        [Theory]
        [InlineData("Program.Property")]
        [InlineData("Program.*")]
        [InlineData("*.Property")]
        public void NI1004_StringLiteralInPropertyScope_ExemptFromFile_NoDiagnostic(string exemption)
        {
            var test = new AutoTestFile(@"
class Program
{
    private string _property;
    public string Property
    {
        get { return ""exempt1""; }
        set { _property = ""exempt2""; }
    }
}");

            VerifyDiagnostics(test, GetExemptionsFile($"<Member>{exemption}</Member>"));
        }

        [Theory]
        [MemberData(nameof(ScopeExemptAttributes))]
        public void NI1004_StringLiteralInPropertyScope_ExemptFromAttribute_NoDiagnostic(string attributeDefinition, string attribute)
        {
            var test = new AutoTestFile($@"
{attributeDefinition}

class Program
{{
    private string _property;

    {attribute}
    public string Property
    {{
        get {{ return ""exempt1""; }}
        set {{ _property = ""exempt2""; }}
    }}
}}");

            VerifyDiagnostics(test);
        }

        [Theory]
        [MemberData(nameof(ScopeExemptAttributes))]
        public void NI1004_StringLiteralInPropertyAccessorScope_ExemptFromAttribute_NoDiagnostic(string attributeDefinition, string attribute)
        {
            var test = new AutoTestFile($@"
{attributeDefinition}

class Program
{{
    private string _property;
    public string Property
    {{
        {attribute}
        get 
        {{
            return ""exempt1"";
        }}
        {attribute}
        set
        {{
            _property = ""exempt2"";
        }}
    }}
}}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralInPropertyInvocation_NotExemptFromAttribute_Diagnostic()
        {
            var test = new AutoTestFile(
                $@"
{AcceptsStringLiteralArgumentsAttribute}

class Foo
{{
    public string Name {{ get; set; }}
}}

class Program
{{
    public Program()
    {{
        var foo = new Foo {{ Name = <?>""{ExampleLiteral}"" }};
    }}
}}",
                GetNI1004LiteralRule(ExampleLiteral));

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralInPropertyInvocation_ExemptFromAttribute_NoDiagnostic()
        {
            var test = new AutoTestFile(AcceptsStringLiteralArgumentsAttribute + @"
class Foo
{
    public string Name { get; [AcceptsStringLiteralArguments] set; }
}

class Program
{
    public Program()
    {
        var foo = new Foo { Name = ""exempt"" };
    }
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralInPropertyInvocation_ExemptFromAssemblyAttribute_NoDiagnostic()
        {
            var test1 = new AutoTestFile(@"
class Foo
{
    public string Name { get; set; }
}

class Program
{
    public Program()
    {
        var foo = new Foo { Name = ""exempt"" };
    }
}");

            var test2 = new AutoTestFile($@"
[assembly: AcceptsStringLiteralArguments(Scope = ""Method"", Target = ""Foo.Name.set"")]

{AcceptsStringLiteralArgumentsAttribute}");

            VerifyDiagnostics(new[] { test1, test2 });
        }

        [Fact]
        public void NI1004_StringLiteralInPropertyInvocation_NotExemptFromAssemblyAttribute_Diagnostic()
        {
            var test1Source = $@"
class Foo
{{
    public string Name {{ get; set; }}
}}

class Program
{{
    public Program()
    {{
        var foo = new Foo {{ Name = <?>""{ExampleLiteral}"" }};
    }}
}}";
            var test1 = new AutoTestFile("ProjectA", test1Source, GetNI1004LiteralRule(ExampleLiteral));

            var test2Source = $@"
[assembly: AcceptsStringLiteralArguments(Scope = ""Method"", Target = ""Foo.Name.set"")]

{AcceptsStringLiteralArgumentsAttribute}";
            var test2 = new AutoTestFile("ProjectB", test2Source);

            VerifyDiagnostics(new[] { test1, test2 });
            VerifyDiagnostics(new[] { test2, test1 });
        }

        #endregion // Properties

        #region Indexers

        [Fact]
        public void NI1004_StringLiteralInIndexer_Diagnostic()
        {
            var test = new AutoTestFile(
                $@"
class Program
{{
    private string _name;
    public string this[string name]
    {{
        get {{ return <?>""{ExampleLiteral}1""; }}
        set {{ _name = <?>""{ExampleLiteral}2""; }}
    }}
}}",
                GetNI1004LiteralRule($"{ExampleLiteral}1"),
                GetNI1004LiteralRule($"{ExampleLiteral}2"));

            VerifyDiagnostics(test);
        }

        [Theory]
        [InlineData("Program.this[System.String]")]
        [InlineData("Program.*")]
        [InlineData("*.this[System.String]")]
        public void NI1004_StringLiteralInIndexerScope_ExemptFromFile_NoDiagnostic(string exemption)
        {
            var test = new AutoTestFile(@"
class Program
{
    private string _name;
    public string this[string name]
    {
        get { return ""exempt""; }
        set { _name = ""exempt""; }
    }
}");

            VerifyDiagnostics(test, GetExemptionsFile($"<Member>{exemption}</Member>"));
        }

        [Theory]
        [MemberData(nameof(ScopeExemptAttributes))]
        public void NI1004_StringLiteralInIndexerScope_ExemptFromAttribute_NoDiagnostic(string attributeDefinition, string attribute)
        {
            var test = new AutoTestFile($@"
{attributeDefinition}

class Program
{{
    private string _name;

    {attribute}
    public string this[string name]
    {{
        get {{ return ""exempt""; }}
        set {{ _name = name + ""exempt""; }}
    }}
}}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralInIndexerInvocation_NoDiagnostic()
        {
            var test = new AutoTestFile(@"
class Foo
{
    private string _name;

    public string this[string name]
    { 
        get { return name; }
        set { _name = name; }
    }
}

class Program
{
    public Program()
    {
        var foo = new Foo();
        string name = foo[""exempt_get""];  // get
        foo[""exempt_set""] = null;         // set
    }
}");

            VerifyDiagnostics(test);
        }

        #endregion // Indexers

        #region Methods

        [Theory]
        [InlineData("<Member>Program.*</Member>")]
        [InlineData(@"<Member AppliesTo=""Scope"">Program.*</Member>")]
        public void NI1004_StringLiteralInMethodScope_ExemptFromFile_NoDiagnostic(string exemption)
        {
            var test = new AutoTestFile(@"
class Program
{
    public Program()
    {
        var name = ""constructor"";
    }

    public void Method1()
    {
        var name = ""instance"";
    }

    public static void Method2()
    {
        var name = ""static"";
    }
}");

            VerifyDiagnostics(test, GetExemptionsFile(exemption));
        }

        [Fact]
        public void NI1004_StringLiteralInMethodScope_InvocationExemptFromFile_Diagnostic()
        {
            var test = new AutoTestFile(
                $@"
class Program
{{
    public void Method()
    {{
        var name = <?>""{ExampleLiteral}"";
    }}
}}",
                GetNI1004LiteralRule(ExampleLiteral));

            VerifyDiagnostics(test, GetExemptionsFile(@"<Member AppliesTo=""Invocation"">Program.Method</Member>"));
        }

        [Theory]
        [MemberData(nameof(ScopeExemptAttributes))]
        public void NI1004_StringLiteralInMethodScope_ExemptFromAttribute_NoDiagnostic(string attributeDefinition, string attribute)
        {
            var test = new AutoTestFile($@"
{attributeDefinition}

class Program
{{
    {attribute}
    public Program()
    {{
        System.Console.WriteLine(""constructor"");
    }}

    {attribute}
    public void Method1()
    {{
        System.Console.WriteLine(""instance"");
    }}

    {attribute}
    public static void Method2()
    {{
        System.Console.WriteLine(""static"");
    }}
}}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralInMethodScope_InvocationExemptFromAttribute_Diagnostic()
        {
            var test = new AutoTestFile(
                $@"
{AcceptsStringLiteralArgumentsAttribute}

class Program
{{
    [AcceptsStringLiteralArguments]
    public void Method()
    {{
        System.Console.WriteLine(<?>""{ExampleLiteral}"");
    }}
}}",
                GetNI1004LiteralRule(ExampleLiteral));

            VerifyDiagnostics(test);
        }

        [Theory]
        [MemberData(nameof(ScopeExemptAttributes))]
        public void NI1004_StringLiteralInMethodScope_ExemptFromAttribute_MultipleFiles_NoDiagnostic(string attributeDefinition, string attribute)
        {
            var test1 = new AutoTestFile($@"
{attributeDefinition}

partial class Program
{{
    {attribute}
    partial void Method1();
}}");

            var test2 = new AutoTestFile(@"
partial class Program
{
    partial void Method1()
    {
        System.Console.WriteLine(""exempt"");
    }
}");

            VerifyDiagnostics(new[] { test1, test2 });
        }

        [Theory]
        [InlineData(ExampleStaticMethodTestTemplate)]
        [InlineData(ExampleExtensionMethodTestTemplate)]
        [InlineData(ExampleInstanceMethodTestTemplate)]
        [InlineData(ExampleConstructorMethodTestTemplate)]
        [InlineData(ExampleGenericMethodTestTemplate)]
        public void NI1004_StringLiteralInMethodInvocation_Diagnostic(string testTemplate)
        {
            var test = new AutoTestFile(
                string.Format(CultureInfo.InvariantCulture, testTemplate, ExampleLiteral, "<?>"),
                GetNI1004LiteralRule(ExampleLiteral));

            VerifyDiagnostics(test);
        }

        [Theory]
        [InlineData(ExampleStaticMethodTestTemplate, "<Member>System.Text.Encoding.GetEncoding(System.String)</Member>")]
        [InlineData(ExampleStaticMethodTestTemplate, @"<Member AppliesTo=""Invocation"">System.Text.Encoding.GetEncoding(System.String)</Member>")]
        [InlineData(ExampleExtensionMethodTestTemplate, "<Member>Example.Extensions.NotLocalized()</Member>")]
        [InlineData(ExampleExtensionMethodTestTemplate, @"<Member AppliesTo=""Invocation"">Example.Extensions.NotLocalized()</Member>")]
        [InlineData(ExampleInstanceMethodTestTemplate, "<Member>Numbers.ComplexDouble.ToString(System.String)</Member>")]
        [InlineData(ExampleInstanceMethodTestTemplate, @"<Member AppliesTo=""Invocation"">Numbers.ComplexDouble.ToString(System.String)</Member>")]
        [InlineData(ExampleConstructorMethodTestTemplate, "<Member>System.IO.StringReader.StringReader(System.String)</Member>")]
        [InlineData(ExampleConstructorMethodTestTemplate, @"<Member AppliesTo=""Invocation"">System.IO.StringReader.StringReader(System.String)</Member>")]
        [InlineData(ExampleGenericMethodTestTemplate, "<Member>Program.Method&lt;System.String&gt;(System.String)</Member>")]
        [InlineData(ExampleGenericMethodTestTemplate, @"<Member AppliesTo=""Invocation"">Program.Method&lt;System.String&gt;(System.String)</Member>")]
        public void NI1004_StringLiteralInMethodInvocation_ExemptFromFile_NoDiagnostic(string testTemplate, string exemption)
        {
            var test = new AutoTestFile(string.Format(CultureInfo.InvariantCulture, testTemplate, ExampleLiteral, string.Empty));

            VerifyDiagnostics(test, GetExemptionsFile(exemption));
        }

        [Fact]
        public void NI1004_StringLiteralInMethodInvocation_ScopeExemptFromFile_Diagnostic()
        {
            var test = new AutoTestFile(
                string.Format(CultureInfo.InvariantCulture, ExampleStaticMethodTestTemplate, ExampleLiteral, "<?>"),
                GetNI1004LiteralRule(ExampleLiteral));

            VerifyDiagnostics(test, GetExemptionsFile(@"<Member AppliesTo=""Scope"">System.Text.Encoding.GetEncoding(System.String)</Member>"));
        }

        [Theory]
        [InlineData("Program*")]
        [InlineData("*&lt;System.String&gt;*")]
        [InlineData("*(System.String)")]
        public void NI1004_StringLiteralInMethodInvocation_ExemptFromFile_Wildcards_NoDiagnostic(string exemption)
        {
            // Any method "type" could be used
            var test = new AutoTestFile(string.Format(CultureInfo.InvariantCulture, ExampleGenericMethodTestTemplate, ExampleLiteral, string.Empty));

            VerifyDiagnostics(test, GetExemptionsFile($"<Member>{exemption}</Member>"));
        }

        [Theory]
        [InlineData(null, "element")]
        [InlineData(TestAssemblyName, null)]
        [InlineData(TestAssemblyName, "element")]
        public void NI1004_StringLiteralInMethodInvocation_ExemptFromFile_AttributesMatch_NoDiagnostic(string assemblyName, string parameterName)
        {
            // Any method "type" could be used
            var test = new AutoTestFile(string.Format(CultureInfo.InvariantCulture, ExampleGenericMethodTestTemplate, ExampleLiteral, string.Empty));

            var attributes = GetNonNullAttributesString(new Dictionary<string, string>
            {
                ["Assembly"] = assemblyName,
                ["Parameter"] = parameterName,
            });

            VerifyDiagnostics(test, GetExemptionsFile($"<Member {attributes}>{ExampleGenericMethodName}</Member>"));
        }

        [Theory]
        [InlineData(null, "wrong")]
        [InlineData("Different", null)]
        [InlineData("Different", "wrong")]
        public void NI1004_StringLiteralInMethodInvocation_ExemptFromFile_AttributesMismatch_Diagnostic(string assemblyName, string parameterName)
        {
            // Any method "type" could be used
            var test = new AutoTestFile(
                string.Format(CultureInfo.InvariantCulture, ExampleGenericMethodTestTemplate, ExampleLiteral, "<?>"),
                GetNI1004LiteralRule(ExampleLiteral));

            var attributes = GetNonNullAttributesString(new Dictionary<string, string>
            {
                ["Assembly"] = assemblyName,
                ["Parameter"] = parameterName,
            });

            VerifyDiagnostics(test, GetExemptionsFile($"<Member {attributes}>{ExampleGenericMethodName}</Member>"));
        }

        [Fact]
        public void NI1004_StringLiteralInMethodInvocation_ExemptFromFile_ParametersMatch_NoDiagnostic()
        {
            var test = new AutoTestFile(@"
namespace My.Namespace
{
    public static class Extensions
    {
        public static string Localized(this string text, bool unused, string cultureName)
        {
            return text + cultureName;
        }
    }

    class Program
    {
        string _foo = string.Empty.Localized(false, ""en-us"");
    }
}");

            VerifyDiagnostics(
                test,
                GetExemptionsFile(@"<Member Parameter=""cultureName"">My.Namespace.Extensions.Localized(System.Boolean, System.String)</Member>"));
        }

        [Fact]
        public void NI1004_StringLiteralInMethodInvocation_ExemptFromFile_ParametersMatch_LessArgumentsThanParameters_NoDiagnostic()
        {
            var test = new AutoTestFile(@"
using System.Globalization;

class Program
{
    private static string _lastName = null;
    private static string _fullName = string.Format(CultureInfo.CurrentCulture, _lastName + ""foo"");
}");

            VerifyDiagnostics(
                test,
                GetExemptionsFile(@"<Member Parameter=""format"">System.String.Format(System.IFormatProvider, System.String, System.Object[])</Member>"));
        }

        [Fact]
        public void NI1004_StringLiteralInMethodInvocation_ExemptFromFile_ParametersMatch_MoreArgumentsThanParameters_NoDiagnostic()
        {
            var test = new AutoTestFile(@"
class Program
{
    public void Method(int num, params string[] args)
    {
    }

    public Program()
    {
        Method(1, null, null, ""exempt"");
    }
}");

            VerifyDiagnostics(
                test,
                GetExemptionsFile(@"<Member Parameter=""args"">Program.Method(System.Int32, System.String[])</Member>"));
        }

        [Theory]
        [InlineData(ExampleClassTestTemplate, "<Type>My.Namespace.ExemptClass</Type>")]
        [InlineData(ExampleClassTestTemplate, @"<Type AppliesTo=""Invocation"">My.Namespace.ExemptClass</Type>")]
        [InlineData(ExampleStructTestTemplate, "<Type>My.Namespace.ExemptStruct</Type>")]
        [InlineData(ExampleStructTestTemplate, @"<Type AppliesTo=""Invocation"">My.Namespace.ExemptStruct</Type>")]
        [InlineData(ExampleAbstractClassTestTemplate, "<Type>My.Namespace.ExemptClassBase</Type>")]
        [InlineData(ExampleAbstractClassTestTemplate, @"<Type AppliesTo=""Invocation"">My.Namespace.ExemptClassBase</Type>")]
        public void NI1004_StringLiteralInMethodInvocation_TypeExemptFromFile_NoDiagnostic(string testTemplate, string exemption)
        {
            var test = new AutoTestFile(string.Format(CultureInfo.InvariantCulture, testTemplate, ExampleLiteral, string.Empty));

            VerifyDiagnostics(test, GetExemptionsFile(exemption));
        }

        [Fact]
        public void NI1004_StringLiteralInMethodInvocation_TypeScopeExemptFromFile_Diagnostic()
        {
            var test = new AutoTestFile(
                string.Format(CultureInfo.InvariantCulture, ExampleClassTestTemplate, ExampleLiteral, "<?>"),
                GetNI1004LiteralRule(ExampleLiteral));

            VerifyDiagnostics(test, GetExemptionsFile(@"<Type AppliesTo=""Scope"">My.Namespace.ExemptClass</Type>"));
        }

        [Theory]
        [InlineData(ExampleClassTestTemplate, "My.Namespace.ExemptClass", null, "name")]
        [InlineData(ExampleStructTestTemplate, "My.Namespace.ExemptStruct", TestAssemblyName, null)]
        [InlineData(ExampleAbstractClassTestTemplate, "My.Namespace.ExemptClassBase", TestAssemblyName, "name")]
        public void NI1004_StringLiteralInMethodInvocation_TypeExemptFromFile_AttributesMatch_NoDiagnostic(string testTemplate, string exemption, string assemblyName, string parameterName)
        {
            var test = new AutoTestFile(string.Format(CultureInfo.InvariantCulture, testTemplate, ExampleLiteral, string.Empty));

            var attributes = GetNonNullAttributesString(new Dictionary<string, string>
            {
                ["Assembly"] = assemblyName,
                ["Parameter"] = parameterName,
            });

            VerifyDiagnostics(test, GetExemptionsFile($"<Type {attributes}>{exemption}</Type>"));
        }

        [Theory]
        [InlineData(ExampleClassTestTemplate, "My.Namespace.ExemptClass", null, "wrong")]
        [InlineData(ExampleStructTestTemplate, "My.Namespace.ExemptStruct", "Different", null)]
        [InlineData(ExampleAbstractClassTestTemplate, "My.Namespace.ExemptClassBase", "Different", "wrong")]
        public void NI1004_StringLiteralInMethodInvocation_TypeExemptFromFile_AttributesMismatch_Diagnostic(string testTemplate, string exemption, string assemblyName, string parameterName)
        {
            var test = new AutoTestFile(
                string.Format(CultureInfo.InvariantCulture, testTemplate, ExampleLiteral, "<?>"),
                GetNI1004LiteralRule(ExampleLiteral));

            var attributes = GetNonNullAttributesString(new Dictionary<string, string>
            {
                ["Assembly"] = assemblyName,
                ["Parameter"] = parameterName,
            });

            VerifyDiagnostics(test, GetExemptionsFile($"<Type {attributes}>{exemption}</Type>"));
        }

        [Fact]
        public void NI1004_StringLiteralInMethodInvocation_BaseTypeExemptFromFile_NoDiagnostic()
        {
            var test = new AutoTestFile($@"
using System;

class Program
{{
    public Program()
    {{
        throw new ArgumentOutOfRangeException(""{ExampleLiteral}"");
    }}
}}");

            VerifyDiagnostics(test, GetExemptionsFile("<Type>System.ArgumentException</Type>"));
        }

        [Theory]
        [InlineData(null, "paramName")]
        [InlineData(TestAssemblyName, null)]
        [InlineData(TestAssemblyName, "paramName")]
        public void NI1004_StringLiteralInMethodInvocation_BaseTypeExemptFromFile_AttributesMatch_NoDiagnostic(string assemblyName, string parameterName)
        {
            var test = new AutoTestFile($@"
using System;

class Program
{{
    public Program()
    {{
        throw new ArgumentOutOfRangeException(""{ExampleLiteral}"");
    }}
}}");

            var attributes = GetNonNullAttributesString(new Dictionary<string, string>
            {
                ["Assembly"] = assemblyName,
                ["Parameter"] = parameterName,
            });

            VerifyDiagnostics(test, GetExemptionsFile($"<Type {attributes}>System.ArgumentException</Type>"));
        }

        [Theory]
        [InlineData(null, "wrong")]
        [InlineData("Different", null)]
        [InlineData("Different", "wrong")]
        public void NI1004_StringLiteralInMethodInvocation_BaseTypeExemptFromFile_AttributesMismatch_Diagnostic(string assemblyName, string parameterName)
        {
            var test = new AutoTestFile(
                $@"
using System;

class Program
{{
    public Program()
    {{
        throw new ArgumentOutOfRangeException(<?>""{ExampleLiteral}"");
    }}
}}",
                GetNI1004LiteralRule(ExampleLiteral));

            var attributes = GetNonNullAttributesString(new Dictionary<string, string>
            {
                ["Assembly"] = assemblyName,
                ["Parameter"] = parameterName,
            });

            VerifyDiagnostics(test, GetExemptionsFile($@"<Type {attributes}>System.ArgumentException</Type>"));
        }

        [Theory]
        [InlineData(@"Foo foo = new Foo(""exempt"");")]
        [InlineData(@"
var foo = new Foo(); 
foo.Method(""exempt"");")]
        [InlineData(@"Foo.StaticMethod(""exempt"");")]
        public void NI1004_StringLiteralInMethodInvocation_ExemptFromAttribute_NoDiagnostic(string methodCall)
        {
            var test = new AutoTestFile($@"
{AcceptsStringLiteralArgumentsAttribute}

class Foo
{{
    public Foo()
    {{
    }}

    [AcceptsStringLiteralArguments]
    public Foo(string name)
    {{
    }}

    [AcceptsStringLiteralArguments]
    public void Method(string name)
    {{
    }}

    [AcceptsStringLiteralArguments]
    public static void StaticMethod(string name)
    {{
    }}
}}

class Bar
{{
    public Bar()
    {{
        {methodCall}
    }}
}}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralInMethodInvocation_ScopeExemptFromAttribute_Diagnostic()
        {
            var test = new AutoTestFile(
                $@"
class Foo
{{
    [ImplementationAllowedToUseStringLiterals]
    public void Method(string name)
    {{
        // only literals in this scope should be exempt
    }}
}}

class Bar
{{
    public Bar()
    {{
        var foo = new Foo();
        foo.Method(<?>""{ExampleLiteral}"");
    }}
}}",
                GetNI1004LiteralRule(ExampleLiteral));

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralInMethodInvocation_ExemptFromAttribute_InvalidScope_NoDiagnostic()
        {
            // Invalid scopes should be the same as default scopes
            var test = new AutoTestFile(AcceptsStringLiteralArgumentsAttribute + @"
class Foo
{
    [AcceptsStringLiteralArguments(Scope = ""Invalid"")]
    public Foo(string name)
    {
    }
}

class Bar
{
    private Foo _foo = new Foo(""exempt"");
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralsInMethodInvocation_ExemptFromAttribute_ParameterScope_NoDiagnosticForParameter()
        {
            var test = new AutoTestFile(
                $@"
{AcceptsStringLiteralArgumentsAttribute}

class Foo
{{
    [AcceptsStringLiteralArguments(Scope = ""Parameter"", Target = ""lastName"")]
    public Foo(string firstName, string lastName)
    {{
    }}
}}

class Bar
{{
    private Foo _foo = new Foo(<?>""{ExampleLiteral}"", ""exempt"");
}}",
                GetNI1004LiteralRule(ExampleLiteral));

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralInMethodInvocation_ExemptFromAttribute_MultipleFiles_NoDiagnosticForParameter()
        {
            var test1 = new AutoTestFile(AcceptsStringLiteralArgumentsAttribute + @"
class Foo
{
    [AcceptsStringLiteralArguments(Scope = ""Parameter"", Target = ""lastName"")]
    public Foo(string firstName, string lastName)
    {
    }
}");

            var test2 = new AutoTestFile(@"
class Bar
{
    private Foo _foo = new Foo(null, ""exempt"");
}");

            VerifyDiagnostics(new[] { test1, test2 });
            VerifyDiagnostics(new[] { test2, test1 });
        }

        [Fact]
        public void NI1004_StringLiteralsInMethodInvocation_ExemptFromAttribute_NoScope_NoDiagnosticForParameter()
        {
            var test = new AutoTestFile(
                $@"
{AcceptsStringLiteralArgumentsAttribute}

class Foo
{{
    [AcceptsStringLiteralArguments(Target = ""|lastName"")]
    public Foo(string firstName, string lastName)
    {{
    }}
}}

class Bar
{{
    private Foo _foo = new Foo(<?>""{ExampleLiteral}"", ""exempt"");
}}",
                GetNI1004LiteralRule(ExampleLiteral));

            VerifyDiagnostics(test);
        }

        [Theory]
        [InlineData("first|second")]
        [InlineData("|first|second")]
        [InlineData("first|second|")]
        [InlineData("|first|second|")]
        public void NI1004_StringLiteralsInMethodInvocation_ExemptFromAttribute_MultipleParametersMatch_NoDiagnosticForParameters(string target)
        {
            var test = new AutoTestFile(
                $@"
{AcceptsStringLiteralArgumentsAttribute}

class Foo
{{
    [AcceptsStringLiteralArguments(Scope = ""Parameter"", Target = ""{target}"")]
    public Foo(string first, string second, string third)
    {{
    }}
}}

class Bar
{{
    private Foo _foo = new Foo(""exempt1"", ""exempt2"", <?>""{ExampleLiteral}"");
}}",
                GetNI1004LiteralRule(ExampleLiteral));

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralInMethodInvocation_ExemptFromAttributeOnAbstractMethod_NoDiagnostic()
        {
            var test = new AutoTestFile(AcceptsStringLiteralArgumentsAttribute + @"
abstract class Base
{
    [AcceptsStringLiteralArguments]
    public abstract void Method(string firstName, string lastName);
}

class Foo : Base
{
    public override void Method(string firstName, string lastName)
    {
    }
}

class Program
{
    public Program()
    {
        var foo = new Foo();
        foo.Method(""exempt1"", ""exempt2"");
    }
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralInMethodInvocation_ExemptFromAttributeOnInterfaceMethod_NoDiagnostic()
        {
            var test = new AutoTestFile(AcceptsStringLiteralArgumentsAttribute + @"
interface IFoo
{
    [AcceptsStringLiteralArguments]
    void Method(string firstName, string lastName);
}

class Foo : IFoo
{
    public void Method(string firstName, string lastName)
    {
    }
}

class Program
{
    public Program()
    {
        var foo = new Foo();
        foo.Method(""exempt1"", ""exempt2"");
    }
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralInMethodInvocation_ExemptFromAttributeOnType_NoDiagnostic()
        {
            var test = new AutoTestFile(AcceptsStringLiteralArgumentsAttribute + @"
[AcceptsStringLiteralArguments]
static class Foo
{
    public static void Method(string name)
    {
    }
}

class Bar
{
    public Bar()
    {
        Foo.Method(""exempt"");
    }
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralInMethodInvocation_ExemptFromAttributeOnParentType_NoDiagnostic()
        {
            var test = new AutoTestFile(AcceptsStringLiteralArgumentsAttribute + @"
[AcceptsStringLiteralArguments]
class Base
{
}

class Foo : Base
{
    public Foo(string name)
    {
    }
}

class Bar
{
    private Foo _foo = new Foo(""exempt"");
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralInMethodInvocation_ExemptFromAttributeOnInterface_NoDiagnostic()
        {
            var test = new AutoTestFile(AcceptsStringLiteralArgumentsAttribute + @"
[AcceptsStringLiteralArguments]
interface IFoo
{
    void Method(string name);
}

class Foo : IFoo
{
    public void Method(string name)
    {
    }
}

class Program
{
    public Program()
    {
        var foo = new Foo();
        foo.Method(""exempt"");
    }
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralInMethodInvocation_ExemptFromAttributeOnParentInterface_NoDiagnostic()
        {
            var test = new AutoTestFile(AcceptsStringLiteralArgumentsAttribute + @"
[AcceptsStringLiteralArguments]
interface IGrandParent
{
    void Method(string name);
}

interface IParent : IGrandParent
{
}

class Foo : IParent
{
    public void Method(string name)
    {
    }
}

class Program
{
    public Program()
    {
        var foo = new Foo();
        foo.Method(""exempt"");
    }
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralInMethodInvocation_ExemptFromAssemblyAttribute_NoDiagnostic()
        {
            var test1 = new AutoTestFile(@"
class Foo
{
    public void Method(string name)
    {
    }
}

class Program
{
    public Program()
    {
        var foo = new Foo();
        foo.Method(""exempt"");
    }
}");

            var test2 = new AutoTestFile($@"
[assembly: AcceptsStringLiteralArguments(Scope = ""Method"", Target = ""Foo.Method(System.String)"")]

{AcceptsStringLiteralArgumentsAttribute}");

            VerifyDiagnostics(new[] { test1, test2 });
        }

        [Fact]
        public void NI1004_StringLiteralInMethodInvocation_NotExemptFromAssemblyAttribute_Diagnostic()
        {
            var test1Source = $@"
class Foo
{{
    public void Method(string name)
    {{
    }}
}}

class Program
{{
    public Program()
    {{
        var foo = new Foo();
        foo.Method(<?>""{ExampleLiteral}"");
    }}
}}";
            var test1 = new AutoTestFile("ProjectA", test1Source, GetNI1004LiteralRule(ExampleLiteral));

            var test2Source = $@"
[assembly: AcceptsStringLiteralArguments(Scope = ""Method"", Target = ""Foo.Method(System.String)"")]

{AcceptsStringLiteralArgumentsAttribute}";
            var test2 = new AutoTestFile("ProjectB", test2Source);

            VerifyDiagnostics(new[] { test1, test2 });
            VerifyDiagnostics(new[] { test2, test1 });
        }

        [Theory]
        [InlineData("Class", "Foo")]
        [InlineData("BaseClass", "FooBase")]
        public void NI1004_StringLiteralInMethodInvocation_ExemptFromAssemblyAttributeForType_NoDiagnostic(string scope, string target)
        {
            var test1 = new AutoTestFile(@"
abstract class FooBase
{
    public abstract void Method(string name);
}

class Foo : FooBase
{
    public override void Method(string name)
    {
    }
}

class Program
{
    public Program()
    {
        var foo = new Foo();
        foo.Method(""exempt"");
    }
}");

            var test2 = new AutoTestFile($@"
[assembly: AcceptsStringLiteralArguments(Scope = ""{scope}"", Target = ""{target}"")]

{AcceptsStringLiteralArgumentsAttribute}");

            VerifyDiagnostics(new[] { test1, test2 });
        }

        [Theory]
        [InlineData("Class", "Foo")]
        [InlineData("BaseClass", "IFoo")]
        public void NI1004_StringLiteralInMethodInvocation_NotExemptFromAssemblyAttributeForType_Diagnostic(string scope, string target)
        {
            var test1Source = $@"
interface IFoo
{{
    void Method(string name);
}}

class Foo : IFoo
{{
    public void Method(string name)
    {{
    }}
}}

class Program
{{
    public Program()
    {{
        var foo = new Foo();
        foo.Method(<?>""{ExampleLiteral}"");
    }}
}}";
            var test1 = new AutoTestFile("ProjectA", test1Source, GetNI1004LiteralRule(ExampleLiteral));

            var test2Source = $@"
[assembly: AcceptsStringLiteralArguments(Scope = ""{scope}"", Target = ""{target}"")]

{AcceptsStringLiteralArgumentsAttribute}";
            var test2 = new AutoTestFile("ProjectB", test2Source);

            VerifyDiagnostics(new[] { test1, test2 });
        }

        [Theory]
        [InlineData("System.String.operator ==(System.String, System.String)", @"""exempt1"" == ""exempt2""")]
        [InlineData("System.String.operator ==*|left", @"""exempt"" == value1")]
        [InlineData("System.String.operator ==*|right", @"value1 == ""exempt""")]
        [InlineData("System.String.operator ==*|left|right", @"""exempt1"" == ""exempt2""")]
        public void NI1004_StringLiteralInMethodInvocation_ExemptFromAssemblyAttribute_Method_NoDiagnostic(string target, string methodCall)
        {
            var test = new AutoTestFile($@"
[assembly: AllowExternalCodeToAcceptStringLiteralArguments(Scope = ""Method"", Target = ""{target}"")]

{AllowExternalCodeToAcceptStringLiteralArgumentsAttribute}

class Program
{{
    public bool IsEqual(string value1)
    {{
        return {methodCall};
    }}
}}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralInMethodInvocation_ExemptFromAssemblyAttribute_Class_NoDiagnostic()
        {
            var test = new AutoTestFile($@"
[assembly: AllowExternalCodeToAcceptStringLiteralArguments(Scope = ""Class"", Target = ""System.String"")]

{AllowExternalCodeToAcceptStringLiteralArgumentsAttribute}

class Program
{{
    public bool StartsWithA(string word)
    {{
        return word.StartsWith(""a"");
    }}
}}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralInMethodInvocation_ExemptFromAssemblyAttribute_BaseClass_NoDiagnostic()
        {
            var test = new AutoTestFile($@"
[assembly: AllowExternalCodeToAcceptStringLiteralArguments(Scope = ""BaseClass"", Target = ""System.Exception"")]

{AllowExternalCodeToAcceptStringLiteralArgumentsAttribute}

class Program
{{
    public void Method(string name)
    {{
        throw new System.ArgumentNullException(""name"");
    }}
}}");

            VerifyDiagnostics(test);
        }

        #endregion  // Methods

        #region Types

        [Theory]
        [InlineData("<Type>Program</Type>")]
        [InlineData(@"<Type AppliesTo=""Scope"">Program</Type>")]
        public void NI1004_StringLiteralInTypeScope_ExemptFromFile_NoDiagnostic(string exemption)
        {
            var test = new AutoTestFile(@"
class Program
{
    private string _name = ""exempt"";
}");

            VerifyDiagnostics(test, GetExemptionsFile(exemption));
        }

        [Fact]
        public void NI1004_StringLiteralInTypeScope_InvocationExemptFromFile_Diagnostic()
        {
            var test = new AutoTestFile(
                $@"
class Program
{{
    private string _name = <?>""{ExampleLiteral}"";
}}",
                GetNI1004LiteralRule(ExampleLiteral));

            VerifyDiagnostics(test, GetExemptionsFile(@"<Type AppliesTo=""Invocation"">Program</Type>"));
        }

        [Theory]
        [InlineData(ExemptFromStringLiteralsRuleAttribute, nameof(ExemptFromStringLiteralsRuleAttribute))]
        [InlineData(ImplementationAllowedToUseStringLiteralsAttribute, nameof(ImplementationAllowedToUseStringLiteralsAttribute))]
        public void NI1004_StringLiteralInTypeScope_ExemptFromAttribute_NoDiagnostic(string attributeDefinition, string attributeName)
        {
            var test = new AutoTestFile($@"
[assembly: {attributeName}(Scope = ""Class"", Target = ""Program"")]

{attributeDefinition}

class Foo   // just prevent attribute from being associated with the exempt class
{{
}}

class Program
{{
    public void Method()
    {{
        System.Console.WriteLine(""exempt"");
    }}
}}");

            VerifyDiagnostics(test);
        }

        [Theory]
        [MemberData(nameof(ScopeExemptAttributes))]
        public void NI1004_StringLiteralsInTypeScope_ExemptFromAttribute_NoDiagnostics(string attributeDefinition, string attribute)
        {
            var test = new AutoTestFile($@"
{attributeDefinition}

{attribute}
class Program
{{
    private const string Name = ""exempt"";

    public void Method()
    {{
        System.Console.WriteLine(""exempt too!"");
    }}

    private class NestedFoo
    {{
        private string _nestedName = ""nested-exempt"";
    }}
}}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralsInTypeScope_InvocationExemptFromAttribute_Diagnostic()
        {
            var test = new AutoTestFile(
                $@"
{AcceptsStringLiteralArgumentsAttribute}

[AcceptsStringLiteralArguments]
class Program
{{
    public Program()
    {{
        System.Console.WriteLine(<?>""{ExampleLiteral}"");
    }}
}}",
                GetNI1004LiteralRule(ExampleLiteral));

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralInTypeScope_InvocationExemptFromAssemblyAttribute_Diagnostic()
        {
            var test = new AutoTestFile(
                $@"
[assembly: AcceptsStringLiteralArguments(Scope = ""Class"", Target = ""Program"")]

{AcceptsStringLiteralArgumentsAttribute}

class Foo   // just prevent attribute from being associated with the exempt class
{{
}}

class Program
{{
    public void Method()
    {{
        System.Console.WriteLine(<?>""{ExampleLiteral}"");
    }}
}}",
                GetNI1004LiteralRule(ExampleLiteral));

            VerifyDiagnostics(test);
        }

        [Theory]
        [MemberData(nameof(ScopeExemptAttributes))]
        public void NI1004_StringLiteralsInTypeScope_ExemptFromAttribute_MultipleFiles_NoDiagnostics(string attributeDefinition, string attribute)
        {
            var test1 = new AutoTestFile($@"
{attributeDefinition}

{attribute}
partial class Program
{{
    private string _field1 = ""exempt1"";
}}");

            var test2 = new AutoTestFile(@"
partial class Program
{
    private string _field2 = ""exempt2"";
}");

            VerifyDiagnostics(new[] { test1, test2 });
        }

        #endregion  // Types

        #region Namespaces

        // Already tested that string literals in namespaces yield diagnostics

        [Theory]
        [InlineData("Some.Example")]
        [InlineData("Some.*")]
        [InlineData("*.Example")]
        public void NI1004_StringLiteralInNamespace_ExemptFromFile_NoDiagnostic(string exemption)
        {
            var test = new AutoTestFile(@"
namespace Some.Example
{
    class Program
    {
        private string _field = ""exempt"";
    }
}");

            VerifyDiagnostics(test, GetExemptionsFile($"<Namespace>{exemption}</Namespace>"));
        }

        [Fact]
        public void NI1004_StringLiteralInNamespace_ExemptFromFile_AttributesMatch_NoDiagnostic()
        {
            var test = new AutoTestFile(@"
namespace Some.Example
{
    class Program
    {
        private string _field = ""exempt"";
    }
}");

            VerifyDiagnostics(
                test,
                GetExemptionsFile($@"<Namespace Assembly=""{TestAssemblyName}"">Some.Example</Namespace>"));
        }

        [Fact]
        public void NI1004_StringLiteralInNamespace_ExemptFromFile_AttributesMismatch_Diagnostic()
        {
            var test = new AutoTestFile(
                $@"
namespace Some.Example
{{
    class Program
    {{
        private string _field = <?>""{ExampleLiteral}"";
    }}
}}",
                GetNI1004LiteralRule(ExampleLiteral));

            VerifyDiagnostics(test, GetExemptionsFile($@"<Namespace Assembly=""Different"">Some.Example</Namespace>"));
        }

        [Theory]
        [InlineData(ExemptFromStringLiteralsRuleAttribute, nameof(ExemptFromStringLiteralsRuleAttribute), "Some")]
        [InlineData(ExemptFromStringLiteralsRuleAttribute, nameof(ExemptFromStringLiteralsRuleAttribute), "Some.Example")]
        [InlineData(ImplementationAllowedToUseStringLiteralsAttribute, nameof(ImplementationAllowedToUseStringLiteralsAttribute), "Some")]
        [InlineData(ImplementationAllowedToUseStringLiteralsAttribute, nameof(ImplementationAllowedToUseStringLiteralsAttribute), "Some.Example")]
        public void NI1004_StringLiteralInNamespace_ExemptFromAssemblyAttribute_NoDiagnostic(string attributeDefinition, string attributeName, string target)
        {
            var test = new AutoTestFile($@"
[assembly: {attributeName}(Scope = ""Namespace"", Target = ""{target}"")]

{attributeDefinition}

namespace Some.Example
{{
    class Program
    {{
        private string _field = ""exempt"";
    }}
}}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1004_StringLiteralInNamespace_NotExemptFromAssemblyAttribute_Diagnostic()
        {
            var test = new AutoTestFile(
                $@"
[assembly: AcceptsStringLiteralArguments(Scope = ""Namespace"", Target = ""Some.Example"")]

{AcceptsStringLiteralArgumentsAttribute}

namespace Some.Example
{{
    class Program
    {{
        private string _field = <?>""{ExampleLiteral}"";
    }}
}}",
                GetNI1004LiteralRule(ExampleLiteral));

            VerifyDiagnostics(test);
        }

        #endregion // Namespaces

        #region Files

        [Theory]
        [InlineData("*2.cs")]
        [InlineData("Test2.cs")]
        public void NI1004_StringLiteralInFile_ExemptFromFile_NoDiagnostic(string exemption)
        {
            var testSource = @"
class Program
{
    public string _field = ""exempt"";
}";
            var test = new AutoTestFile("Test2.cs", DiagnosticVerifier.DefaultProjectName, testSource);

            VerifyDiagnostics(test, GetExemptionsFile($"<Filename>{exemption}</Filename>"));
        }

        [Theory]
        [InlineData(ExemptFromStringLiteralsRuleAttribute, nameof(ExemptFromStringLiteralsRuleAttribute))]
        [InlineData(ImplementationAllowedToUseStringLiteralsAttribute, nameof(ImplementationAllowedToUseStringLiteralsAttribute))]
        public void NI1004_StringLiteralInFile_ExemptFromAttribute_NoDiagnostic(string attributeDefinition, string attributeName)
        {
            var test = new AutoTestFile($@"
[assembly: {attributeName}(Scope = ""File"", Target = ""{DefaultFileName}"")]

{attributeDefinition}

class Foo   // just prevent attribute from being associated with the exempt class
{{
}}

class Program
{{
    public void Method()
    {{
        System.Console.WriteLine(""exempt"");
    }}
}}");

            VerifyDiagnostics(test);
        }

        #endregion  // Files

        #region Assemblies

        [Fact]
        public void NI1004_StringLiteralsInAssembly_ExemptFromFile_NoDiagnostics()
        {
            var test1 = new AutoTestFile(@"
namespace Test1
{
    public class Foo
    {
        private string _name = ""exempt1"";

        public Foo(string name)
        {
            System.Console.WriteLine(""exempt2"");
        }
    }
}");

            var test2 = new AutoTestFile(@"
namespace Test2
{
    class Bar
    {
        public Bar()
        {
            var foo = new Test1.Foo(""exempt3"");
        }
    }
}");

            VerifyDiagnostics(
                new[] { test1, test2 },
                new[] { GetExemptionsFile($"<Assembly>{TestAssemblyName}</Assembly>") });
        }

        [Fact]
        public void NI1004_StringLiteralsInAssembly_ExemptFromAssemblyAttribute_NoDiagnostics()
        {
            var attributeDefinition = new AutoTestFile(ExemptFromStringLiteralsRuleAttribute);
            var assemblyInfo = new AutoTestFile(@"[assembly: ExemptFromStringLiteralsRule(Scope = ""Disabled"")]");

            var test1 = new AutoTestFile(@"
namespace Test1
{
    public class Foo
    {
        private string _name = ""exempt1"";

        public Foo(string name)
        {
            System.Console.WriteLine(""exempt2"");
        }
    }
}");

            var test2 = new AutoTestFile(@"
namespace Test2
{
    class Bar
    {
        public Bar()
        {
            var foo = new Test1.Foo(""exempt3"");
        }
    }
}");

            VerifyDiagnostics(new[] { attributeDefinition, assemblyInfo, test1, test2 });
        }

        #endregion // Assemblies
#pragma warning restore SA1124 // Do not use regions

        [Fact]
        public void NI1004_DuplicateExemptionsInOneFile_NoDiagnostic() // an exception would create a diagnostic
        {
            VerifyDiagnostics(
                new AutoTestFile(string.Empty),
                GetExemptionsFile(string.Concat(AllExemptionTypesXml, AllExemptionTypesXml)));
        }

        [Fact]
        public void NI1004_DuplicateExemptionsAcrossMultipleFiles_NoDiagnostic()
        {
            var additionalFiles = new[]
            {
                GetExemptionsFile(new FileInfo("LiteralExemptions1.txt"), AllExemptionTypesXml),
                GetExemptionsFile(new FileInfo("LiteralExemptions2.txt"), AllExemptionTypesXml),
            };
            VerifyDiagnostics(new AutoTestFile(string.Empty), additionalFiles);
        }

        [Fact]
        public void NI1004_StringLiteralsExemptFromMultipleFiles_NoDiagnostics()
        {
            var test = new AutoTestFile(@"
class Program
{
    private string _field1 = ""exempt1""; 
    private string _field2 = ""exempt2"";
}");

            var additionalFiles = new[]
            {
                GetExemptionsFile(new FileInfo("LiteralExemptions1.xml"), "<String>exempt1</String>"),
                GetExemptionsFile(new FileInfo("LiteralExemptions2.xml"), "<String>exempt2</String>"),
            };
            VerifyDiagnostics(test, additionalFiles);
        }

        [Fact]
        public void NI1004_ExemptionsFileHasInvalidXml_Diagnostic()
        {
            var invalidExemptionFile = new TestAdditionalDocument(
                ExampleLiteralExemptionsFileName,
                "<Exemptions><String>Foo</Exemptions>");

            VerifyDiagnostics(
                new TestFile(string.Empty),
                invalidExemptionFile,
                GetNI1004FileParseResult(
                    ExampleLiteralExemptionsFileName,
                    "The 'String' start tag on line 1 position 14 does not match the end tag of 'Exemptions'. Line 1, position 26."));
        }

        [Theory]
        [InlineData(ExemptFromStringLiteralsRuleAttribute, "ExemptFromStringLiteralsRule", "File")]
        [InlineData(ExemptFromStringLiteralsRuleAttribute, "ExemptFromStringLiteralsRule", "Constant")]

        public void NI1004_AttributeTargetMissing_Diagnostic(string attributeDefinition, string attribute, string scope)
        {
            var test = new TestFile($@"
{attributeDefinition}

[{attribute}(Scope = ""{scope}"")]
class Program
{{
}}");

            VerifyDiagnostics(test, GetNI1004AttributeMissingTargetResult($"{attribute}Attribute", scope, DiagnosticVerifier.DefaultFileName));
        }

        [Fact]
        public void NI1004_AttributeTargetMissing_TargetComesFromSymbol_NoDiagnostic()
        {
            var test = new AutoTestFile($@"
{ExemptFromStringLiteralsRuleAttribute}

[ExemptFromStringLiteralsRule(Scope = ""Class"")]
class Foo
{{
    class Bar
    {{
        private string _test = ""{ExampleLiteral}"";    // was the exemption added?
    }}
}}");

            VerifyDiagnostics(test);
        }

        private Rule GetNI1004LiteralRule(params string[] literals)
        {
            return new Rule(StringsShouldBeInResourcesAnalyzer.Rule, literals);
        }

        private DiagnosticResult GetNI1004AttributeMissingTargetResult(string attributeName, string scopeName, string filename)
        {
            return GetResult(StringsShouldBeInResourcesAnalyzer.AttributeMissingTargetRule, attributeName, scopeName, filename);
        }

        private DiagnosticResult GetNI1004FileParseResult(string fileName, string exceptionMessage)
        {
            return GetResult(fileName, StringsShouldBeInResourcesAnalyzer.FileParseRule, exceptionMessage);
        }

        private TestAdditionalDocument GetExemptionsFile(params string[] exemptions)
        {
            return GetExemptionsFile(null, exemptions);
        }

        private TestAdditionalDocument GetExemptionsFile(FileInfo? fileInfo, params string[] exemptions)
        {
            var defaultFileInfo = new FileInfo(ExampleLiteralExemptionsFileName);
            var documentContents = $@"
<Exemptions>
    {string.Join(Environment.NewLine, exemptions.Select(x => $"{x}"))}
</Exemptions>";

            return new TestAdditionalDocument((fileInfo ?? defaultFileInfo).Name, documentContents);
        }

        private string GetNonNullAttributesString(Dictionary<string, string> attributeNameToValue)
        {
            return string.Join(" ", attributeNameToValue.Where(x => !string.IsNullOrEmpty(x.Value)).Select(x => $@"{x.Key}=""{x.Value}"""));
        }
    }
}

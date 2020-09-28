using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using NationalInstruments.Tools.Analyzers.Style;
using NationalInstruments.Tools.Analyzers.TestUtilities;
using NationalInstruments.Tools.Analyzers.TestUtilities.TestFiles;
using NationalInstruments.Tools.Analyzers.TestUtilities.Verifiers;
using Xunit;

namespace NationalInstruments.Tools.Analyzers.UnitTests
{
    /// <summary>
    /// Tests for misspelled and unmeaningful names reported by the <see cref="SpellingAnalyzer"/>. Also tests
    /// that additional dictionaries can be provided and parsing errors are reported.
    /// </summary>
    public sealed class SpellingAnalyzerTests : NIDiagnosticAnalyzerTests<SpellingAnalyzer>
    {
        public enum DictionaryType
        {
            Xml,
            Dic,
        }

        public static IEnumerable<object[]> MisspelledMembers
            => new[]
            {
                new object[] { CreateTypeWithConstructor("<?>Clazz", isStatic: false), "Clazz", "Clazz.Clazz()" },
                new object[] { CreateTypeWithConstructor("<?>Clazz", isStatic: true), "Clazz", "Clazz.Clazz()" },
                new object[] { CreateTypeWithField("Program", "<?>_fild"), "fild", "Program._fild" },
                new object[] { CreateTypeWithEvent("Program", "<?>Evt"), "Evt", "Program.Evt" },
                new object[] { CreateTypeWithProperty("Program", "<?>Naem"), "Naem", "Program.Naem" },
                new object[] { CreateTypeWithMethod("Program", "<?>SomeMathod"), "Mathod", "Program.SomeMathod()" },
            };

        public static IEnumerable<object[]> UnmeaningfulMembers
            => new[]
            {
                new object[] { CreateTypeWithConstructor("<?>A", isStatic: false), "A" },
                new object[] { CreateTypeWithConstructor("<?>B", isStatic: false), "B" },
                new object[] { CreateTypeWithField("Program", "<?>_c"), "c" },
                new object[] { CreateTypeWithEvent("Program", "<?>D"), "D" },
                new object[] { CreateTypeWithProperty("Program", "<?>E"), "E" },
                new object[] { CreateTypeWithMethod("Program", "<?>F"), "F" },
            };

        public static IEnumerable<object[]> MisspelledMemberParameters
            => new[]
            {
                new object[] { CreateTypeWithConstructor("Program", false, "int <?>yourNaem"), "Naem", "yourNaem", "Program.Program(int)" },
                new object[] { CreateTypeWithMethod("Program", "Method", "int <?>yourNaem"), "Naem", "yourNaem", "Program.Method(int)" },
                new object[] { CreateTypeWithIndexer("Program", "int <?>yourNaem"), "Naem", "yourNaem", "Program.this[int]" },
            };

        [Theory]
        [InlineData("namespace Bar { }")]
        [InlineData("class Program { }")]
        [InlineData("class Program { void Member() { } }")]
        [InlineData("class Program { int _variable = 1; }")]
        [InlineData("class Program { void Member(string name) { } }")]
        [InlineData("class Program { delegate int GetNumber(string name); }")]
        [InlineData("class Program<TResource> { }")]
        public void NoMisspellings_Verify_NoDiagnostics(string source)
        {
            var testFile = new TestFile(source);

            VerifyDiagnostics(testFile);
        }

        [Fact]
        public void MisspellingAllowedByGlobalXmlDictionary_Verify_NoDiagnostics()
        {
            var testFile = new TestFile("class Clazz { }");
            var dictionary = CreateXmlDictionary(new[] { "clazz" });

            VerifyDiagnostics(testFile, dictionary);
        }

        [Fact]
        public void MisspellingAllowedByGlobalDicDictionary_Verify_NoDiagnostics()
        {
            var testFile = new TestFile("class Clazz { }");
            var dictionary = CreateDicDictionary(new[] { "clazz" });

            VerifyDiagnostics(testFile, dictionary);
        }

        [Fact]
        public void MisspellingsAllowedByMultipleGlobalDictionaries_Verify_NoDiagnostics()
        {
            var testFile = new TestFile(@"class Clazz { const string Naem = ""foo""; }");
            var xmlDictionary = CreateXmlDictionary(new[] { "clazz" });
            var dicDictionary = CreateDicDictionary(new[] { "naem" });

            VerifyDiagnostics(testFile, new[] { xmlDictionary, dicDictionary });
        }

        [Fact]
        public void CorrectWordDisallowedByGlobalXmlDictionary_Verify_EmitsDiagnostic()
        {
            var testFile = new AutoTestFile("class <?>Program { }", GetTypeRule("Program", "Program"));
            var dictionary = CreateXmlDictionary(null, new[] { "program" });

            VerifyDiagnostics(testFile, dictionary);
        }

        [Fact]
        public void MisspellingAllowedByProjectDictionary_Verify_NoDiagnostics()
        {
            var testFile = new TestFile("AssemblyA", "class Clazz {}");
            var dictionary = CreateDicDictionary(new[] { "clazz" });

            VerifyDiagnostics(testFile, null, GetProjectAdditionalFiles("AssemblyA", dictionary));
        }

        [Fact]
        public void MisspellingAllowedByDifferentProjectDictionary_Verify_EmitsDiagnostic()
        {
            var testFile = new AutoTestFile("AssemblyA", "class <?>Clazz {}", GetTypeRule("Clazz", "Clazz"));
            var dictionary = CreateDicDictionary(new[] { "clazz" });

            VerifyDiagnostics(testFile, null, GetProjectAdditionalFiles("AssemblyB", dictionary));
        }

        [Fact(Skip = "Assembly names are disabled for now")]
        public void AssemblyMisspelled_Verify_EmitsDiagnostic()
        {
            var testFile = new TestFile("MyAssambly", "class Program { }");

            VerifyDiagnostics(testFile, GetResult(SpellingAnalyzer.AssemblyRule, "Assambly", "MyAssambly"));
        }

        [Fact(Skip = "Unmeaningful rules disabled for now")]
        public void AssemblyUnmeaningful_Verify_EmitsDiagnostic()
        {
            var testFile = new TestFile("A", "class Program { }");

            VerifyDiagnostics(testFile, GetResult(SpellingAnalyzer.AssemblyMoreMeaningfulNameRule, "A"));
        }

        [Fact]
        public void NamespaceMisspelled_Verify_EmitsDiagnostic()
        {
            var testFile = new AutoTestFile("namespace Tests.<?>MyNarmspace { }", GetNamespaceRule("Narmspace", "Tests.MyNarmspace"));

            VerifyDiagnostics(testFile);
        }

        [Fact(Skip = "Unmeaningful rules disabled for now")]
        public void NamespaceUnmeaningful_Verify_EmitsDiagnostic()
        {
            var testFile = new AutoTestFile("namespace Tests.<|A|> { }", new Rule(SpellingAnalyzer.NamespaceMoreMeaningfulNameRule));

            VerifyDiagnostics(testFile);
        }

        [Theory]
        [InlineData("namespace MyNamespace { class <?>MyClazz { } }", "Clazz", "MyNamespace.MyClazz")]
        [InlineData("namespace MyNamespace { struct <?>MyStroct { } }", "Stroct", "MyNamespace.MyStroct")]
        [InlineData("namespace MyNamespace { enum <?>MyEnim { } }", "Enim", "MyNamespace.MyEnim")]
        [InlineData("namespace MyNamespace { interface <?>IMyFase { } }", "Fase", "MyNamespace.IMyFase")]
        [InlineData("namespace MyNamespace { delegate int <?>MyDelegete(); }", "Delegete", "MyNamespace.MyDelegete")]
        public void TypeMisspelled_Verify_EmitsDiagnostic(string source, string misspelling, string typeName)
        {
            var testFile = new AutoTestFile(source, GetTypeRule(misspelling, typeName));

            VerifyDiagnostics(testFile);
        }

        [Theory(Skip = "Unmeaningful rules disabled for now")]
        [InlineData("class <?>A { }", "A")]
        [InlineData("struct <?>B { }", "B")]
        [InlineData("enum <?>C { }", "C")]
        [InlineData("interface <?>ID { }", "D")]
        [InlineData("delegate int <?>E();", "E")]
        public void TypeUnmeaningful_Verify_EmitsDiagnostic(string source, string typeName)
        {
            var testFile = new AutoTestFile(source, GetTypeUnmeaningfulRule(typeName));

            VerifyDiagnostics(testFile);
        }

        [Theory]
        [MemberData(nameof(MisspelledMembers))]
        public void MemberMisspelled_Verify_EmitsDiagnostic(string source, string misspelling, string memberName)
        {
            var testFile = new AutoTestFile(source, GetMemberRule(misspelling, memberName));

            VerifyDiagnostics(testFile);
        }

        [Fact]
        public void MemberOverrideMisspelled_Verify_EmitsDiagnosticOnlyAtDefinition()
        {
            var source = @"
abstract class Parent
{
    protected abstract string <?>Naem { get; }

    public abstract int <?>Mathod();
}

class Child : Parent
{
    protected override string Naem => ""child"";

    public override int Mathod() => 0;
}

class Grandchild : Child
{
    protected override string Naem => ""grandchild"";

    public override int Mathod() => 1;
}";
            var testFile = new AutoTestFile(
                source,
                GetMemberRule("Naem", "Parent.Naem"),
                GetMemberRule("Mathod", "Parent.Mathod()"));

            VerifyDiagnostics(testFile);
        }

        [Theory(Skip = "Unmeaningful rules disabled for now")]
        [MemberData(nameof(UnmeaningfulMembers))]
        public void MemberUnmeaningful_Verify_EmitsDiagnostic(string source, string memberName)
        {
            var testFile = new AutoTestFile(source, GetMemberUnmeaningfulRule(memberName));

            VerifyDiagnostics(testFile);
        }

        [Fact]
        public void VariableMisspelled_Verify_EmitsDiagnostic()
        {
            var source = @"
class Program
{
    public Program()
    {
        var <?>myVoriable = 5;
    }
}";
            var testFile = new AutoTestFile(source, GetVariableRule("Voriable", "myVoriable"));

            VerifyDiagnostics(testFile);
        }

        [Theory]
        [MemberData(nameof(MisspelledMemberParameters))]
        public void MemberParameterMisspelled_Verify_EmitsDiagnostic(string source, string misspelling, string parameterName, string memberName)
        {
            var testFile = new AutoTestFile(source, GetMemberParameterRule(memberName, misspelling, parameterName));

            VerifyDiagnostics(testFile);
        }

        [Fact(Skip = "Unmeaningful rules disabled for now")]
        public void MemberParameterUnmeaningful_Verify_EmitsDiagnostic()
        {
            var source = @"
class Program
{
    public void Method(string <?>a)
    {
    }

    public string this[int <?>i] => null;
}";
            var testFile = new AutoTestFile(
                source,
                GetMemberParameterUnmeaningfulRule("Program.Method(string)", "a"),
                GetMemberParameterUnmeaningfulRule("Program.this[int]", "i"));

            VerifyDiagnostics(testFile);
        }

        [Fact]
        public void DelegateParameterMisspelled_Verify_EmitsDiagnostic()
        {
            var testFile = new AutoTestFile(
                "delegate void MyDelegate(string <?>firstNaem);",
                GetDelegateParameterRule("MyDelegate", "Naem", "firstNaem"));

            VerifyDiagnostics(testFile);
        }

        [Fact(Skip = "Unmeaningful rules disabled for now")]
        public void DelegateParameterUnmeaningful_Verify_EmitsDiagnostic()
        {
            var testFile = new AutoTestFile(
                "delegate void MyDelegate(string <?>a);",
                GetDelegateParameterUnmeaningfulRule("MyDelegate", "a"));

            VerifyDiagnostics(testFile);
        }

        [Theory]
        [InlineData("class MyClass<TCorrect, <?>TWroong> { }", "MyClass<TCorrect, TWroong>", "Wroong", "TWroong")]
        [InlineData("struct MyStructure<<?>TWroong> { }", "MyStructure<TWroong>", "Wroong", "TWroong")]
        [InlineData("interface IInterface<<?>TWroong> { }", "IInterface<TWroong>", "Wroong", "TWroong")]
        [InlineData("delegate int MyDelegate<<?>TWroong>();", "MyDelegate<TWroong>", "Wroong", "TWroong")]

        public void TypeTypeParameterMisspelled_Verify_EmitsDiagnostic(string source, string typeName, string misspelling, string typeParameterName)
        {
            var testFile = new AutoTestFile(source, GetTypeTypeParameterRule(typeName, misspelling, typeParameterName));

            VerifyDiagnostics(testFile);
        }

        [Theory(Skip = "Unmeaningful rules disabled for now")]
        [InlineData("class MyClass<<?>A> { }", "MyClass<A>", "A")]
        [InlineData("struct MyStructure<<?>B> { }", "MyStructure<B>", "B")]
        [InlineData("interface IInterface<<?>C> { }", "IInterface<C>", "C")]
        [InlineData("delegate int MyDelegate<<?>D>();", "MyDelegate<D>", "D")]
        public void TypeTypeParameterUnmeaningful_Verify_EmitsDiagnostic(string source, string typeName, string typeParameterName)
        {
            var testFile = new AutoTestFile(source, GetTypeTypeParameterUnmeaningfulRule(typeName, typeParameterName));

            VerifyDiagnostics(testFile);
        }

        [Fact]
        public void MethodTypeParameterMisspelled_Verify_EmitsDiagnostic()
        {
            var source = @"
class Program
{
    void Method<<?>TTipe>(TTipe item)
    {
    }
}";
            var testFile = new AutoTestFile(source, GetMethodTypeParameterRule("Program.Method<TTipe>(TTipe)", "Tipe", "TTipe"));

            VerifyDiagnostics(testFile);
        }

        [Fact(Skip = "Unmeaningful rules disabled for now")]
        public void MethodTypeParameterUnmeaningful_Verify_EmitsDiagnostic()
        {
            var source = @"
class Program
{
    void Method<<?>TA>(TA parameter)
    {
    }
}";
            var testFile = new AutoTestFile(source, GetMethodTypeParameterUnmeaningfulRule("Program.Method<TA>(TA)", "TA"));

            VerifyDiagnostics(testFile);
        }

        [Fact]
        public void MisspellingContainsOnlyCapitalizedLetters_Verify_NoDiagnostics()
        {
            var testFile = new TestFile("class FCCA { }");

            VerifyDiagnostics(testFile);
        }

        [Theory]
        [InlineData("0x0")]
        [InlineData("0xDEADBEEF")]
        public void MisspellingStartsWithADigit_Verify_NoDiagnostics(string misspelling)
        {
            var testFile = new TestFile($"enum Name {{ My{misspelling} }}");

            VerifyDiagnostics(testFile);
        }

        [Fact]
        public void MalformedXmlDictionary_Verify_EmitsDiagnostic()
        {
            var contents = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Dictionary>
    <Words>
        <Recognized>
            <Word>okay</Word>
            <word>bad</Word> <!-- xml tags are case-sensitive -->
        </Recognized>
    </Words>
</Dictionary>";
            var dictionary = new TestAdditionalDocument("CodeAnalysisDictionary.xml", contents);

            VerifyDiagnostics(
                new TestFile("class Program { }"),
                dictionary,
                GetFileParseResult(
                    "CodeAnalysisDictionary.xml",
                    "The 'word' start tag on line 6 position 14 does not match the end tag of 'Word'. Line 6, position 24."));
        }

        private static AdditionalText CreateXmlDictionary(IEnumerable<string> recognizedWords, IEnumerable<string> unrecognizedWords = null) =>
            CreateXmlDictionary("CodeAnalysisDictionary.xml", recognizedWords, unrecognizedWords);

        private static AdditionalText CreateXmlDictionary(string filename, IEnumerable<string> recognizedWords, IEnumerable<string> unrecognizedWords = null)
        {
            var contents = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Dictionary>
    <Words>
        <Recognized>{CreateXml(recognizedWords)}</Recognized>
        <Unrecognized>{CreateXml(unrecognizedWords)}</Unrecognized>
    </Words>
</Dictionary>";

            return new TestAdditionalDocument(filename, contents);

            string CreateXml(IEnumerable<string> words) =>
                string.Join(Environment.NewLine, words?.Select(x => $"<Word>{x}</Word>") ?? Enumerable.Empty<string>());
        }

        private static TestAdditionalDocument CreateDicDictionary(IEnumerable<string> recognizedWords)
        {
            var contents = string.Join(Environment.NewLine, recognizedWords);
            return new TestAdditionalDocument("CustomDictionary.dic", contents);
        }

        private static string CreateTypeWithConstructor(string typeName, bool isStatic, string parameter = "")
        {
            return $@"
#pragma warning disable {SpellingAnalyzer.RuleId}
class {typeName.TrimStart(new[] { '<', '?', '>' })}
#pragma warning restore {SpellingAnalyzer.RuleId}
{{
    {(isStatic ? "static " : string.Empty)}{typeName}({parameter}) {{ }}
}}";
        }

        private static string CreateTypeWithMethod(string typeName, string methodName, string parameter = "")
            => $"class {typeName} {{ void {methodName}({parameter}) {{ }} }}";

        private static string CreateTypeWithIndexer(string typeName, string parameter)
            => $"class {typeName} {{ int this[{parameter}] => 0; }}";

        private static string CreateTypeWithProperty(string typeName, string propertyName)
            => $"class {typeName} {{ string {propertyName} {{ get; }} }}";

        private static string CreateTypeWithField(string typeName, string fieldName)
            => $"class {typeName} {{ private string {fieldName}; }}";

        private static string CreateTypeWithEvent(string typeName, string eventName)
        {
            return $@"using System;

class {typeName} {{ event EventHandler<string> {eventName}; }}";
        }

        private static Rule GetNamespaceRule(string misspelling, string namespaceName)
            => new Rule(SpellingAnalyzer.NamespaceRule, misspelling, namespaceName);

        private static Rule GetTypeRule(string misspelling, string typeName)
            => new Rule(SpellingAnalyzer.TypeRule, misspelling, typeName);

        private static Rule GetTypeUnmeaningfulRule(string typeName)
            => new Rule(SpellingAnalyzer.TypeMoreMeaningfulNameRule, typeName);

        private static Rule GetMemberRule(string misspelling, string memberName)
            => new Rule(SpellingAnalyzer.MemberRule, misspelling, memberName);

        private static Rule GetMemberUnmeaningfulRule(string memberName)
            => new Rule(SpellingAnalyzer.MemberMoreMeaningfulNameRule, memberName);

        private static Rule GetVariableRule(string misspelling, string variableName)
            => new Rule(SpellingAnalyzer.VariableRule, misspelling, variableName);

        private static Rule GetMemberParameterRule(string memberName, string misspelling, string parameterName)
            => new Rule(SpellingAnalyzer.MemberParameterRule, memberName, misspelling, parameterName);

        private static Rule GetMemberParameterUnmeaningfulRule(string memberName, string parameterName)
            => new Rule(SpellingAnalyzer.MemberParameterMoreMeaningfulNameRule, memberName, parameterName);

        private static Rule GetDelegateParameterRule(string delegateName, string misspelling, string parameterName)
            => new Rule(SpellingAnalyzer.DelegateParameterRule, delegateName, misspelling, parameterName);

        private static Rule GetDelegateParameterUnmeaningfulRule(string delegateName, string parameterName)
            => new Rule(SpellingAnalyzer.DelegateParameterMoreMeaningfulNameRule, delegateName, parameterName);

        private static Rule GetTypeTypeParameterRule(string typeName, string misspelling, string typeParameterName)
            => new Rule(SpellingAnalyzer.TypeTypeParameterRule, typeName, misspelling, typeParameterName);

        private static Rule GetTypeTypeParameterUnmeaningfulRule(string typeName, string typeParameterName)
            => new Rule(SpellingAnalyzer.TypeTypeParameterMoreMeaningfulNameRule, typeName, typeParameterName);

        private static Rule GetMethodTypeParameterRule(string methodName, string misspelling, string typeParameterName)
            => new Rule(SpellingAnalyzer.MethodTypeParameterRule, methodName, misspelling, typeParameterName);

        private static Rule GetMethodTypeParameterUnmeaningfulRule(string methodName, string typeParameterName)
            => new Rule(SpellingAnalyzer.MethodTypeParameterMoreMeaningfulNameRule, methodName, typeParameterName);

        private DiagnosticResult GetFileParseResult(string fileName, string message)
            => GetResult(SpellingAnalyzer.FileParseRule, fileName, message);
    }
}

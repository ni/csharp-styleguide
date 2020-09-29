using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NationalInstruments.Analyzers.Utilities.Extensions;
using Xunit;

namespace NationalInstruments.Analyzers.Utilities.UnitTests
{
    /// <summary>
    /// Tests that <see cref="Extensions.ISymbolExtensions.GetFullName(ISymbol)"/> returns a unique name for different
    /// types of symbols.
    /// </summary>
    public class ISymbolExtensionTests : UtilitiesTestBase
    {
        [Fact]
        public void GetFullName_IndexerCall_MatchesExpected()
        {
            const string ExpectedIndexerName = "My.Namespace.Program.this[System.String]";

            VerifyCSharp(
                @"
namespace My.Namespace
{
    class Program
    {
        public Program()
        {
            int num = this[""a""];
        }

        public int this[string]
        {
            get { return 0; }
            set { _value = value; }
        }
    }
}",
                (tree, compilation) => AssertSymbolFullNameMatchesExpected<ElementAccessExpressionSyntax>(tree, compilation, ExpectedIndexerName));
        }

        [Fact]
        public void GetFullName_InstanceMethodCall_MatchesExpected()
        {
            const string ExpectedMethodName = "System.String.Equals(System.String, System.String)";

            VerifyCSharp(
                @"
using System;

namespace My.Namespace
{
    class Program
    {
        private bool _isEqual = string.Equals(""a"", ""b"");
    }
}",
                (tree, compilation) => AssertSymbolFullNameMatchesExpected<InvocationExpressionSyntax>(tree, compilation, ExpectedMethodName));
        }

        [Fact]
        public void GetFullName_StaticMethodCall_MatchesExpected()
        {
            const string ExpectedMethodName = "System.Text.Encoding.GetEncoding(System.String)";

            VerifyCSharp(
                @"
using System.Text;

namespace My.Namespace
{
    class Program
    {
        private Encoding _encoding = Encoding.GetEncoding(""a"");
    }
}",
                (tree, compilation) => AssertSymbolFullNameMatchesExpected<InvocationExpressionSyntax>(tree, compilation, ExpectedMethodName));
        }

        [Fact]
        public void GetFullName_ConstructorMethodCall_MatchesExpected()
        {
            const string ExpectedMethodName = "System.IO.StringReader.StringReader(System.String)";

            VerifyCSharp(
                @"
using System.IO;

namespace My.Namespace
{
    class Program
    {
        private StringReader _reader = new StringReader(""a"");
    }
}",
                (tree, compilation) => AssertSymbolFullNameMatchesExpected<ObjectCreationExpressionSyntax>(tree, compilation, ExpectedMethodName));
        }

        [Fact]
        public void GetFullName_GenericMethodCall_MatchesExpected()
        {
            const string ExpectedMethodName = "System.Collections.Generic.Dictionary<System.String, System.Int32>.this[System.String]";

            VerifyCSharp(
                @"
using System.Collections.Generic;

namespace My.Namespace
{
    class Program
    {
        private Dictionary<string, int> _mapping = new Dictionary<string, int>();

        public Program()
        {
            int num = _mapping[""a""];
        }
    }
}",
                (tree, compilation) => AssertSymbolFullNameMatchesExpected<ElementAccessExpressionSyntax>(tree, compilation, ExpectedMethodName));
        }

        [Fact]
        public void GetFullName_ExtensionMethodCall_MatchesExpected()
        {
            const string ExpectedMethodName = "My.Namespace.Extensions.NotLocalized()";

            VerifyCSharp(
                @"
namespace My.Namespace
{
    public static class Extensions
    {
        public static string NotLocalized(this string text)
        {
            return text;
        }
    }

    class Program
    {
        private string _name = ""Name"".NotLocalized();
    }
}",
                (tree, compilation) => AssertSymbolFullNameMatchesExpected<InvocationExpressionSyntax>(tree, compilation, ExpectedMethodName));
        }

        [Fact]
        public void GetFullName_OperatorMethodCall_MatchesExpected()
        {
            const string ExpectedOperatorName = "System.String.operator ==(System.String, System.String)";

            VerifyCSharp(
                @"
using System;

namespace My.Namespace
{
    class Program
    {
        private bool _isEqual = ""a"" == ""b"";
    }
}",
                (tree, compilation) => AssertSymbolFullNameMatchesExpected<BinaryExpressionSyntax>(tree, compilation, ExpectedOperatorName));
        }

        [Fact]
        public void GetFullName_DelegateCall_MatchesExpected()
        {
            const string ExpectedDelegateName = "My.Namespace.SayHello.Invoke(System.String)";

            VerifyCSharp(
                @"
namespace My.Namespace
{
    delegate void SayHello(string name);

    class Program
    {
        private SayHello _greet = new SayHello(name => Console.WriteLine(name));

        public Program()
        {
            _greet(""Myself"");
        }
    }
}",
                (tree, compilation) => AssertSymbolFullNameMatchesExpected<InvocationExpressionSyntax>(tree, compilation, ExpectedDelegateName));
        }

        [Fact]
        public void GetFullName_FieldDeclaration_MatchesExpected()
        {
            const string ExpectedFieldName = "My.Namespace.Program.MyField";

            VerifyCSharp(
                @"
namespace My.Namespace
{
    class Program
    {
        public string MyField = ""foo"";
    }
}",
                (tree, compilation) => AssertSymbolFullNameMatchesExpected<VariableDeclaratorSyntax>(tree, compilation, ExpectedFieldName));
        }

        [Fact]
        public void GetFullName_PropertyDeclaration_MatchesExpected()
        {
            const string ExpectedPropertyName = "My.Namespace.Program.Name";
            const string ExpectedPropertyGetName = "My.Namespace.Program.Name.get";
            const string ExpectedPropertySetName = "My.Namespace.Program.Name.set";

            VerifyCSharp(
                @"
namespace My.Namespace
{
    class Program
    {
        public string Name { get; set; }
    }
}",
                (tree, compilation) =>
                {
                    var symbol = GetSymbol<PropertyDeclarationSyntax>(tree, compilation);
                    Assert.Equal(ExpectedPropertyName, symbol.GetFullName());

                    var property = symbol as IPropertySymbol;

                    Assert.NotNull(property);
                    Assert.Equal(ExpectedPropertyGetName, property.GetMethod.GetFullName());
                    Assert.Equal(ExpectedPropertySetName, property.SetMethod.GetFullName());
                });
        }

        [Fact]
        public void GetFullName_IndexerDeclaration_MatchesExpected()
        {
            const string ExpectedIndexerName = "My.Namespace.Program.this[System.String]";
            const string ExpectedIndexerGetName = "My.Namespace.Program.this[System.String].get";
            const string ExpectedIndexerSetName = "My.Namespace.Program.this[System.String].set";

            VerifyCSharp(
                @"
namespace My.Namespace
{
    class Program
    {
        private int _value;

        public int this[string]
        {
            get { return 0; }
            set { _value = value; }
        }
    }
}",
                (tree, compilation) =>
                {
                    var symbol = GetSymbol<IndexerDeclarationSyntax>(tree, compilation);
                    Assert.Equal(ExpectedIndexerName, symbol.GetFullName());

                    var property = symbol as IPropertySymbol;

                    Assert.NotNull(property);
                    Assert.Equal(ExpectedIndexerGetName, property.GetMethod.GetFullName());
                    Assert.Equal(ExpectedIndexerSetName, property.SetMethod.GetFullName());
                });
        }

        [Fact]
        public void GetFullName_MethodDeclaration_MatchesExpected()
        {
            const string ExpectedMethodName = "My.Namespace.Program.Method(System.String, System.Int32)";

            VerifyCSharp(
                @"
namespace My.Namespace
{
    class Program
    {
        public void Method(string name, int age)
        {
        }
    }
}",
                (tree, compilation) => AssertSymbolFullNameMatchesExpected<MethodDeclarationSyntax>(tree, compilation, ExpectedMethodName));
        }

        [Fact]
        public void GetFullName_DelegateDeclaration_MatchesExpected()
        {
            const string ExpectedDelegateName = "My.Namespace.MyDelegate";

            VerifyCSharp(
                @"
namespace My.Namespace
{
    delegate int MyDelegate(string name);
}",
                (tree, compilation) => AssertSymbolFullNameMatchesExpected<DelegateDeclarationSyntax>(tree, compilation, ExpectedDelegateName));
        }

        [Theory]
        [InlineData("enum MyEnum {}", "My.Namespace.MyEnum")]
        [InlineData("class MyClass {}", "My.Namespace.MyClass")]
        [InlineData("struct MyStruct {}", "My.Namespace.MyStruct")]
        [InlineData("interface IInterface {}", "My.Namespace.IInterface")]
        public void GetFullName_TypeDeclaration_MatchesExpected(string typeDefinition, string expectedTypeName)
        {
            VerifyCSharp(
                $@"
namespace My.Namespace
{{
    {typeDefinition}
}}",
                (tree, compilation) => AssertSymbolFullNameMatchesExpected<BaseTypeDeclarationSyntax>(tree, compilation, expectedTypeName));
        }

        [Fact]
        public void GetFullName_NestedClassDeclaration_MatchesExpected()
        {
            const string ExpectedClassName = "My.Namespace.Foo+My.Namespace.Foo.Bar";

            VerifyCSharp(
                @"
namespace My.Namespace
{
    class Foo
    {
        class Bar
        {
        }
    }
}",
                (tree, compilation) => AssertSymbolFullNameMatchesExpected<ClassDeclarationSyntax>(tree, compilation, ExpectedClassName));
        }

        [Fact]
        public void GetFullName_NamespaceDeclaration_MatchesExpected()
        {
            const string ExpectedNamespaceName = "My.Namespace";

            VerifyCSharp(
                @"
namespace My.Namespace
{
}",
                (tree, compilation) => AssertSymbolFullNameMatchesExpected<NamespaceDeclarationSyntax>(tree, compilation, ExpectedNamespaceName));
        }

        private void AssertSymbolFullNameMatchesExpected<TSyntax>(SyntaxTree tree, Compilation compilation, string expectedName)
            where TSyntax : SyntaxNode
        {
            var symbol = GetSymbol<TSyntax>(tree, compilation);

            Assert.Equal(expectedName, symbol.GetFullName());
        }

        private ISymbol GetSymbol<TSyntax>(SyntaxTree tree, Compilation compilation)
            where TSyntax : SyntaxNode
        {
            var semanticModel = compilation.GetSemanticModel(tree);

            // Get the last occurrence of TSyntax with the assumption that any other occurrences are needed for test setup
            var syntax = tree.GetRoot().DescendantNodes().OfType<TSyntax>().Last();

            return syntax.GetDeclaredOrReferencedSymbol(semanticModel);
        }
    }
}

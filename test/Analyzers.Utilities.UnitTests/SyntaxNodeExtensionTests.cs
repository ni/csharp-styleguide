using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NationalInstruments.Tools.Analyzers.Utilities.Extensions;
using Xunit;

namespace NationalInstruments.Tools.Analyzers.Utilities.UnitTests
{
    public sealed class SyntaxNodeExtensionTests : UtilitiesTestBase
    {
        public static IEnumerable<object[]> MemberInvocations =>
            new[]
            {
                new object[] { "var self = new Program();" },   // constructor
                new object[] { "Method();" },                   // method
                new object[] { @"var count = this[""foo""];" }, // indexer
                new object[] { "bool equal = 1 == 2;" },        // binary operator
                new object[] { "var five = +5;" },              // unary operator
            };

        [Fact]
        [Trait("TestCategory", "System")]
        public void GetDeclaredOrReferencedSymbol_DeclaredSyntax_MatchesGetDeclaredSymbol()
        {
            VerifyCSharp(
                @"
class Program
{
}",
                (tree, compilation) =>
                {
                    var semanticModel = compilation.GetSemanticModel(tree);
                    var syntax = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();

                    var expectedSymbol = semanticModel.GetDeclaredSymbol(syntax);
                    var symbol = syntax.GetDeclaredOrReferencedSymbol(semanticModel);

                    Assert.Equal(expectedSymbol, symbol);
                });
        }

        [Fact]
        [Trait("TestCategory", "System")]
        public void GetDeclaredOrReferencedSymbol_NonDeclaredSyntax_MatchesGetSymbolInfoSymbol()
        {
            VerifyCSharp(
                @"
namespace Test
{
}",
                (tree, compilation) =>
                {
                    var semanticModel = compilation.GetSemanticModel(tree);
                    var syntax = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().First();

                    var expectedSymbol = semanticModel.GetSymbolInfo(syntax).Symbol;
                    var symbol = syntax.GetDeclaredOrReferencedSymbol(semanticModel);

                    Assert.Equal(expectedSymbol, symbol);
                });
        }

        [Theory]
        [MemberData(nameof(MemberInvocations))]
        public void IsParameterizedMemberInvocation_MemberInvocation_ReturnsTrue(string invocation)
        {
            var code = $@"
class Program
{{
    private Program()
    {{
    }}

    public void Create()
    {{
        {invocation}
    }}

    public void Method()
    {{
    }}

    public int this[string name]
    {{
        get {{ return 0; }}
        set {{ }}
    }}
}}";

            VerifyCSharp(
                code,
                (tree, compilation) =>
                {
                    var hasMethodInvocationSyntax = tree.GetRoot().DescendantNodes().Where(x => x.IsParameterizedMemberInvocation()).Any();

                    Assert.True(hasMethodInvocationSyntax);
                });
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NationalInstruments.Analyzers.Utilities.Extensions;
using Xunit;

namespace NationalInstruments.Analyzers.Utilities.UnitTests
{
    /// <summary>
    /// Tests that <see cref="ITypeSymbolExtensions.GetBaseTypesAndThis(ITypeSymbol)"/> returns a given type and its bases.
    /// </summary>
    public class ITypeSymbolExtensionTests : UtilitiesTestBase
    {
        public static IEnumerable<object[]> TypesWithBases =>
            new[]
            {
                new object[]
                {
                    @"
// interface doesn't matter
interface IBase
{
}

class Base : IBase
{
}

class Program : Base
{
    public void Method()
    {
    }
}",
                    new[] { "Program", "Base", "Object" },
                },
                new object[]
                {
                    @"
class Program
{
    public void Method()
    {
    }
}",
                    new[] { "Program", "Object" },
                },
                new object[]
                {
                    @"
class Foo
{
    class Bar
    {
        public void Method()
        {
        }
    }
}",
                    new[] { "Bar", "Object" },
                },
            };

        [Theory]
        [MemberData(nameof(TypesWithBases))]
        public void GetBaseTypesAndThis_HasBases_ReturnsThisAndBases(string code, string[] expectedTypes)
        {
            VerifyCSharp(
                code,
                (tree, compilation) =>
                {
                    var methodSyntax = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
                    var method = methodSyntax.GetDeclaredOrReferencedSymbol(compilation.GetSemanticModel(tree));

                    var i = 0;
                    foreach (var type in (method?.ContainingType?.GetBaseTypesAndThis()).ToSafeEnumerable())
                    {
                        Assert.Equal(expectedTypes[i++], type.Name);
                    }
                });
        }
    }
}

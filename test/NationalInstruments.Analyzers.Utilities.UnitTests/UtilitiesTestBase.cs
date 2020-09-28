using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace NationalInstruments.Analyzers.Utilities.UnitTests
{
    public class UtilitiesTestBase
    {
        protected void VerifyCSharp(string code, Action<SyntaxTree, Compilation> verificationFunc)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            var compilation = CSharpCompilation.Create("TestCompilation", syntaxTrees: new[] { tree }, references: new[] { mscorlib });

            verificationFunc(tree, compilation);
        }
    }
}

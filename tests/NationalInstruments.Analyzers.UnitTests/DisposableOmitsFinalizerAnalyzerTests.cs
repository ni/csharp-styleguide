using System.Data.Common;
using NationalInstruments.Analyzers.Correctness;
using NationalInstruments.Analyzers.TestUtilities;
using NationalInstruments.Analyzers.TestUtilities.TestFiles;
using NationalInstruments.Analyzers.TestUtilities.Verifiers;
using Xunit;

namespace NationalInstruments.Analyzers.UnitTests
{
    /// <summary>
    /// Tests that <see cref="DisposableOmitsFinalizerAnalyzer" /> emits a violation when necessary.
    /// </summary>
    public sealed class DisposableOmitsFinalizerAnalyzerTests : NIDiagnosticAnalyzerTests<DisposableOmitsFinalizerAnalyzer>
    {
        [Fact]
        public void NI1816X_ClassNotDerivedHasFinalizer_NoDiagnostic()
        {
            var goodCode = @"
    using System;
    namespace NationalInstruments.Core
    {
        internal class NonDerivedDisposable : IDisposable
        {
            public NonDerivedDisposable()
            {
            }
            ~NonDerivedDisposable()
            {
            }
            public void Dispose()
            {
            }
        }
    }";

            var test = new TestFile(goodCode);
            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1816X_ClassDerivedOmitsFinalizer_NoDiagnostic()
        {
            var goodCode = @"
    using System;
    namespace NationalInstruments.Core
    {
        internal class Disposable : IDisposable
        {
            public Disposable()
            {
            }
            public void Dispose()
            {
            }
        }
        internal class DerivedDisposable : Disposable
        {   
            public DerivedDisposable()
            {
            }
        }
    }";

            var test = new TestFile(goodCode);
            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1816X_ClassDerivedHasFinalizer_HasDiagnostic()
        {
            var badCode = @"
    using System;
    namespace NationalInstruments.Core
    {
        internal class Disposable : IDisposable
        {
            public Disposable()
            {
            }
            public void Dispose()
            {
            }
        }
        internal class DerivedDisposable : Disposable
        {   
            public DerivedDisposable()
            {
            }
            ~DerivedDisposable()
            {
            }
        }
    }";

            var test = new TestFile(badCode);
            var expectedFailure = GetResultAt(14, 24, DisposableOmitsFinalizerAnalyzer.Rule, "DerivedDisposable");
            VerifyDiagnostics(test, expectedFailure);
        }
    }
}

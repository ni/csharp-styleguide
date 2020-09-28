using NationalInstruments.Tools.Analyzers.Correctness;
using NationalInstruments.Tools.Analyzers.TestUtilities;
using NationalInstruments.Tools.Analyzers.TestUtilities.TestFiles;
using NationalInstruments.Tools.Analyzers.TestUtilities.Verifiers;
using Xunit;

namespace NationalInstruments.Tools.Analyzers.UnitTests
{
    /// <summary>
    /// Unit-tests that verify diagnostics are emitted when the Boolean literal 'false' is
    /// returned in any code branch and any interface implementation (implicit/explicit).
    /// </summary>
    public sealed class ReceiveWeakEventMustReturnTrueAnalyzerTests : NIDiagnosticAnalyzerTests<ReceiveWeakEventMustReturnTrueAnalyzer>
    {
        [Fact]
        public void NI1005_ReceiveWeakEventReturnsFalse_Diagnostic()
        {
            var test = new TestFile(@"
using System;
using System.Windows;

class MyEvents : IWeakEventListener
{
    public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
    {
        return false;
    }
}");

            VerifyDiagnostics(test, GetNI1005ResultAt(9, 16));
        }

        [Fact]
        public void NI1005_ReceiveWeakEventReturnsCalculatedFalse_NoDiagnostic()
        {
            var test = new TestFile(@"
using System;
using System.Windows;

class MyEvents : IWeakEventListener
{
    public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
    {
        return Convert.ToBoolean(0);
    }
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void NI1005_ReceiveWeakEventConditionallyReturnsFalse_Diagnostic()
        {
            var test = new TestFile(@"
using System;
using System.Windows;

class MyEvents : IWeakEventListener
{
    public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
    {
        if (sender == null) {
            return false;
        }

        return true;
    }
}");

            VerifyDiagnostics(test, GetNI1005ResultAt(10, 20));
        }

        [Fact]
        public void NI1005_ExplicitReceiveWeakEventReturnsFalse_Diagnostic()
        {
            var test = new TestFile(@"
using System;
using System.Windows;

class MyEvents : IWeakEventListener
{
    bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
    {
        return false;
    }
}");

            VerifyDiagnostics(test, GetNI1005ResultAt(9, 16));
        }

        [Fact]
        public void NI1005_NonImplementingReceiveWeakEventReturnsFalse_NoDiagnostic()
        {
            var test = new TestFile(@"
using System;
using System.Windows;

class MyEvents : IWeakEventListener
{
    bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
    {
        return true;
    }

    bool ReceiveWeakEvent()
    {
        return false;
    }
}");

            VerifyDiagnostics(test);
        }

        private DiagnosticResult GetNI1005ResultAt(int line, int column)
        {
            return GetResultAt(line, column, ReceiveWeakEventMustReturnTrueAnalyzer.Rule);
        }
    }
}

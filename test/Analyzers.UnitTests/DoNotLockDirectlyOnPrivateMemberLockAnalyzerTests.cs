using NationalInstruments.Tools.Analyzers.Correctness;
using NationalInstruments.Tools.Analyzers.TestUtilities;
using NationalInstruments.Tools.Analyzers.TestUtilities.TestFiles;
using NationalInstruments.Tools.Analyzers.TestUtilities.Verifiers;
using Xunit;

namespace NationalInstruments.Tools.Analyzers.UnitTests
{
    /// <summary>
    /// Tests for <see cref="DoNotLockDirectlyOnPrivateMemberLockAnalyzer"/>.
    /// </summary>
    public sealed class DoNotLockDirectlyOnPrivateMemberLockAnalyzerTests : NIDiagnosticAnalyzerTests<DoNotLockDirectlyOnPrivateMemberLockAnalyzer>
    {
        private const string PrivateMemberLock = @"
using System;
using NationalInstruments.Core;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}

namespace NationalInstruments.Core
{
    public class PrivateMemberLock
    {
        public IDisposable Acquire()
        {
            return new Disposable();
        }
    }
}
";

        [Fact]
        public void DoNotLockDirectlyOnPrivateMemberLockAnalyzer_LockOnPrivateMemberLock_Diagnostic()
        {
            var test = new AutoTestFile(
                PrivateMemberLock + @"
class ClassUnderTest
{
    private PrivateMemberLock _privateMemberLock = new PrivateMemberLock();
    public void MethodUnderTest()
    {
        <|>lock (_privateMemberLock)
        {
        }
    }
}",
                GetNI1016DoNotLockDirectlyOnPrivateMemberLockRule());

            VerifyDiagnostics(test);
        }

        [Fact]
        public void DoNotLockDirectlyOnPrivateMemberLockAnalyzer_UsingOnPrivateMemberLockAcquire_NoDiagnostic()
        {
            var test = new AutoTestFile(PrivateMemberLock + @"
class ClassUnderTest
{
    private PrivateMemberLock _privateMemberLock = new PrivateMemberLock();
    public void MethodUnderTest()
    {
        using (_privateMemberLock.Acquire())
        {
        }
    }
}");
            VerifyDiagnostics(test);
        }

        [Fact]
        public void DoNotLockDirectlyOnPrivateMemberLockAnalyzer_LockOnObject_NoDiagnostic()
        {
            var test = new AutoTestFile(PrivateMemberLock + @"
class ClassUnderTest
{
    private object _object = new object();
    public void MethodUnderTest()
    {
        lock (_object)
        {
        }
    }
}");
            VerifyDiagnostics(test);
        }

        [Fact]
        public void DoNotLockDirectlyOnPrivateMemberLockAnalyzer_LockOnMethodThatReturnsPrivateMemberLock_Diagnostic()
        {
            var test = new AutoTestFile(
                PrivateMemberLock + @"
class ClassUnderTest
{
    private PrivateMemberLock GetPrivateMemberLock()
    {
        return new PrivateMemberLock();
    }

    public void MethodUnderTest()
    {
        <|>lock (GetPrivateMemberLock())
        {
        }
    }
}",
                GetNI1016DoNotLockDirectlyOnPrivateMemberLockRule());

            VerifyDiagnostics(test);
        }

        private Rule GetNI1016DoNotLockDirectlyOnPrivateMemberLockRule()
        {
            return new Rule(DoNotLockDirectlyOnPrivateMemberLockAnalyzer.Rule);
        }
    }
}

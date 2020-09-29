using NationalInstruments.Analyzers.Correctness;
using NationalInstruments.Analyzers.TestUtilities;
using NationalInstruments.Analyzers.TestUtilities.TestFiles;
using NationalInstruments.Analyzers.TestUtilities.Verifiers;
using Xunit;

namespace NationalInstruments.Analyzers.UnitTests
{
    public sealed class AwaitInReadLockOrTransactionAnalyzerTests : NIDiagnosticAnalyzerTests<AwaitInReadLockOrTransactionAnalyzer>
    {
        [Fact]
        public void AwaitInReadLockOrTransaction_UsingInvalidElementAPI_Diagnostic()
        {
            var test = new AutoTestFile(
                @"
using System;
using System.Threading.Tasks;
using NationalInstruments.SourceModel;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}

namespace NationalInstruments.SourceModel
{
    public class Element
    {
        public Element()
        {
        }

        public IDisposable AcquireModelReadLock()
        {
            return new Disposable();
        }
    }
}

class ClassUnderTest
{
    public async Task Awaitable()
    {
    }

    public async Task MethodUnderTest()
    {
        using ((new Element()).AcquireModelReadLock())
        {
            <|>await Awaitable();
        }
    }
}",
                GetNI1015AwaitInReadLockOrTransactionRule());

            VerifyDiagnostics(test);
        }

        [Fact]
        public void AwaitInReadLockOrTransaction_UsingInvalidElementAPIWithNoAwait_NoDiagnostic()
        {
            var test = new AutoTestFile(@"
using System;
using System.Threading.Tasks;
using NationalInstruments.SourceModel;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}

namespace NationalInstruments.SourceModel
{
    public class Element
    {
        public Element()
        {
        }

        public IDisposable AcquireModelReadLock()
        {
            return new Disposable();
        }
    }
}

class ClassUnderTest
{
    public async Task Awaitable()
    {
    }

    public async Task MethodUnderTest()
    {
        using ((new Element()).AcquireModelReadLock())
        {
            var notAnAwait = new Element();
        }
    }
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void AwaitInReadLockOrTransaction_UsingAllowedAPIOnDisallowedClass_NoDiagnostic()
        {
            var test = new AutoTestFile(@"
using System;
using System.Threading.Tasks;
using NationalInstruments.SourceModel;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}

namespace NationalInstruments.SourceModel
{
    public class Element
    {
        public Element()
        {
        }

        public IDisposable AllowedMethod()
        {
            return new Disposable();
        }
    }
}

class ClassUnderTest
{
    public async Task Awaitable()
    {
    }

    public async Task MethodUnderTest()
    {
        using (var t = (new Element()).AllowedMethod())
        {
            await Awaitable();
        }
    }
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void AwaitInReadLockOrTransaction_UsingDisallowedAPIOnAllowedClass_NoDiagnostic()
        {
            var test = new AutoTestFile(@"
using System;
using System.Threading.Tasks;
using NationalInstruments.NotSourceModel;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}

namespace NationalInstruments.NotSourceModel
{
    public class Element
    {
        public Element()
        {
        }

        public IDisposable AcquireModelReadLock()
        {
            return new Disposable();
        }
    }
}

class ClassUnderTest
{
    public async Task Awaitable()
    {
    }

    public async Task MethodUnderTest()
    {
        using ((new Element()).AcquireModelReadLock())
        {
            await Awaitable();
        }
    }
}");

            VerifyDiagnostics(test);
        }

        [Fact]
        public void AwaitInReadLockOrTransaction_UsingInvalidITransactionManagerAPI_Diagnostic()
        {
            var test = new AutoTestFile(
                @"
using System;
using System.Threading.Tasks;
using NationalInstruments.SourceModel;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}

namespace NationalInstruments.SourceModel
{
    public interface ITransactionManager
    {
        IDisposable BeginTransaction();
    }

    public class TransactionManager : ITransactionManager
    {
        public TransactionManager()
        {
        }

        public IDisposable BeginTransaction()
        {
            return new Disposable();
        }
    }
}

class ClassUnderTest
{
    public async Task Awaitable()
    {
    }

    public async Task MethodUnderTest()
    {
        using (var t = (new TransactionManager()).BeginTransaction())
        {
            <|>await Awaitable();
        }
    }
}",
                GetNI1015AwaitInReadLockOrTransactionRule());

            VerifyDiagnostics(test);
        }

        [Fact]
        public void AwaitInReadLockOrTransaction_UsingInvalidITransactionManagerAPIInNestedStructure_Diagnostic()
        {
            var test = new AutoTestFile(
                @"
using System;
using System.Threading.Tasks;
using NationalInstruments.SourceModel;

public class Disposable : IDisposable
{
    public void Dispose()
    {
    }
}

namespace NationalInstruments.SourceModel
{
    public interface ITransactionManager
    {
        IDisposable BeginTransaction();
    }

    public class TransactionManager : ITransactionManager
    {
        public TransactionManager()
        {
        }

        public IDisposable BeginTransaction()
        {
            return new Disposable();
        }
    }
}

class ClassUnderTest
{
    public async Task Awaitable()
    {
    }

    public async Task MethodUnderTest()
    {
        int i = 0;
        if (i == 0)
        {
            using (var t = (new TransactionManager()).BeginTransaction())
            {
                <|>await Awaitable();
            }
        }
    }
}",
                GetNI1015AwaitInReadLockOrTransactionRule());

            VerifyDiagnostics(test);
        }

        private Rule GetNI1015AwaitInReadLockOrTransactionRule()
        {
            return new Rule(AwaitInReadLockOrTransactionAnalyzer.Rule);
        }
    }
}

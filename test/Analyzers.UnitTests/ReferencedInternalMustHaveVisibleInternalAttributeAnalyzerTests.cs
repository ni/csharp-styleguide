using NationalInstruments.Tools.Analyzers.Correctness;
using NationalInstruments.Tools.Analyzers.TestUtilities;
using NationalInstruments.Tools.Analyzers.TestUtilities.TestFiles;
using NationalInstruments.Tools.Analyzers.TestUtilities.Verifiers;
using Xunit;

namespace NationalInstruments.Tools.Analyzers.UnitTests
{
    public class ReferencedInternalMustHaveVisibleInternalAttributeAnalyzerTests :
        NIDiagnosticAnalyzerTests<ReferencedInternalMustHaveVisibleInternalAttributeAnalyzer>
    {
        private const string UsingSystem = @"
using System;";

        private const string Namespace = @"
namespace NationalInstruments.MyNamespace
{";

        private const string AttributeDefinition = @"
namespace NationalInstruments.Core
{
    // AttributeUsage copied from ObsoleteAttribute.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor
                        | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event
                        | AttributeTargets.Interface | AttributeTargets.Delegate, Inherited=false)]
    public sealed class VisibleInternalAttribute : Attribute
    {
    }
}
";

        private const string CallerAssemblyName = "CallerAssembly";

        private const string CalleeAssemblyName = "CalleeAssembly";

        [Fact]
        public void ReferenceToInternalInSameAssembly_InternalsReferencedWithAttribute_NoDiagnostic()
        {
            var test = new AutoTestFile(GenerateCalleeHeader() + @"
    public class Program
    {
        internal InternalType InternalTypeInstance { get; set; }

        internal void Method(InternalType typeInstance)
        {
            InternalType.StaticMethod();
            typeInstance.RegularMethod();
            typeInstance.InternalMethod();
        }
    }
    
    [NationalInstruments.Core.VisibleInternalAttribute]
    internal class InternalType
    {
        public static void StaticMethod()
        {
        }

        public void RegularMethod()
        {
        }
        
        [NationalInstruments.Core.VisibleInternal]
        internal void InternalMethod()
        {
        }
    }
}");
            VerifyDiagnostics(test);
        }

        [Fact]
        public void ReferenceToInternalInSameAssembly_InternalMethodAndTypeReferencedWithoutAttribute_NoDiagnostic()
        {
            var test = new AutoTestFile(
                GenerateCallerHeader() + @"
    public class Program
    {
        internal InternalType InternalTypeInstance { get; set; }

        internal void Method(InternalType typeInstance)
        {
            typeInstance.InternalMethod();
        }
    }
    
    internal class InternalType
    {
        internal void InternalMethod()
        {
        }
    }
}");
            VerifyDiagnostics(test);
        }

        [Fact]
        public void ReferenceToInternalInDifferentAssembly_InternalMethodReferencedWithoutAttribute_Diagnostic()
        {
            var calleeSource = GenerateCalleeHeader(CallerAssemblyName) + @"
    internal class InternalType
    {
        internal void InternalMethod()
        {
        }
    }
}";
            var callee = new AutoTestFile(
                CalleeAssemblyName,
                calleeSource);
            var callerSource = GenerateCallerHeader() + @"
    public class Program
    {
        internal <?>InternalType InternalTypeInstance { get; set; }

        internal void Method(<?>InternalType typeInstance)
        {
            typeInstance.<?>InternalMethod();
        }
    }
}";
            var caller = new AutoTestFile(
                null,
                CallerAssemblyName,
                callerSource,
                new[] { CalleeAssemblyName },
                GetNI1009Rule("NationalInstruments.MyNamespace.InternalType"),
                GetNI1009Rule("NationalInstruments.MyNamespace.InternalType"),
                GetNI1009Rule("NationalInstruments.MyNamespace.InternalType.InternalMethod()"));
            VerifyDiagnostics(new[] { callee, caller });
        }

        [Fact]
        public void ReferenceToInternalInDifferentAssembly_InternalMethodReferencedWithAttribute_NoDiagnostic()
        {
            var calleeSource = GenerateCalleeHeader(CallerAssemblyName) + $@"
    [NationalInstruments.Core.VisibleInternalAttribute]
    internal class InternalType
    {{
        [NationalInstruments.Core.VisibleInternal]
        internal void InternalMethod()
        {{
        }}
    }}
}}";
            var callee = new AutoTestFile(
                CalleeAssemblyName,
                calleeSource);
            var callerSource = GenerateCallerHeader() + @"
    public class Program
    {
        internal InternalType InternalTypeInstance { get; set; }

        internal void Method(InternalType typeInstance)
        {
            typeInstance.InternalMethod();
        }
    }
}";
            var caller = new AutoTestFile(
                null,
                CallerAssemblyName,
                callerSource,
                new[] { CalleeAssemblyName });
            VerifyDiagnostics(new[] { callee, caller });
        }

        /// <summary>
        /// This test does not represent how the analyzer is intended to behave once work is finished. This is a
        /// temporary sanity check that tracks what we have so far.
        /// </summary>
        [Fact]
        public void TemporarySanityCheckReferenceToInternalInDifferentAssembly_InternalMethodReferencedWithAccessToTypeOnly_Diagnostic()
        {
            var calleeSource = GenerateCalleeHeader(CallerAssemblyName) + $@"
    [NationalInstruments.Core.VisibleInternal]
    internal class InternalType
    {{
        internal void InternalMethod()
        {{
        }}
    }}
}}";
            var callee = new AutoTestFile(
                CalleeAssemblyName,
                calleeSource);
            var callerSource = GenerateCallerHeader() + @"
    public class Program
    {
        internal InternalType InternalTypeInstance { get; set; }

        internal void Method(InternalType typeInstance)
        {
            typeInstance.<?>InternalMethod();
        }
    }
}";
            var caller = new AutoTestFile(
                null,
                CallerAssemblyName,
                callerSource,
                new[] { CalleeAssemblyName },
                GetNI1009Rule("NationalInstruments.MyNamespace.InternalType.InternalMethod()"));
            VerifyDiagnostics(new[] { callee, caller });
        }

        private static string GenerateCalleeHeader()
        {
            return GenerateCalleeHeader(string.Empty);
        }

        private static string GenerateCalleeHeader(string callerAssemblyToProvideAccess)
        {
            return UsingSystem + InternalsVisibleToDefinition(callerAssemblyToProvideAccess) + AttributeDefinition + Namespace;
        }

        private static string InternalsVisibleToDefinition(string callerAssemblyToProvideAccess)
        {
            return string.IsNullOrEmpty(callerAssemblyToProvideAccess)
                ? string.Empty
                : $@"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""{callerAssemblyToProvideAccess}"")]";
        }

        private static string GenerateCallerHeader()
        {
            return UsingSystem + Namespace;
        }

        private static Rule GetNI1009Rule(string member) =>
            new Rule(ReferencedInternalMustHaveVisibleInternalAttributeAnalyzer.Rule, member);
    }
}

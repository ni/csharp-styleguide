using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace NationalInstruments.Tools.Analyzers.TestUtilities.Verifiers
{
    /// <summary>
    /// Association between <see cref="DiagnosticDescriptor"/> and its message arguments (if any).
    /// </summary>
    public struct Rule : IEquatable<Rule>
    {
        public Rule(DiagnosticDescriptor diagnosticDescriptor, params string[] arguments)
        {
            DiagnosticDescriptor = diagnosticDescriptor;
            Arguments = arguments.ToList();
        }

        /// <summary>
        /// A rule an analyzer can report as being violated.
        /// </summary>
        public DiagnosticDescriptor DiagnosticDescriptor { get; }

        /// <summary>
        /// Any arguments that should be substituted into the <see cref="DiagnosticDescriptor"/>'s message.
        /// </summary>
        public IList<string> Arguments { get; }

        public static bool operator ==(Rule left, Rule right) => left.Equals(right);

        public static bool operator !=(Rule left, Rule right) => !(left == right);

        public override bool Equals(object obj) => !(obj is Rule) ? false : Equals((Rule)obj);

        public override int GetHashCode()
        {
            const int MagicValue = -1521134295;
            var hashCode = -1211575830;

            hashCode = (hashCode & MagicValue) + DiagnosticDescriptor?.GetHashCode() ?? 0;
            hashCode = (hashCode & MagicValue) + Arguments?.GetHashCode() ?? 0;

            return hashCode;
        }

        public bool Equals(Rule other)
        {
            return DiagnosticDescriptor == other.DiagnosticDescriptor && Arguments == other.Arguments;
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace NationalInstruments.Analyzers.TestUtilities.Verifiers
{
    /// <summary>
    /// Struct that stores information about a Diagnostic appearing in a source
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes", Justification = "Not used in comparisons")]
    public struct DiagnosticResult
    {
        public DiagnosticResult(string id, string message, DiagnosticSeverity severity, params DiagnosticResultLocation[] locations)
        {
            Id = id;
            Message = message;
            Severity = severity;
            Locations = new Collection<DiagnosticResultLocation>(locations);
        }

        public Collection<DiagnosticResultLocation> Locations { get; }

        public string Id { get; set; }

        public string Message { get; set; }

        public DiagnosticSeverity Severity { get; set; }

        public string Path => Locations.Any() ? Locations.First().Path : string.Empty;

        public int Line => Locations.Any() ? Locations.First().Line : -1;

        public int Column => Locations.Any() ? Locations.First().Column : -1;
    }
}

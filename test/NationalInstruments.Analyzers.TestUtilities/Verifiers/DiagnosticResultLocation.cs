using System;

namespace NationalInstruments.Analyzers.TestUtilities.Verifiers
{
    /// <summary>
    /// Location where the diagnostic appears, as determined by path, line number, and column number.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes", Justification = "Not used in comparisons")]
    public struct DiagnosticResultLocation
    {
        public DiagnosticResultLocation(string path)
            : this(path, -1, -1)
        {
        }

        public DiagnosticResultLocation(string path, int line, int column)
        {
            if (line < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(line), "line must be >= -1");
            }

            if (column < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(column), "column must be >= -1");
            }

            Path = path;
            Line = line;
            Column = column;
        }

        public string Path { get; }

        public int Line { get; }

        public int Column { get; }
    }
}

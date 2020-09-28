namespace NationalInstruments.Analyzers.TestUtilities.Markers
{
    /// <summary>
    /// Marker that's created when a token denoting "create a diagnostic for this position"
    /// is encountered in markup.
    /// </summary>
    public class DiagnosticPositionMarker : SourceMarker
    {
        public DiagnosticPositionMarker(int line, int column)
            : base(line, column)
        {
        }
    }
}

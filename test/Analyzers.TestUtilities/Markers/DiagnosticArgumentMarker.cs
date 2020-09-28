namespace NationalInstruments.Tools.Analyzers.TestUtilities.Markers
{
    /// <summary>
    /// Marker that's created when a token denoting "create a diagnostic for this position
    /// with the supplied argument(s)" is encountered in markup.
    /// </summary>
    public class DiagnosticArgumentMarker : SourceMarker
    {
        public DiagnosticArgumentMarker(int line, int column)
            : base(line, column)
        {
        }
    }
}

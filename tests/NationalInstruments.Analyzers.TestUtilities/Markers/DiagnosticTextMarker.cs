namespace NationalInstruments.Analyzers.TestUtilities.Markers
{
    /// <summary>
    /// Marker that's created when tokens denoting "create a diagnostic for this position
    /// with the captured text" is encountered in markup.
    /// </summary>
    public class DiagnosticTextMarker : SourceMarker
    {
        public DiagnosticTextMarker(int line, int column, string text)
            : base(line, column)
        {
            Text = text;
        }

        /// <summary>
        /// The source code enclosed by <see cref="TestMarkup.DiagnosticTextStartSyntax"/> and
        /// <see cref="TestMarkup.DiagnosticTextEndSyntax"/>.
        /// </summary>
        public string Text { get; }
    }
}

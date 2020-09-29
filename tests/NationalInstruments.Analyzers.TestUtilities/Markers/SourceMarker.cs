namespace NationalInstruments.Analyzers.TestUtilities.Markers
{
    /// <summary>
    /// Base class for all markers.
    /// </summary>
    public abstract class SourceMarker
    {
        protected SourceMarker(int line, int column)
        {
            Line = line;
            Column = column;
        }

        /// <summary>
        /// Line where diagnostic should occur.
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// Column where diagnostic should occur.
        /// </summary>
        public int Column { get; }
    }
}

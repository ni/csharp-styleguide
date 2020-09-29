using System;
using System.Collections.Generic;
using NationalInstruments.Analyzers.TestUtilities.Markers;

namespace NationalInstruments.Analyzers.TestUtilities.UnitTests.Assets
{
    /// <summary>
    /// Convenient grouping of markup and the markers expected to be extracted from it.
    /// </summary>
    public struct Test : IEquatable<Test>
    {
        /// <summary>
        /// Constructor that provides a way to quickly initialize the struct's properties.
        /// </summary>
        /// <param name="markup">Source code with markers.</param>
        /// <param name="expectedMarkers">Markers expected to be extracted from the <paramref name="markup"/>.</param>
        public Test(string markup, params SourceMarker[] expectedMarkers)
        {
            Markup = markup;

            ExpectedMarkers = expectedMarkers;
        }

        /// <summary>
        /// Source code with markers.
        /// </summary>
        public string Markup { get; }

        /// <summary>
        /// Markers expected to be extracted from the <see cref="Markup"/>.
        /// </summary>
        public IList<SourceMarker> ExpectedMarkers { get; }

        public static bool operator ==(Test left, Test right) => left.Equals(right);

        public static bool operator !=(Test left, Test right) => !(left == right);

        public override bool Equals(object obj)
        {
            if (!(obj is Test))
            {
                return false;
            }

            return Equals((Test)obj);
        }

        public override int GetHashCode()
        {
            const int MagicValue = -1521134295;
            var hashCode = -1211575830;

            hashCode = (hashCode * MagicValue) + Markup?.GetHashCode() ?? 0;
            hashCode = (hashCode * MagicValue) + ExpectedMarkers?.GetHashCode() ?? 0;

            return hashCode;
        }

        public bool Equals(Test other)
        {
            return Markup == other.Markup && ExpectedMarkers == other.ExpectedMarkers;
        }
    }
}

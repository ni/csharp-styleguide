using System;
using System.Collections.Generic;
using System.Linq;

namespace NationalInstruments.Tools
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "CircularList doesn't actually have anything to dispose")]
    public class SimpleMovingAverage
    {
        public SimpleMovingAverage(int maxNumberOfSamples)
        {
            if (maxNumberOfSamples <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxNumberOfSamples), "numberOfSamples can't be negative or 0.");
            }

            MaxNumberOfSamples = maxNumberOfSamples;
            Samples = new LinkedList<double>();
        }

        public LinkedList<double> Samples { get; }

        public int MaxNumberOfSamples { get; }

        public double Average => Samples.Average();

        public void AddSample(double value)
        {
            if (Samples.Count == MaxNumberOfSamples)
            {
                Samples.RemoveFirst();
            }

            Samples.AddLast(value);
        }

        public void ClearSamples()
        {
            Samples.Clear();
        }
    }
}

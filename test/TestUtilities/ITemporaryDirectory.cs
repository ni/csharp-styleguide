using System;

namespace NationalInstruments.Tools.TestUtilities
{
    public interface ITemporaryDirectory : IDisposable
    {
        string FullPath { get; }
    }
}

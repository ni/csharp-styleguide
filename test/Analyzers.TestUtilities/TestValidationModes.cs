using System;

namespace NationalInstruments.Tools.Analyzers.TestUtilities
{
    [Flags]
    public enum TestValidationModes
    {
        None = 0,
        ValidateWarnings = 1,
        ValidateErrors = 2,
    }
}

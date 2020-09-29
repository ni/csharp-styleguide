using System;

namespace NationalInstruments.Analyzers.TestUtilities
{
    [Flags]
    public enum TestValidationModes
    {
        None = 0,
        ValidateWarnings = 1,
        ValidateErrors = 2,
    }
}

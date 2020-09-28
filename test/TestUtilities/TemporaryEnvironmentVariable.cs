using System;

namespace NationalInstruments.Tools.TestUtilities
{
    public class TemporaryEnvironmentVariable : Disposable
    {
        private readonly string _name;

        private readonly string _originalValue;

        public TemporaryEnvironmentVariable(string name, string replacement)
        {
            _name = name;
            _originalValue = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, replacement);
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();
            Environment.SetEnvironmentVariable(_name, _originalValue);
        }
    }
}

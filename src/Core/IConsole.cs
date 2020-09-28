using System;

namespace NationalInstruments.Tools
{
    public interface IConsole
    {
        bool KeyAvailable { get; }

        void WriteLine(string v);

        ConsoleKeyInfo ReadKey(bool v);
    }
}

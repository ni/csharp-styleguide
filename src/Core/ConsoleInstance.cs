using System;

namespace NationalInstruments.Tools
{
    internal class ConsoleInstance : IConsole
    {
        public bool KeyAvailable => Console.KeyAvailable;

        public ConsoleKeyInfo ReadKey(bool intercept)
        {
            return Console.ReadKey(intercept);
        }

        public void WriteLine(string value)
        {
            Console.WriteLine(value);
        }
    }
}

using System.IO;
using System.Linq;

namespace NationalInstruments.Tools.Core
{
    public static partial class DirectoryHelper
    {
        public static bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }
    }
}

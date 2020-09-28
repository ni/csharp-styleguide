using System;
using System.IO;

namespace NationalInstruments.Tools.Core
{
    public static partial class DirectoryHelper
    {
        /// <summary>
        /// try to delete the directory.  Don't allow an exception.
        /// Examples: file in use, directory not found, etc.
        /// </summary>
        /// <param name="path">The director to delete</param>
        /// <param name="recursive">Whether or not to delete recursively</param>
        /// <returns>false if exception was thrown and eaten, true otherwise</returns>
        public static bool TryDeleteDirectory(string path, bool recursive = false)
        {
            try
            {
                Directory.Delete(path, recursive);
                return true;
            }
            catch (Exception)
            {
                // do nothing - just eat it.
                return false;
            }
        }
    }
}

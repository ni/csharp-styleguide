using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NationalInstruments.Tools.Core;

namespace NationalInstruments.Tools.TestUtilities
{
    public static class DeploymentHelper
    {
        public static async Task<ITemporaryDirectory> GetTempCopyAsync(string source)
        {
            var temporaryDirectory = new TemporaryDirectory();
            await DirectoryHelper.CopyFastAsync(new DirectoryInfo(Path.GetFullPath(source)), new DirectoryInfo(temporaryDirectory.FullPath), true, CancellationToken.None).ConfigureAwait(false);
            return temporaryDirectory;
        }

        public static string TestAssetsDirectory(params string[] relativePath)
        {
            var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
            var dirPath = Path.GetDirectoryName(codeBasePath);

            if (relativePath == null || relativePath.Length == 0)
            {
                return dirPath;
            }

            var paths = new List<string>(relativePath);
            paths.Insert(0, dirPath);

            return Path.Combine(paths.ToArray());
        }
    }
}

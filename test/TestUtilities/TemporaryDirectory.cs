using System.IO;

namespace NationalInstruments.Tools.TestUtilities
{
    public class TemporaryDirectory : Disposable, ITemporaryDirectory
    {
        public TemporaryDirectory()
        {
            FullPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;
        }

        public string FullPath { get; }

        protected override void DisposeManagedResources()
        {
            foreach (var path in Directory.GetFiles(FullPath, "*.*", SearchOption.AllDirectories))
            {
                File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.ReadOnly);
            }

            Directory.Delete(FullPath, recursive: true);
        }
    }
}

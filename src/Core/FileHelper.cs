using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NationalInstruments.Tools.Core.Extensions;

namespace NationalInstruments.Tools.Core
{
    public static class FileHelper
    {
        public const long HashSizeLimit = 1024 * 1024 * 1024L;
        private const int DefaultBlockSize = 4096;

        /// <summary>
        /// try to delete the file.  If we fail, ignore result.  Don't allow an exception.
        /// Examples: file in use, directory not found, etc.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>false if exception was thrown and eaten, true otherwise</returns>
        public static bool TryDeleteFile(string path)
        {
            try
            {
                File.Delete(path);
                return true;
            }
            catch (Exception)
            {
                // do nothing - just eat it.
                return false;
            }
        }

        public static string GetHash(string filePath, long fileSize)
        {
            if (fileSize < HashSizeLimit)
            {
                return GetHash(filePath);
            }

            using (var ms = new MemoryStream())
            {
                var fileSizeBytes = BitConverter.GetBytes(fileSize);
                ms.Write(fileSizeBytes, 0, fileSizeBytes.Length);

                var lastWrite = BitConverter.GetBytes(File.GetLastWriteTime(filePath).Ticks);
                ms.Write(lastWrite, 0, lastWrite.Length);

                var fileName = Path.GetFileName(filePath);
                ms.AppendEncodedStringToStream(fileName, Encoding.UTF8);
                ms.Position = 0;

                return GetHash(ms) + "Q";
            }
        }

        public static string GetHash(string filePath)
        {
            using (var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return GetHash(fs);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms", Justification = "This was migrated from LV.Tools. Keeping as is.")]
        public static string GetHash(Stream stream, int blockSize = DefaultBlockSize)
        {
            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                var buffer = new byte[blockSize];
                while (true)
                {
                    var bytesRead = stream.Read(buffer, 0, blockSize);

                    if (bytesRead != 0)
                    {
                        // process this chunk
                        var offset = sha1.TransformBlock(buffer, 0, bytesRead, null, 0);
                        if (offset != bytesRead)
                        {
                            throw new NotImplementedException("API Error calculating hash: " + offset + ", " + bytesRead);
                        }
                    }

                    if (bytesRead != blockSize)
                    {
                        sha1.TransformFinalBlock(buffer, 0, 0);
                        break;
                    }
                }

                return sha1.Hash.ToBase32String();
            }
        }
    }
}

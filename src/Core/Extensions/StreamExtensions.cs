using System.IO;
using System.Text;

namespace NationalInstruments.Tools.Core.Extensions
{
    public static class StreamExtensions
    {
        public static void AppendEncodedStringToStream(this Stream stream, string file, Encoding encoding)
        {
            var documentBytes = encoding.GetBytes(file);
            stream.Write(documentBytes, 0, documentBytes.Length);

            // call SetLength, this will cause the file to truncate.
            stream.SetLength(stream.Position);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace NationalInstruments.Tools
{
    public static class PlaylistFileHelper
    {
        public static IEnumerable<string> GetTestsFromPlaylistFile(string playlistPath, ILogger logger = null)
        {
            logger?.LogInformation($"Loading Playlist from path: {playlistPath}");

            if (File.Exists(playlistPath))
            {
                try
                {
                    var attribute = string.Equals(Path.GetExtension(playlistPath), ".tlx", StringComparison.OrdinalIgnoreCase) ? "name" : "Test";
                    var doc = XDocument.Parse(File.ReadAllText(playlistPath));
                    var result = from e in doc.Root.Elements()
                                 select e.Attribute(attribute).Value;
                    return result.ToList();
                }
                catch (XmlException ex)
                {
                    logger?.LogError(ex.ToString());
                    return new List<string>();
                }
            }

            logger?.LogWarning($"The Playlist file \"{playlistPath}\" does not exist");
            return new List<string>();
        }

        public static void WritePlaylistFile(string playlistPath, IEnumerable<string> tests)
        {
            var directory = Path.GetDirectoryName(playlistPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var writer = new XmlTextWriter(playlistPath, Encoding.UTF8))
            {
                writer.Formatting = Formatting.Indented;
                writer.Indentation = 4;

                writer.WriteStartDocument();
                writer.WriteStartElement("Playlist");
                writer.WriteAttributeString("version", "1.0");

                foreach (var test in tests)
                {
                    writer.WriteStartElement("Add");
                    writer.WriteAttributeString("Test", test);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }
        }
    }
}

using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace NationalInstruments.Tools
{
    /// <summary>
    /// Abstract base class for versioned serializable objects that do not have a previous version.
    ///
    /// See <see cref="IVersionedSerializable"/> for full documentation.
    /// </summary>
    public abstract class InitialVersionedSerializable : IVersionedSerializable
    {
        public uint SerializationVersion { get; } = 1;

        public void InitializeFrom(MemoryStream stream, uint dataVersion)
        {
            if (dataVersion != 0 && dataVersion != SerializationVersion)
            {
                throw new SerializationException("Cannot deserialize object, as it has a different version!");
            }

            using (var jsonReader = new BsonDataReader(stream))
            {
                var serializer = new JsonSerializer();
                serializer.Populate(jsonReader, this);
            }
        }
    }
}

using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace NationalInstruments.Tools
{
    /// <summary>
    /// Abstract base class for versioned serializable objects that have a previous version.
    ///
    /// See <see cref="IVersionedSerializable"/> for full documentation.
    /// </summary>
    /// <typeparam name="TPrevious">The previous version of the class.</typeparam>
    public abstract class VersionedSerializable<TPrevious> : IVersionedSerializable<TPrevious>
        where TPrevious : IVersionedSerializable, new()
    {
        public abstract uint SerializationVersion { get; }

        public void InitializeFrom(MemoryStream stream, uint dataVersion)
        {
            if (dataVersion > SerializationVersion)
            {
                throw new SerializationException("Cannot deserialize a newer version of this object!");
            }

            if (dataVersion < SerializationVersion)
            {
                var previous = new TPrevious();
                previous.InitializeFrom(stream, dataVersion);

                InitializeFrom(previous);
                return;
            }

            using (var jsonReader = new BsonDataReader(stream))
            {
                var serializer = new JsonSerializer();
                serializer.Populate(jsonReader, this);
            }
        }

        public abstract void InitializeFrom(TPrevious previous);
    }
}

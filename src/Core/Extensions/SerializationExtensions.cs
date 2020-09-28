using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace NationalInstruments.Tools.Extensions
{
    public static class SerializationExtensions
    {
        private const int MagicLength = sizeof(uint) + sizeof(byte); // IVersionedSerializable.SerializationVersion + MagicByte
        private const byte MagicByte = 0xAA; // Something that will not be a valid BSON object type (see https://docs.mongodb.com/manual/reference/bson-types) so we can distinguish versioned and unversioned BSON.

        public static byte[] SerializeToBson<T>(this T argument)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BsonDataWriter(stream))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(writer, new DataHolder<T> { Value = argument });
                return stream.GetBuffer();
            }
        }

        public static T DeserializeBson<T>(this byte[] serializedRepresentation)
        {
            using (var stream = new MemoryStream(serializedRepresentation))
            using (var reader = new BsonDataReader(stream))
            {
                var serializer = new JsonSerializer();
                return serializer.Deserialize<DataHolder<T>>(reader).Value;
            }
        }

        public static string SerializeToJson<T>(this T argument)
        {
            using (var stringWriter = new StringWriter())
            using (var writer = new JsonTextWriter(stringWriter))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(writer, argument);
                return stringWriter.GetStringBuilder().ToString();
            }
        }

        public static byte[] SerializeToJsonBytes<T>(this T argument)
        {
            return Encoding.UTF8.GetBytes(argument.SerializeToJson());
        }

        public static T DeserializeJson<T>(this string jsonString)
        {
            using (var stringReader = new StringReader(jsonString))
            using (var reader = new JsonTextReader(stringReader))
            {
                var serializer = new JsonSerializer();
                return serializer.Deserialize<T>(reader);
            }
        }

        public static T DeserializeJson<T>(this byte[] data)
        {
            return Encoding.UTF8.GetString(data).DeserializeJson<T>();
        }

        /// <summary>
        /// Deserialize a versioned serializable from a byte array (either BSON or BSON with version information prefixed)
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize. Must be either a newer version of the serialized data or the same version.</typeparam>
        /// <param name="data">The serialized data to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        public static T DeserializeVersionedBson<T>(this byte[] data)
            where T : IVersionedSerializable, new()
        {
            var dataVersion = GetVersionInformation(data);

            using (var stream = GetOffsetStream(data, dataVersion))
            {
                var deserialized = new T();
                deserialized.InitializeFrom(stream, dataVersion);
                return deserialized;
            }
        }

        /// <summary>
        /// Serialize a versioned serializable to a byte array (BSON with version information prefixed).
        /// </summary>
        /// <typeparam name="T">The type of object to serialize.</typeparam>
        /// <param name="arg">The object to serialize.</param>
        /// <returns>The serialized object.</returns>
        public static byte[] SerializeToVersionedBson<T>(this T arg)
            where T : IVersionedSerializable
        {
            using (var stream = new MemoryStream())
            using (var writer = new BsonDataWriter(stream))
            {
                AddVersionInformation(stream, arg.SerializationVersion);

                var serializer = new JsonSerializer();
                serializer.Serialize(writer, arg);
                return stream.GetBuffer();
            }
        }

        private static uint GetVersionInformation(byte[] data)
        {
            if (data.Length < MagicLength)
            {
                return 0;
            }

            // In unversioned data, this byte is a BSON object type (https://docs.mongodb.com/manual/reference/bson-types/),
            // and the first 4 bytes are the document size. If we see our magic byte instead, we have versioned data,
            // and the first 4 bytes are the version.
            if (data[4] != MagicByte)
            {
                return 0;
            }

            var version = (uint)data[0] << 24 | (uint)data[1] << 16 | (uint)data[2] << 8 | data[3];

            return version;
        }

        private static MemoryStream GetOffsetStream(byte[] data, uint dataVersion)
        {
            // Version 0 is raw data, so we don't have an offset.
            // Other versions have extra data, so we need to skip to the BSON data.
            var offset = dataVersion == 0 ? 0 : MagicLength;
            var length = data.Length - offset;

            return new MemoryStream(data, offset, length);
        }

        private static void AddVersionInformation(Stream stream, uint version)
        {
            var bytes = new[]
            {
                (byte)(version >> 24),
                (byte)((version >> 16) & 0xFF),
                (byte)((version >> 8) & 0xFF),
                (byte)(version & 0xFF), MagicByte,
            };

            stream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Helper class for BSON serialization.
        /// </summary>
        /// <remarks>
        /// The BSON specification requires that all data be in a mapping
        /// (see http://bsonspec.org/spec.html), which most reference types
        /// have (property/field => value). Value types and strings do not
        /// have such a mapping, so we need to wrap them with something in
        /// order to provide it, which is what this class is for.
        /// </remarks>
        /// <typeparam name="T">The type of data to hold.</typeparam>
        private struct DataHolder<T>
        {
            public T Value;
        }
    }
}

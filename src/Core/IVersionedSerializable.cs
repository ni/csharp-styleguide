using System.IO;
using NationalInstruments.Tools.Extensions;
using Newtonsoft.Json;

namespace NationalInstruments.Tools
{
    /// <summary>
    /// A serializable object that has a version associated with it.
    /// </summary>
    /// <remarks>
    /// These objects are serialized to BSON, with 5 additional bytes before the BSON starts.
    /// The first 4 bytes are the version, and the last is a magic byte (<see cref="SerializationExtensions.MagicByte"/>)
    /// so the versioned data can be distinguished from raw BSON (which also starts with 4-byte number).
    ///
    /// Data that lacks version information is deserialized directly to version 1 of the object. Otherwise,
    /// it is deserialized to that version and mutated forward to the version requested. The first version
    /// of the object should inherit from <see cref="InitialVersionedSerializable"/>, and should be
    /// deserializable directly from unversioned data if there has been a published unversioned instance.
    /// Later versions should inherit from <see cref="VersionedSerializable{TPrevious}"/>, with the immediate
    /// previous version. e.g.
    ///     class V1 : InitialVersionedSerializable
    ///     class V2 : VersionedSerializable{V1}
    ///     class V3 : VersionedSerializable{V2}
    ///
    /// Generally, <see cref="InitializeFrom"/> should not be called directly outside of tests of the mutation code.
    /// Use <see cref="SerializationExtensions.DeserializeVersionedBson{T}"/> and <see cref="SerializationExtensions.SerializeToVersionedBson{T}"/> instead.
    /// </remarks>
    public interface IVersionedSerializable
    {
        [JsonIgnore]
        uint SerializationVersion { get; }

        void InitializeFrom(MemoryStream stream, uint dataVersion);
    }

    /// <summary>
    /// A serializable object that has a version and a previous version associated with it.
    /// </summary>
    /// <typeparam name="TPrevious">The previous version type.</typeparam>
    public interface IVersionedSerializable<in TPrevious> : IVersionedSerializable
        where TPrevious : IVersionedSerializable
    {
        void InitializeFrom(TPrevious previous);
    }
}

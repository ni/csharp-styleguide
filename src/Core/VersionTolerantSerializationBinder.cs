using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace NationalInstruments.Tools
{
    /// <summary>
    /// Serialization binder that works with message serialization to allow us to serialize the same type from a different version of the same assembly.
    /// </summary>
    public sealed class VersionTolerantSerializationBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            var fullName = typeName + ", " + assemblyName;
            var parsedTypeName = new TypeName(fullName);
            EnsureAssembliesFromCurrentDomain(parsedTypeName);
            return Type.GetType(parsedTypeName.ToString());
        }

        private static void EnsureAssembliesFromCurrentDomain(TypeName type)
        {
            EnsureAssemblyFromCurrentDomain(type);

            if (type.GenericParameters != null)
            {
                foreach (var genericType in type.GenericParameters)
                {
                    EnsureAssembliesFromCurrentDomain(genericType);
                }
            }
        }

        private static void EnsureAssemblyFromCurrentDomain(TypeName type)
        {
            if (string.IsNullOrEmpty(type.Assembly))
            {
                return;
            }

            // Get a loaded assembly with the same name but (potentially) different version
            var loadingAssemblyName = new AssemblyName(type.Assembly);
            var sameAssembly =
                AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(x => x.GetName().Name == loadingAssemblyName.Name);
            if (sameAssembly == null)
            {
                return;
            }

            type.Assembly = sameAssembly.FullName;
        }
    }
}

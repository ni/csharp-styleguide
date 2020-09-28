using System;
using Microsoft.Extensions.Configuration;

namespace NationalInstruments.Tools.Extensions
{
    public static class ConfigurationExtensions
    {
        public static string GetValueThrowIfNotDefined(this IConfiguration configuration, string key)
        {
            var value = configuration.GetValue<string>(key);
            if (value == null)
            {
                throw new InvalidOperationException($"Configuration Key [{key}] not found.");
            }

            return value;
        }

        public static T GetValueThrowIfNotDefined<T>(this IConfiguration configuration, string key)
        {
            var value = (T)configuration.GetValue(typeof(T), key);
            if (value == null)
            {
                throw new InvalidOperationException($"Configuration Key [{key}] not found.");
            }

            return value;
        }
    }
}

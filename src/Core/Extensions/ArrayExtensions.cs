using System;
using System.Globalization;
using System.Linq;
using NationalInstruments.Tools.Core;

namespace NationalInstruments.Tools.Extensions
{
    public static class ArrayExtensions
    {
        public static string HexString(this byte[] data)
        {
            return data.Aggregate(string.Empty, (current, b) => current + b.ToString("X2", CultureInfo.InvariantCulture));
        }

        public static T[] Insert<T>(this T[] array, int location, T element)
        {
            array.VerifyArgumentIsNotNull(nameof(array));

            var newArray = new T[array.Length + 1];
            var increase = 0;

            for (var i = 0; i < array.Length; i++)
            {
                if (i == location)
                {
                    newArray[i] = element;
                    increase = 1;
                }

                newArray[i + increase] = array[i];
            }

            return newArray;
        }

        public static T[] Add<T>(this T[] array, T element)
        {
            array.VerifyArgumentIsNotNull(nameof(array));

            var length = array.Length;
            Array.Resize(ref array, length + 1);
            array[length] = element;
            return array;
        }

        public static T[] Remove<T>(this T[] array, T element)
        {
            array.VerifyArgumentIsNotNull(nameof(array));

            var position = Array.FindIndex(array, match => match.Equals(element));
            if (position < 0)
            {
                return array;
            }

            var length = array.Length - 1;
            var newArray = new T[length];
            Array.Copy(array, 0, newArray, 0, position);
            Array.Copy(array, position + 1, newArray, position, length - position);
            return newArray;
        }

        public static bool ArrayAreEqual<T>(T[] byteArray1, T[] byteArray2)
        {
            return byteArray1.Length == byteArray2.Length && byteArray1.SequenceEqual(byteArray2);
        }

        /// <summary>
        /// Initializes a new array of a given type and length.
        /// </summary>
        /// <typeparam name="T">The type of the array</typeparam>
        /// <param name="length">The length of the array.</param>
        /// <returns>An array of the given length with instantiated <typeparamref name="T"/>.</returns>
        public static T[] InitializeArray<T>(int length)
            where T : new()
        {
            var array = new T[length];

            for (var i = 0; i < length; ++i)
            {
                array[i] = new T();
            }

            return array;
        }
    }
}

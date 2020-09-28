using System;

namespace NationalInstruments.Tools.Core
{
    /// <summary>
    /// Functions for converting from byte array to/from base32.
    /// This base32 uses {A-Z, 2-7} for the 32 possible values.
    /// 2-7 are nice (as opposed to 0-5) because 0 looks like O and 1 looks like I or L.  2-7 are more distinct in case someone ends up typing them.
    /// Base32 is nice for case-insensitive file systems (like windows) because using {A-Z (26) and (0-9)} we get 36 valid, unique characters
    /// We can't easily get to 64 with valid file characters and case insensitivity.
    /// Base64 is problematic because A and a have different values, but on case insensitive systems, they have the same value 'A'=='a', so we lose precision.
    /// </summary>
    public static class Base32EncodingHelper
    {
        /// <summary>
        /// convert base 32 input to array of bytes
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static byte[] FromBase32StringToBytes(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentNullException(nameof(input));
            }

            input = input.TrimEnd('='); // remove padding characters
            var byteCount = input.Length * 5 / 8; // this must be TRUNCATED
            var returnArray = new byte[byteCount];

            byte curByte = 0, bitsRemaining = 8;
            int mask = 0, arrayIndex = 0;

            foreach (var c in input)
            {
                var value = CharToValue(c);

                if (bitsRemaining > 5)
                {
                    mask = value << (bitsRemaining - 5);
                    curByte = (byte)(curByte | mask);
                    bitsRemaining -= 5;
                }
                else
                {
                    mask = value >> (5 - bitsRemaining);
                    curByte = (byte)(curByte | mask);
                    returnArray[arrayIndex++] = curByte;
                    curByte = (byte)(value << (3 + bitsRemaining));
                    bitsRemaining += 3;
                }
            }

            // if we didn't end with a full byte
            if (arrayIndex != byteCount)
            {
                returnArray[arrayIndex] = curByte;
            }

            return returnArray;
        }

        /// <summary>
        /// convert byte array to base32 string
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ToBase32String(this byte[] input)
        {
            if (input == null || input.Length == 0)
            {
                throw new ArgumentNullException(nameof(input));
            }

            var charCount = (int)Math.Ceiling(input.Length / 5d) * 8;
            charCount = (int)Math.Ceiling(input.Length / 5d * 8);
            var returnArray = new char[charCount];

            byte nextChar = 0, bitsRemaining = 5;
            var arrayIndex = 0;

            foreach (var b in input)
            {
                nextChar = (byte)(nextChar | (b >> (8 - bitsRemaining)));
                returnArray[arrayIndex++] = ValueToChar(nextChar);

                if (bitsRemaining < 4)
                {
                    nextChar = (byte)((b >> (3 - bitsRemaining)) & 31);
                    returnArray[arrayIndex++] = ValueToChar(nextChar);
                    bitsRemaining += 5;
                }

                bitsRemaining -= 3;
                nextChar = (byte)((b << bitsRemaining) & 31);
            }

            // if we didn't end with a full char
            if (arrayIndex != charCount)
            {
                returnArray[arrayIndex++] = ValueToChar(nextChar);
                while (arrayIndex != charCount)
                {
                    returnArray[arrayIndex++] = '='; // padding
                }
            }

            return new string(returnArray);
        }

        /// <summary>
        /// base 32 values are case-insensitive
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static bool Base32Equals(this string first, string second)
        {
            return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
        }

        private static int CharToValue(char c)
        {
            var value = (int)c;

            // 65-90 == uppercase letters
            if (value < 91 && value > 64)
            {
                return value - 65;
            }

            // 52-57 == numbers 2-7
            if (value < 56 && value > 49)
            {
                return value - 24;
            }

            // 97-122 == lowercase letters
            if (value < 123 && value > 96)
            {
                return value - 97;
            }

            throw new ArgumentException("Character is not a Base32 character.", nameof(c));
        }

        private static char ValueToChar(byte b)
        {
            // starting with 'A' (65)
            if (b < 26)
            {
                return (char)(b + 65);
            }

            // starting with '2' (50)
            if (b < 32)
            {
                return (char)(b + 24);
            }

            throw new ArgumentException("Byte is not a value Base32 value.", nameof(b));
        }
    }
}

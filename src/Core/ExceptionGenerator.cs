using System;
using System.Globalization;
using NationalInstruments.Tools.Core;

namespace NationalInstruments.Tools
{
    /// <summary>
    /// Class used to contain methods for generating exceptions.
    /// </summary>
    public static class ExceptionGenerator
    {
        /// <summary>
        /// Generates a ArgumentNullException
        /// </summary>
        /// <param name="valueName">Name of the argument</param>
        /// <returns>Exception</returns>
        public static Exception ArgumentNull(string valueName)
        {
            valueName.VerifyArgumentIsNotNull(nameof(valueName));

            return new ArgumentNullException(valueName);
        }

        /// <summary>
        /// Generates a ArgumentNullException
        /// </summary>
        /// <param name="valueName">Name of the argument</param>
        /// <param name="message">Exception message</param>
        /// <returns>Exception</returns>
        public static Exception ArgumentNull(string valueName, string message)
        {
            valueName.VerifyArgumentIsNotNull(nameof(valueName));

            return new ArgumentNullException(valueName, message);
        }

        /// <summary>
        /// Generates a ArgumentException
        /// </summary>
        /// <param name="valueName">Name of the argument</param>
        /// <returns>Exception</returns>
        public static Exception Argument(string valueName)
        {
            valueName.VerifyArgumentIsNotNull(nameof(valueName));

            return new ArgumentException(valueName);
        }

        /// <summary>
        /// Generates a ArgumentException
        /// </summary>
        /// <param name="valueName">Name of the argument</param>
        /// <param name="message">Exception message</param>
        /// <returns>Exception</returns>
        public static Exception Argument(string valueName, string message)
        {
            valueName.VerifyArgumentIsNotNull(nameof(valueName));
            message.VerifyArgumentIsNotNullOrEmpty(nameof(message));

            return new ArgumentException(message, valueName);
        }

        /// <summary>
        /// Generates a ArgumentOutOfRangeException
        /// </summary>
        /// <param name="valueName">Name of the argument</param>
        /// <returns>Exception</returns>
        public static Exception ArgumentOutOfRange(string valueName)
        {
            valueName.VerifyArgumentIsNotNull(nameof(valueName));

            return new ArgumentOutOfRangeException(valueName);
        }

        /// <summary>
        /// Generates a ArgumentOutOfRangeException
        /// </summary>
        /// <param name="valueName">Name of the argument</param>
        /// <param name="message">Exception message</param>
        /// <returns>Exception</returns>
        public static Exception ArgumentOutOfRange(string valueName, string message)
        {
            valueName.VerifyArgumentIsNotNull(nameof(valueName));
            message.VerifyArgumentIsNotNullOrEmpty(nameof(message));

            return new ArgumentOutOfRangeException(valueName, message);
        }

        /// <summary>
        /// Generates a InvalidOperationException
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <returns>Exception</returns>
        public static Exception InvalidOperation(string message)
        {
            message.VerifyArgumentIsNotNullOrEmpty(nameof(message));

            return new InvalidOperationException(message);
        }

        public static Exception InvalidOperation(string valueName, string message)
        {
            valueName.VerifyArgumentIsNotNull(nameof(valueName));
            message.VerifyArgumentIsNotNullOrEmpty(nameof(message));

            return new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, message, valueName));
        }
    }
}

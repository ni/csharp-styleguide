using System;

namespace NationalInstruments.Tools.Analyzers.Correctness.StringsShouldBeInResources
{
    /// <summary>
    /// Exception that's thrown if an NI1004 attribute needs a <c>Target</c> property defined but it's either missing or null.
    /// </summary>
    public sealed class AttributeMissingTargetException : Exception
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public AttributeMissingTargetException()
        {
        }

        /// <summary>
        /// Constructor that accepts a message.
        /// </summary>
        /// <param name="message">Message to give the exception.</param>
        public AttributeMissingTargetException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Constructor that accepts a message and an inner exception.
        /// </summary>
        /// <param name="message">Message to give the exception.</param>
        /// <param name="innerException">Exception that occurred before this exception.</param>
        public AttributeMissingTargetException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Constructor that accepts a message, attribute name, and scope name.
        /// </summary>
        /// <param name="message">Message to give the exception.</param>
        /// <param name="attributeName">Name of the analyzer attribute missing a target value.</param>
        /// <param name="scopeName">Scope value of the analyzer attribute.</param>
        public AttributeMissingTargetException(string message, string attributeName, string scopeName)
            : base(message)
        {
            AttributeName = attributeName;
            ScopeName = scopeName;
        }

        /// <summary>
        /// Name of the attribute missing a target value.
        /// </summary>
        public string AttributeName { get; private set; }

        /// <summary>
        /// Name of the attribute's scope.
        /// </summary>
        public string ScopeName { get; private set; }
    }
}

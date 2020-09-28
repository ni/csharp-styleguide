using System;
using System.Globalization;

namespace NationalInstruments.Tools.Core
{
    public static class Verifier
    {
        public static void Verify<T>(this T value, Func<T, bool> checker, Func<string, string, Exception> generateException, string valueName = null, string message = null)
        {
            if (!checker(value))
            {
                throw generateException(valueName, message);
            }
        }

        public static void Verify(Func<bool> checker, Func<Exception> generateException)
        {
            if (!checker())
            {
                throw generateException();
            }
        }

        public static void VerifyArgumentIsNotNull<T>(this T item, string parameterName)
            where T : class
        {
            item.Verify(IsNotNull, ExceptionGenerator.ArgumentNull, parameterName);
        }

        public static void VerifyValueIsNotNull<T>(this T item, string parameterName)
            where T : class
        {
            item.Verify(IsNotNull, ExceptionGenerator.InvalidOperation, parameterName, Resources.Verifier_VerifyValueIsNotNull_Expected_non_null_variable__0__is_null);
        }

        public static void VerifyArgumentIsNotNullOrEmpty(this string item, string parameterName)
        {
            if (string.IsNullOrEmpty(item))
            {
                item.Verify(IsNotNull, ExceptionGenerator.ArgumentNull, parameterName);
                item.Verify(IsNotNullOrEmpty, ExceptionGenerator.Argument, parameterName, string.Format(CultureInfo.InvariantCulture, Resources.Verifier_VerifyArgumentIsNotNullOrEmpty_The_value__0__should_not_be_Null_or_Empty, parameterName));
            }
        }

        public static void VerifyValueIsNotNullOrEmpty(this string item, string parameterName)
        {
            if (string.IsNullOrEmpty(item))
            {
                item.Verify(IsNotNull, ExceptionGenerator.InvalidOperation, parameterName, Resources.Verifier_VerifyValueIsNotNullOrEmpty_The_value__0__should_not_be_Null);
                item.Verify(IsNotNullOrEmpty, ExceptionGenerator.InvalidOperation, parameterName, Resources.Verifier_VerifyValueIsNotNullOrEmpty_The_value__0__should_not_be_Empty);
            }
        }

        public static bool IsNotNull<T>(T value)
        {
            return !Equals(value, default(T));
        }

        public static bool IsNotNullOrEmpty(string value)
        {
            return !string.IsNullOrEmpty(value);
        }

        public static bool IsOfReferenceType<T>(object value)
            where T : class
        {
            return value is T;
        }

        public static bool IsOfValueType<T>(object value)
        {
            return value is T;
        }

        public static bool IsValidEnumValue(Enum enumValue)
        {
            var type = enumValue.GetType();
            return Enum.IsDefined(type, enumValue);
        }
    }
}

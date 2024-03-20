using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using NationalInstruments.Analyzers.Utilities.Extensions;

namespace NationalInstruments.Analyzers.Correctness.StringsShouldBeInResources
{
    /// <summary>
    /// While not an actual attribute that decorates source, this object is created for each "exemption" attribute
    /// discovered in source. It exposes the common properties that all exemption attributes can contain: name,
    /// scope, target, and literal.
    /// </summary>
    /// <remarks>
    /// An attribute is considered an "exemption" attribute if it's name is one of the following:
    ///
    /// - ExemptFromStringLiteralsRule
    /// - AllowThisNonLocalizedLiteral
    /// - AcceptsStringLiteralArguments
    /// - ImplementationAllowedToUseStringLiterals
    /// </remarks>
    ///
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Justification = "Naming is correct in this case.")]
    public class ExemptionAttribute
    {
        private readonly AttributeData _attributeData;

        public ExemptionAttribute(AttributeData attributeData)
        {
            _attributeData = attributeData;

            if (!Enum.TryParse(GetNamedArgumentValueOrDefault("Scope"), out ExemptionScope exemptionScope))
            {
                exemptionScope = ExemptionScope.Unknown;
            }

            Scope = exemptionScope;
        }

        public string? Name => _attributeData.AttributeClass?.Name;

        public ExemptionScope Scope { get; }

        public string? Target => GetNamedArgumentValueOrDefault("Target");

        public string? Literal => _attributeData.ConstructorArguments.FirstOrDefault().Value?.ToString()
                                 ?? GetNamedArgumentValueOrDefault("Literal");

        public static string? GetTargetFromAttribute(ExemptionAttribute exemptionAttribute)
        {
            return GetTargetFromAttributeOrSymbol<ISymbol>(exemptionAttribute, null);
        }

        [SuppressMessage(
            "MicrosoftCodeAnalysisCorrectness",
            "RS1035:'CultureInfo.CurrentCulture' is banned for use by analyzers: Analyzers should use the locale given by the compiler command line arguments, not the CurrentCulture",
            Justification = "No public API available to get the locale given to the compiler, see https://github.com/dotnet/roslyn/issues/66566")]
        internal static string? GetTargetFromAttributeOrSymbol<TExpected>(ExemptionAttribute exemptionAttribute, ISymbol? decoratedSymbol)
        {
            if (!string.IsNullOrWhiteSpace(exemptionAttribute.Target))
            {
                return exemptionAttribute.Target;
            }

            if (decoratedSymbol is TExpected)
            {
                return decoratedSymbol.GetFullName();
            }

            var attributeName = exemptionAttribute.Name;
            var scopeName = Enum.GetName(typeof(ExemptionScope), exemptionAttribute.Scope);

            throw new AttributeMissingTargetException(
                string.Format(CultureInfo.CurrentCulture, "Attribute {0} is missing a Target value.", attributeName),
                attributeName ?? "Unknown attribute",
                scopeName);
        }

        private string? GetNamedArgumentValueOrDefault(string argumentName)
        {
            return _attributeData.NamedArguments.FirstOrDefault(x => x.Key == argumentName).Value.Value?.ToString();
        }
    }
}

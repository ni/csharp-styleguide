using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NationalInstruments.Analyzers.Properties;
using NationalInstruments.Analyzers.Utilities;
using NationalInstruments.Analyzers.Utilities.Extensions;

namespace NationalInstruments.Analyzers.Correctness
{
    /// <summary>
    /// Analyzer that reports a diagnostic if a called method's fully-qualified name starts with the text of an
    /// entry listed in one or more control files identified by having a name containing the text 'bannedmethods'.
    /// </summary>
    /// <remarks>
    /// It can also emit diagnostics if the contents of one or more control files cannot be parsed as XML.
    /// </remarks>
    /// <example>
    /// BannedMethods.xml contains:
    ///  <![CDATA[
    /// <BannedMethods>
    ///     <Entry>System.Console</Entry>
    /// </BannedMethods>
    /// ]]>
    ///
    /// <code>
    /// using System;
    ///
    /// class Program
    /// {
    ///     static void Main(string[] args)
    ///     {
    ///         Console.WriteLine("hello, world");  // violation; System.Console.* is banned
    ///     }
    /// }
    /// </code>
    /// </example>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DoNotUseBannedMethodsAnalyzer : NIDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "NI1006";

        private static readonly LocalizableString LocalizedTitle = new LocalizableResourceString(nameof(Resources.NI1006_Title), Resources.ResourceManager, typeof(Resources));

        public static DiagnosticDescriptor Rule { get; } = new DiagnosticDescriptor(
            DiagnosticId,
            LocalizedTitle,
            new LocalizableResourceString(nameof(Resources.NI1006_Message), Resources.ResourceManager, typeof(Resources)),
            Category.Correctness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.NI1006_Description), Resources.ResourceManager, typeof(Resources)),
            helpLinkUri: "https://github.com/ni/csharp-styleguide/blob/main/docs/Banned%20Methods.md");

        public static DiagnosticDescriptor FileParseRule { get; } = new DiagnosticDescriptor(
            DiagnosticId,
            LocalizedTitle,
            new LocalizableResourceString(nameof(Resources.ParseError_Message), Resources.ResourceManager, typeof(Resources)),
            Category.IO,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, FileParseRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecutionIf(IsRunningInProduction);
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationStartContext =>
            {
                var additionalFileService = new AdditionalFileService(compilationStartContext.Options.AdditionalFiles, FileParseRule);
                var analyzer = new BannedMethodsAnalyzer(additionalFileService, compilationStartContext.CancellationToken);
                analyzer.LoadConfigurations("BannedMethods");

                compilationStartContext.RegisterSyntaxNodeAction(
                    analyzer.AnalyzeMethodInvocation,
                    SyntaxKind.InvocationExpression,
                    SyntaxKind.ObjectCreationExpression);

                compilationStartContext.RegisterCompilationEndAction(additionalFileService.ReportAnyParsingDiagnostics);
            });
        }

        private class BannedMethodsAnalyzer : ConfigurableAnalyzer
        {
            private readonly IAdditionalFileService _additionalFileService;
            private readonly HashSet<AnalyzerEntry> _bannedMethods = new HashSet<AnalyzerEntry>();

            public BannedMethodsAnalyzer(IAdditionalFileService additionalFileService, CancellationToken cancellationToken)
                : base(additionalFileService, cancellationToken)
            {
                _additionalFileService = additionalFileService;
            }

            public void AnalyzeMethodInvocation(SyntaxNodeAnalysisContext context)
            {
                var invocationSyntax = context.Node;

                if (_bannedMethods.Count == 0 || invocationSyntax == null)
                {
                    return;
                }

                ISymbol invocation = invocationSyntax.GetDeclaredOrReferencedSymbol(context.SemanticModel);
                var fullName = invocation?.GetFullName();
                if (string.IsNullOrEmpty(fullName))
                {
                    return;
                }

                foreach (var bannedMatch in _bannedMethods.Where(analyzerEntry => analyzerEntry.EntryRegex.IsMatch(fullName)))
                {
                    if (bannedMatch.IsBannedInThisAssembly(context.ContainingSymbol.ContainingAssembly))
                    {
                        var diagnostic = Diagnostic.Create(Rule, invocationSyntax.GetLocation(), fullName, bannedMatch.AdditionalErrorMessage);
                        context.ReportDiagnostic(diagnostic);
                        break;
                    }
                }
            }

            protected override void LoadConfigurations(XElement rootElement, string filePath)
            {
                if (TryGetRootElementDiagnostic(rootElement, "BannedMethods", filePath, FileParseRule, out var diagnostic))
                {
                    _additionalFileService.ParsingDiagnostics.Add(diagnostic);
                }

                foreach (XElement element in rootElement.Elements())
                {
                    ParseBannedMethodElement(element, default(AnalyzerEntryOptionsForParsing));
                }
            }

            private void ParseBannedMethodElement(XElement element, AnalyzerEntryOptionsForParsing options)
            {
                AnalyzerEntryOptionsForParsing updatedOptions = options.UpdateFromElement(element);
                switch (element.Name.LocalName)
                {
                    case "EntryGroup":
                        foreach (XElement child in element.Elements())
                        {
                            ParseBannedMethodElement(child, updatedOptions);
                        }

                        break;
                    case "Entry":
                        var entry = new AnalyzerEntry(element.Value, updatedOptions);
                        _bannedMethods.Add(entry);
                        break;
                    default:
                        var diagnostic = Diagnostic.Create(FileParseRule, Location.None, string.Format(CultureInfo.InvariantCulture, "Unsupported element in BannedMethods.xml: {0}", element.Name.LocalName));
                        _additionalFileService.ParsingDiagnostics.Add(diagnostic);
                        break;
                }
            }

            private struct AnalyzerEntryOptionsForParsing
            {
                private AnalyzerEntryOptionsForParsing(string justification, string alternative, string assemblies)
                {
                    Justification = justification;
                    Alternative = alternative;
                    Assemblies = assemblies;
                }

                public string Justification { get; }

                public string Alternative { get; }

                public string Assemblies { get; }

                public AnalyzerEntryOptionsForParsing WithJustification(string justification)
                {
                    return new AnalyzerEntryOptionsForParsing(justification?.Trim(), Alternative, Assemblies);
                }

                public AnalyzerEntryOptionsForParsing WithAlternative(string alternative)
                {
                    return new AnalyzerEntryOptionsForParsing(Justification, alternative?.Trim(), Assemblies);
                }

                public AnalyzerEntryOptionsForParsing WithAssemblies(string assemblies)
                {
                    return new AnalyzerEntryOptionsForParsing(Justification, Alternative, assemblies?.Trim());
                }

                public AnalyzerEntryOptionsForParsing UpdateFromElement(XElement element)
                {
                    AnalyzerEntryOptionsForParsing toReturn = this;
                    foreach (XAttribute attribute in element.Attributes())
                    {
                        switch (attribute.Name.LocalName)
                        {
                            case "Justification":
                                toReturn = toReturn.WithJustification(attribute.Value);
                                break;
                            case "Alternative":
                                toReturn = toReturn.WithAlternative(attribute.Value);
                                break;
                            case "Assemblies":
                                toReturn = toReturn.WithAssemblies(attribute.Value);
                                break;
                        }
                    }

                    return toReturn;
                }
            }

            private class AnalyzerEntry : IEquatable<AnalyzerEntry>
            {
                public AnalyzerEntry(string entryName, AnalyzerEntryOptionsForParsing options)
                {
                    EntryName = entryName.Trim();
                    EntryRegex = GetEscapedRegex(EntryName);
                    Justification = options.Justification;
                    Alternative = options.Alternative;
                    AssemblyRegexes = !string.IsNullOrWhiteSpace(options.Assemblies)
                        ? options.Assemblies.Trim().Split(',').Select(x => GetEscapedRegex(x))
                        : new List<Regex>();
                }

                public string EntryName { get; }

                public Regex EntryRegex { get; }

                public string Justification { get; }

                public string Alternative { get; }

                public string Assemblies { get; }

                public IEnumerable<Regex> AssemblyRegexes { get; }

                public string AdditionalErrorMessage
                {
                    get
                    {
                        var messageBuilder = new StringBuilder();
                        if (!string.IsNullOrEmpty(Alternative))
                        {
                            messageBuilder.Append(", ").AppendFormat(CultureInfo.CurrentCulture, Resources.NI1006_AdditionalInfo_Alternative, Alternative);
                        }

                        if (!string.IsNullOrEmpty(Justification))
                        {
                            messageBuilder.Append(", ").AppendFormat(CultureInfo.CurrentCulture, Resources.NI1006_AdditionalInfo_Justification, Justification);
                        }

                        if (AssemblyRegexes.Any())
                        {
                            messageBuilder.Append(", ").Append(Resources.NI1006_AdditionalInfo_BannedInThisAssembly);
                        }

                        return messageBuilder.ToString();
                    }
                }

                public static bool operator ==(AnalyzerEntry entry1, AnalyzerEntry entry2) => EqualityComparer<AnalyzerEntry>.Default.Equals(entry1, entry2);

                public static bool operator !=(AnalyzerEntry entry1, AnalyzerEntry entry2) => !(entry1 == entry2);

                public bool IsBannedInThisAssembly(IAssemblySymbol assemblySymbol)
                {
                    return !AssemblyRegexes.Any() || AssemblyRegexes.Any(regex => regex.IsMatch(assemblySymbol.Name));
                }

                public override bool Equals(object obj)
                {
                    return Equals(obj as AnalyzerEntry);
                }

                public override int GetHashCode()
                {
                    const int MagicValue = -1521134295;
                    var hashCode = -1211575830;

                    hashCode = (hashCode * MagicValue) + EntryName.GetHashCode();
                    hashCode = (hashCode * MagicValue) + Justification?.GetHashCode() ?? 0;
                    hashCode = (hashCode * MagicValue) + Alternative?.GetHashCode() ?? 0;
                    hashCode = (hashCode * MagicValue) + Assemblies?.GetHashCode() ?? 0;

                    return hashCode;
                }

                public bool Equals(AnalyzerEntry other)
                {
                    return other != null
                        && EntryName == other.EntryName
                        && Justification == other.Justification
                        && Alternative == other.Alternative
                        && Assemblies == other.Assemblies;
                }

                private static Regex GetEscapedRegex(string name)
                {
                    return new Regex(string.Format(CultureInfo.InvariantCulture, @"^{0}(\b|$)", Regex.Escape(name)), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                }
            }
        }
    }
}

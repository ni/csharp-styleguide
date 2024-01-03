using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using NationalInstruments.Analyzers.Properties;
using NationalInstruments.Analyzers.Utilities;
using NationalInstruments.Analyzers.Utilities.Extensions;
using NationalInstruments.Analyzers.Utilities.Text;

namespace NationalInstruments.Analyzers.Style
{
    /// <summary>
    /// CA1704: Identifiers should be spelled correctly
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SpellingAnalyzer : NIDiagnosticAnalyzer
    {
        public const string MisspelledDiagnosticId = "NI1704";
        public const string UnmeaningfulDiagnosticId = "NI1728";

        private const DiagnosticSeverity DefaultDiagnosticSeverity = DiagnosticSeverity.Warning;

        private static readonly Func<string, LocalizableString> CreateLocalizableResourceString = (string nameOfLocalizableResource) => new LocalizableResourceString(nameOfLocalizableResource, Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString LocalizableTitle = CreateLocalizableResourceString(nameof(Resources.NI1704_Title));
        private static readonly LocalizableString LocalizableDescription = CreateLocalizableResourceString(nameof(Resources.NI1704_Description));

        private static readonly SourceTextValueProvider<CodeAnalysisDictionary> _xmlDictionaryProvider = new SourceTextValueProvider<CodeAnalysisDictionary>(ParseXmlDictionary);
        private static readonly SourceTextValueProvider<CodeAnalysisDictionary> _dicDictionaryProvider = new SourceTextValueProvider<CodeAnalysisDictionary>(ParseDicDictionary);
        private static readonly CodeAnalysisDictionary _mainDictionary = GetMainDictionary();

        public static readonly DiagnosticDescriptor FileParseRule = new DiagnosticDescriptor(
            MisspelledDiagnosticId,
            LocalizableTitle,
            CreateLocalizableResourceString(nameof(Resources.NI1704_DictionaryParseError_Message)),
            Category.Style,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor AssemblyRule = new DiagnosticDescriptor(
            MisspelledDiagnosticId,
            LocalizableTitle,
            CreateLocalizableResourceString(nameof(Resources.NI1704_Assembly_Message)),
            Category.Style,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: true,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static readonly DiagnosticDescriptor NamespaceRule = new DiagnosticDescriptor(
            MisspelledDiagnosticId,
            LocalizableTitle,
            CreateLocalizableResourceString(nameof(Resources.NI1704_Namespace_Message)),
            Category.Style,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: true,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static readonly DiagnosticDescriptor TypeRule = new DiagnosticDescriptor(
            MisspelledDiagnosticId,
            LocalizableTitle,
            CreateLocalizableResourceString(nameof(Resources.NI1704_Type_Message)),
            Category.Style,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: true,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static readonly DiagnosticDescriptor MemberRule = new DiagnosticDescriptor(
            MisspelledDiagnosticId,
            LocalizableTitle,
            CreateLocalizableResourceString(nameof(Resources.NI1704_Member_Message)),
            Category.Style,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: true,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static readonly DiagnosticDescriptor VariableRule = new DiagnosticDescriptor(
            MisspelledDiagnosticId,
            LocalizableTitle,
            CreateLocalizableResourceString(nameof(Resources.NI1704_Variable_Message)),
            Category.Style,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: true,
            description: LocalizableDescription);

        public static readonly DiagnosticDescriptor MemberParameterRule = new DiagnosticDescriptor(
            MisspelledDiagnosticId,
            LocalizableTitle,
            CreateLocalizableResourceString(nameof(Resources.NI1704_MemberParameter_Message)),
            Category.Style,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: true,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static readonly DiagnosticDescriptor DelegateParameterRule = new DiagnosticDescriptor(
            MisspelledDiagnosticId,
            LocalizableTitle,
            CreateLocalizableResourceString(nameof(Resources.NI1704_DelegateParameter_Message)),
            Category.Style,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: true,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static readonly DiagnosticDescriptor TypeTypeParameterRule = new DiagnosticDescriptor(
            MisspelledDiagnosticId,
            LocalizableTitle,
            CreateLocalizableResourceString(nameof(Resources.NI1704_TypeTypeParameter_Message)),
            Category.Style,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: true,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static readonly DiagnosticDescriptor MethodTypeParameterRule = new DiagnosticDescriptor(
            MisspelledDiagnosticId,
            LocalizableTitle,
            CreateLocalizableResourceString(nameof(Resources.NI1704_MethodTypeParameter_Message)),
            Category.Style,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: true,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static readonly DiagnosticDescriptor AssemblyMoreMeaningfulNameRule = new DiagnosticDescriptor(
            UnmeaningfulDiagnosticId,
            LocalizableTitle,
            CreateLocalizableResourceString(nameof(Resources.NI1704_AssemblyMoreMeaningful_Message)),
            Category.Style,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static readonly DiagnosticDescriptor NamespaceMoreMeaningfulNameRule = new DiagnosticDescriptor(
            UnmeaningfulDiagnosticId,
            LocalizableTitle,
            CreateLocalizableResourceString(nameof(Resources.NI1704_NamespaceMoreMeaningful_Message)),
            Category.Style,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static readonly DiagnosticDescriptor TypeMoreMeaningfulNameRule = new DiagnosticDescriptor(
            UnmeaningfulDiagnosticId,
            LocalizableTitle,
            CreateLocalizableResourceString(nameof(Resources.NI1704_TypeMoreMeaningful_Message)),
            Category.Style,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static readonly DiagnosticDescriptor MemberMoreMeaningfulNameRule = new DiagnosticDescriptor(
            UnmeaningfulDiagnosticId,
            LocalizableTitle,
            CreateLocalizableResourceString(nameof(Resources.NI1704_MemberMoreMeaningful_Message)),
            Category.Style,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static readonly DiagnosticDescriptor MemberParameterMoreMeaningfulNameRule = new DiagnosticDescriptor(
            UnmeaningfulDiagnosticId,
            LocalizableTitle,
            CreateLocalizableResourceString(nameof(Resources.NI1704_MemberParameterMoreMeaningful_Message)),
            Category.Style,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static readonly DiagnosticDescriptor DelegateParameterMoreMeaningfulNameRule = new DiagnosticDescriptor(
            UnmeaningfulDiagnosticId,
            LocalizableTitle,
            CreateLocalizableResourceString(nameof(Resources.NI1704_DelegateParameterMoreMeaningful_Message)),
            Category.Style,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static readonly DiagnosticDescriptor TypeTypeParameterMoreMeaningfulNameRule = new DiagnosticDescriptor(
            UnmeaningfulDiagnosticId,
            LocalizableTitle,
            CreateLocalizableResourceString(nameof(Resources.NI1704_TypeTypeParameterMoreMeaningful_Message)),
            Category.Style,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static readonly DiagnosticDescriptor MethodTypeParameterMoreMeaningfulNameRule = new DiagnosticDescriptor(
            UnmeaningfulDiagnosticId,
            LocalizableTitle,
            CreateLocalizableResourceString(nameof(Resources.NI1704_MethodTypeParameterMoreMeaningful_Message)),
            Category.Style,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            FileParseRule,
            AssemblyRule,
            NamespaceRule,
            TypeRule,
            MemberRule,
            MemberParameterRule,
            DelegateParameterRule,
            TypeTypeParameterRule,
            MethodTypeParameterRule,
            AssemblyMoreMeaningfulNameRule,
            NamespaceMoreMeaningfulNameRule,
            TypeMoreMeaningfulNameRule,
            MemberMoreMeaningfulNameRule,
            MemberParameterMoreMeaningfulNameRule,
            DelegateParameterMoreMeaningfulNameRule,
            TypeTypeParameterMoreMeaningfulNameRule,
            MethodTypeParameterMoreMeaningfulNameRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecutionIf(IsRunningInProduction && !InDebugMode);
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static CodeAnalysisDictionary GetMainDictionary()
        {
            var assemblyType = typeof(SpellingAnalyzer);
            var assembly = assemblyType.GetTypeInfo().Assembly;
            var dictionary = $"{assemblyType.Namespace}.Dictionary.dic";

            using (var stream = assembly.GetManifestResourceStream(dictionary))
            {
                var text = SourceText.From(stream);
                return ParseDicDictionary(text);
            }
        }

        private static CodeAnalysisDictionary ParseXmlDictionary(SourceText text)
            => text.Parse(CodeAnalysisDictionary.CreateFromXml);

        private static CodeAnalysisDictionary ParseDicDictionary(SourceText text)
            => text.Parse(CodeAnalysisDictionary.CreateFromDic);

        private static string RemovePrefixIfPresent(string prefix, string name)
            => name.StartsWith(prefix, StringComparison.Ordinal) ? name.Substring(1) : name;

        private static IEnumerable<Diagnostic> GetMisspelledWordDiagnostics(ISymbol symbol, string misspelledWord)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Assembly:
                    // Do not report spelling rules in assembly names for now. The spelling should be enforced
                    // at the API level and the name of the assembly on disk isn't relevant
                    // yield return Diagnostic.Create(AssemblyRule, Location.None, misspelledWord, symbol.Name);
                    break;

                case SymbolKind.Namespace:
                    yield return Diagnostic.Create(NamespaceRule, symbol.Locations.First(), misspelledWord, symbol.ToDisplayString());
                    break;

                case SymbolKind.NamedType:
                    foreach (var location in symbol.Locations)
                    {
                        yield return Diagnostic.Create(TypeRule, location, misspelledWord, symbol.ToDisplayString());
                    }

                    break;

                case SymbolKind.Method:
                case SymbolKind.Property:
                case SymbolKind.Event:
                case SymbolKind.Field:
                    yield return Diagnostic.Create(MemberRule, symbol.Locations.First(), misspelledWord, symbol.ToDisplayString());
                    break;

                case SymbolKind.Parameter:
                    yield return symbol.ContainingType.TypeKind == TypeKind.Delegate
                        ? Diagnostic.Create(DelegateParameterRule, symbol.Locations.First(), symbol.ContainingType.ToDisplayString(), misspelledWord, symbol.Name)
                        : Diagnostic.Create(MemberParameterRule, symbol.Locations.First(), symbol.ContainingSymbol.ToDisplayString(), misspelledWord, symbol.Name);

                    break;

                case SymbolKind.TypeParameter:
                    yield return symbol.ContainingSymbol.Kind == SymbolKind.Method
                        ? Diagnostic.Create(MethodTypeParameterRule, symbol.Locations.First(), symbol.ContainingSymbol.ToDisplayString(), misspelledWord, symbol.Name)
                        : Diagnostic.Create(TypeTypeParameterRule, symbol.Locations.First(), symbol.ContainingSymbol.ToDisplayString(), misspelledWord, symbol.Name);

                    break;

                case SymbolKind.Local:
                    yield return Diagnostic.Create(VariableRule, symbol.Locations.First(), misspelledWord, symbol.ToDisplayString());
                    break;

                default:
                    throw new NotImplementedException($"Unknown SymbolKind: {symbol.Kind}");
            }
        }

        private void OnCompilationStart(CompilationStartAnalysisContext compilationStartContext)
        {
            if (IsRunningInProduction && InDebugMode)
            {
                System.Diagnostics.Debugger.Launch();
            }

            var projectDictionary = _mainDictionary.Clone();
            var dictionaries = ReadDictionaries();
            if (dictionaries.Any())
            {
                var aggregatedDictionary = dictionaries.Aggregate((x, y) => x.CombineWith(y));
                projectDictionary = projectDictionary.CombineWith(aggregatedDictionary);
            }

            compilationStartContext.RegisterOperationAction(AnalyzeVariable, OperationKind.VariableDeclarator);
            compilationStartContext.RegisterCompilationEndAction(AnalyzeAssembly);
            compilationStartContext.RegisterSymbolAction(
                AnalyzeSymbol,
                SymbolKind.Namespace,
                SymbolKind.NamedType,
                SymbolKind.Method,
                SymbolKind.Property,
                SymbolKind.Event,
                SymbolKind.Field,
                SymbolKind.Parameter);

            IEnumerable<CodeAnalysisDictionary> ReadDictionaries()
            {
                var fileProvider = AdditionalFileProvider.FromOptions(compilationStartContext.Options);
                return fileProvider?.GetMatchingFiles(@"(?:dictionary|custom).*?\.(?:xml|dic)$")
                    .Select(CreateDictionaryFromAdditionalText)
                    .OfType<CodeAnalysisDictionary>() ?? Enumerable.Empty<CodeAnalysisDictionary>();

                CodeAnalysisDictionary? CreateDictionaryFromAdditionalText(AdditionalText additionalFile)
                {
                    CodeAnalysisDictionary? dictionary = null;
                    var text = additionalFile.GetText(compilationStartContext.CancellationToken);
                    var isXml = additionalFile.Path.EndsWith("xml", StringComparison.OrdinalIgnoreCase);
                    var provider = isXml ? _xmlDictionaryProvider : _dicDictionaryProvider;

                    if (text is not null && !compilationStartContext.TryGetValue(text, provider, out dictionary))
                    {
                        try
                        {
                            // Annoyingly (and expectedly), TryGetValue swallows the parsing exception,
                            // so we have to parse again to get it.
                            var unused = isXml ? ParseXmlDictionary(text) : ParseDicDictionary(text);
                            ReportFileParseDiagnostic(additionalFile.Path, "Unknown error");
                        }
                        catch (Exception ex)
                        {
                            ReportFileParseDiagnostic(additionalFile.Path, ex.Message);
                        }
                    }

                    return dictionary;
                }

                void ReportFileParseDiagnostic(string filePath, string message)
                {
                    var diagnostic = Diagnostic.Create(FileParseRule, Location.None, filePath, message);
                    compilationStartContext.RegisterCompilationEndAction(x => x.ReportDiagnostic(diagnostic));
                }
            }

            void AnalyzeVariable(OperationAnalysisContext operationContext)
            {
                var variableOperation = (IVariableDeclaratorOperation)operationContext.Operation;
                var variable = variableOperation.Symbol;

                var diagnostics = GetDiagnosticsForSymbol(variable, variable.Name, checkForUnmeaningful: false);
                foreach (var diagnostic in diagnostics)
                {
                    operationContext.ReportDiagnostic(diagnostic);
                }
            }

            void AnalyzeAssembly(CompilationAnalysisContext context)
            {
                var assembly = context.Compilation.Assembly;
                var diagnostics = GetDiagnosticsForSymbol(assembly, assembly.Name);

                foreach (var diagnostic in diagnostics)
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }

            void AnalyzeSymbol(SymbolAnalysisContext symbolContext)
            {
                var typeParameterDiagnostics = Enumerable.Empty<Diagnostic>();

                ISymbol symbol = symbolContext.Symbol;
                if (symbol.IsOverride)
                {
                    return;
                }

                var symbolName = symbol.Name;
                switch (symbol)
                {
                    case IFieldSymbol field:
                        symbolName = RemovePrefixIfPresent("_", symbolName);
                        break;

                    case IMethodSymbol method:
                        switch (method.MethodKind)
                        {
                            case MethodKind.PropertyGet:
                            case MethodKind.PropertySet:
                                return;

                            case MethodKind.Constructor:
                            case MethodKind.StaticConstructor:
                                symbolName = symbol.ContainingType.Name;
                                break;
                        }

                        foreach (var typeParameter in method.TypeParameters)
                        {
                            typeParameterDiagnostics = GetDiagnosticsForSymbol(typeParameter, RemovePrefixIfPresent("T", typeParameter.Name));
                        }

                        break;

                    case INamedTypeSymbol type:
                        if (type.TypeKind == TypeKind.Interface)
                        {
                            symbolName = RemovePrefixIfPresent("I", symbolName);
                        }

                        foreach (var typeParameter in type.TypeParameters)
                        {
                            typeParameterDiagnostics = GetDiagnosticsForSymbol(typeParameter, RemovePrefixIfPresent("T", typeParameter.Name));
                        }

                        break;
                }

                var diagnostics = GetDiagnosticsForSymbol(symbol, symbolName);
                var allDiagnostics = typeParameterDiagnostics.Concat(diagnostics);
                foreach (var diagnostic in allDiagnostics)
                {
                    symbolContext.ReportDiagnostic(diagnostic);
                }
            }

            IEnumerable<Diagnostic> GetDiagnosticsForSymbol(ISymbol symbol, string symbolName, bool checkForUnmeaningful = true)
            {
                var diagnostics = new List<Diagnostic>();
                if (checkForUnmeaningful && symbolName.Length == 1)
                {
                    // diagnostics.AddRange(GetUnmeaningfulIdentifierDiagnostics(symbol, symbolName));
                }
                else
                {
                    foreach (var misspelledWord in GetMisspelledWords(symbolName))
                    {
                        diagnostics.AddRange(GetMisspelledWordDiagnostics(symbol, misspelledWord));
                    }
                }

                return diagnostics;
            }

            IEnumerable<string> GetMisspelledWords(string symbolName)
            {
                var parser = new WordParser(symbolName, WordParserOptions.SplitCompoundWords);
                if (parser.PeekWord() is not null)
                {
                    var word = parser.NextWord()!;

                    do
                    {
                        if (IsWordAcronym(word) || IsWordNumeric(word) || IsWordSpelledCorrectly(word))
                        {
                            continue;
                        }

                        yield return word;
                    }
                    while ((word = parser.NextWord()) is not null);
                }
            }

            bool IsWordAcronym(string word) => word.All(char.IsUpper);

            bool IsWordNumeric(string word) => char.IsDigit(word[0]);

            bool IsWordSpelledCorrectly(string word)
                => !projectDictionary.UnrecognizedWords.Contains(word) && projectDictionary.RecognizedWords.Contains(word);
        }
    }
}

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
        public const string RuleId = "NI1704";

        private const string Category = "Naming";
        private const DiagnosticSeverity DefaultDiagnosticSeverity = DiagnosticSeverity.Warning;

        private static readonly LocalizableString LocalizableTitle = new LocalizableResourceString(nameof(Resources.IdentifiersShouldBeSpelledCorrectlyTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString LocalizableMessageFileParse = new LocalizableResourceString(nameof(Resources.IdentifiersShouldBeSpelledCorrectlyFileParse), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString LocalizableMessageAssembly = new LocalizableResourceString(nameof(Resources.IdentifiersShouldBeSpelledCorrectlyMessageAssembly), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString LocalizableMessageNamespace = new LocalizableResourceString(nameof(Resources.IdentifiersShouldBeSpelledCorrectlyMessageNamespace), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString LocalizableMessageType = new LocalizableResourceString(nameof(Resources.IdentifiersShouldBeSpelledCorrectlyMessageType), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString LocalizableMessageMember = new LocalizableResourceString(nameof(Resources.IdentifiersShouldBeSpelledCorrectlyMessageMember), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString LocalizableMessageVariable = new LocalizableResourceString(nameof(Resources.IdentifiersShouldBeSpelledCorrectlyMessageVariable), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString LocalizableMessageMemberParameter = new LocalizableResourceString(nameof(Resources.IdentifiersShouldBeSpelledCorrectlyMessageMemberParameter), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString LocalizableMessageDelegateParameter = new LocalizableResourceString(nameof(Resources.IdentifiersShouldBeSpelledCorrectlyMessageDelegateParameter), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString LocalizableMessageTypeTypeParameter = new LocalizableResourceString(nameof(Resources.IdentifiersShouldBeSpelledCorrectlyMessageTypeTypeParameter), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString LocalizableMessageMethodTypeParameter = new LocalizableResourceString(nameof(Resources.IdentifiersShouldBeSpelledCorrectlyMessageMethodTypeParameter), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString LocalizableMessageAssemblyMoreMeaningfulName = new LocalizableResourceString(nameof(Resources.IdentifiersShouldBeSpelledCorrectlyMessageAssemblyMoreMeaningfulName), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString LocalizableMessageNamespaceMoreMeaningfulName = new LocalizableResourceString(nameof(Resources.IdentifiersShouldBeSpelledCorrectlyMessageNamespaceMoreMeaningfulName), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString LocalizableMessageTypeMoreMeaningfulName = new LocalizableResourceString(nameof(Resources.IdentifiersShouldBeSpelledCorrectlyMessageTypeMoreMeaningfulName), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString LocalizableMessageMemberMoreMeaningfulName = new LocalizableResourceString(nameof(Resources.IdentifiersShouldBeSpelledCorrectlyMessageMemberMoreMeaningfulName), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString LocalizableMessageMemberParameterMoreMeaningfulName = new LocalizableResourceString(nameof(Resources.IdentifiersShouldBeSpelledCorrectlyMessageMemberParameterMoreMeaningfulName), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString LocalizableMessageDelegateParameterMoreMeaningfulName = new LocalizableResourceString(nameof(Resources.IdentifiersShouldBeSpelledCorrectlyMessageDelegateParameterMoreMeaningfulName), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString LocalizableMessageTypeTypeParameterMoreMeaningfulName = new LocalizableResourceString(nameof(Resources.IdentifiersShouldBeSpelledCorrectlyMessageTypeTypeParameterMoreMeaningfulName), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString LocalizableMessageMethodTypeParameterMoreMeaningfulName = new LocalizableResourceString(nameof(Resources.IdentifiersShouldBeSpelledCorrectlyMessageMethodTypeParameterMoreMeaningfulName), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString LocalizableDescription = new LocalizableResourceString(nameof(Resources.IdentifiersShouldBeSpelledCorrectlyDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly SourceTextValueProvider<CodeAnalysisDictionary> _xmlDictionaryProvider = new SourceTextValueProvider<CodeAnalysisDictionary>(ParseXmlDictionary);
        private static readonly SourceTextValueProvider<CodeAnalysisDictionary> _dicDictionaryProvider = new SourceTextValueProvider<CodeAnalysisDictionary>(ParseDicDictionary);
        private static readonly CodeAnalysisDictionary _mainDictionary = GetMainDictionary();

        public static DiagnosticDescriptor FileParseRule { get; } = new DiagnosticDescriptor(
            $"{RuleId}_ParseError",
            LocalizableTitle,
            LocalizableMessageFileParse,
            Category,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor AssemblyRule { get; } = new DiagnosticDescriptor(
            RuleId,
            LocalizableTitle,
            LocalizableMessageAssembly,
            Category,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static DiagnosticDescriptor NamespaceRule { get; } = new DiagnosticDescriptor(
            RuleId,
            LocalizableTitle,
            LocalizableMessageNamespace,
            Category,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static DiagnosticDescriptor TypeRule { get; } = new DiagnosticDescriptor(
            RuleId,
            LocalizableTitle,
            LocalizableMessageType,
            Category,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static DiagnosticDescriptor MemberRule { get; } = new DiagnosticDescriptor(
            RuleId,
            LocalizableTitle,
            LocalizableMessageMember,
            Category,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static DiagnosticDescriptor VariableRule { get; } = new DiagnosticDescriptor(
            RuleId,
            LocalizableTitle,
            LocalizableMessageVariable,
            Category,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription);

        public static DiagnosticDescriptor MemberParameterRule { get; } = new DiagnosticDescriptor(
            RuleId,
            LocalizableTitle,
            LocalizableMessageMemberParameter,
            Category,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static DiagnosticDescriptor DelegateParameterRule { get; } = new DiagnosticDescriptor(
            RuleId,
            LocalizableTitle,
            LocalizableMessageDelegateParameter,
            Category,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static DiagnosticDescriptor TypeTypeParameterRule { get; } = new DiagnosticDescriptor(
            RuleId,
            LocalizableTitle,
            LocalizableMessageTypeTypeParameter,
            Category,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static DiagnosticDescriptor MethodTypeParameterRule { get; } = new DiagnosticDescriptor(
            RuleId,
            LocalizableTitle,
            LocalizableMessageMethodTypeParameter,
            Category,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static DiagnosticDescriptor AssemblyMoreMeaningfulNameRule { get; } = new DiagnosticDescriptor(
            RuleId,
            LocalizableTitle,
            LocalizableMessageAssemblyMoreMeaningfulName,
            Category,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static DiagnosticDescriptor NamespaceMoreMeaningfulNameRule { get; } = new DiagnosticDescriptor(
            RuleId,
            LocalizableTitle,
            LocalizableMessageNamespaceMoreMeaningfulName,
            Category,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static DiagnosticDescriptor TypeMoreMeaningfulNameRule { get; } = new DiagnosticDescriptor(
            RuleId,
            LocalizableTitle,
            LocalizableMessageTypeMoreMeaningfulName,
            Category,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static DiagnosticDescriptor MemberMoreMeaningfulNameRule { get; } = new DiagnosticDescriptor(
            RuleId,
            LocalizableTitle,
            LocalizableMessageMemberMoreMeaningfulName,
            Category,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static DiagnosticDescriptor MemberParameterMoreMeaningfulNameRule { get; } = new DiagnosticDescriptor(
            RuleId,
            LocalizableTitle,
            LocalizableMessageMemberParameterMoreMeaningfulName,
            Category,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static DiagnosticDescriptor DelegateParameterMoreMeaningfulNameRule { get; } = new DiagnosticDescriptor(
            RuleId,
            LocalizableTitle,
            LocalizableMessageDelegateParameterMoreMeaningfulName,
            Category,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static DiagnosticDescriptor TypeTypeParameterMoreMeaningfulNameRule { get; } = new DiagnosticDescriptor(
            RuleId,
            LocalizableTitle,
            LocalizableMessageTypeTypeParameterMoreMeaningfulName,
            Category,
            DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: LocalizableDescription,
            helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1704-identifiers-should-be-spelled-correctly");

        public static DiagnosticDescriptor MethodTypeParameterMoreMeaningfulNameRule { get; } = new DiagnosticDescriptor(
            RuleId,
            LocalizableTitle,
            LocalizableMessageMethodTypeParameterMoreMeaningfulName,
            Category,
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

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.EnableConcurrentExecutionIf(IsRunningInProduction && !InDebugMode);
            analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            analysisContext.RegisterCompilationStartAction(OnCompilationStart);
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

        private static IEnumerable<Diagnostic> GetUnmeaningfulIdentifierDiagnostics(ISymbol symbol, string symbolName)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Assembly:
                    yield return Diagnostic.Create(AssemblyMoreMeaningfulNameRule, Location.None, symbolName);
                    break;

                case SymbolKind.Namespace:
                    yield return Diagnostic.Create(NamespaceMoreMeaningfulNameRule, symbol.Locations.First(), symbolName);
                    break;

                case SymbolKind.NamedType:
                    foreach (var location in symbol.Locations)
                    {
                        yield return Diagnostic.Create(TypeMoreMeaningfulNameRule, location, symbolName);
                    }

                    break;

                case SymbolKind.Method:
                case SymbolKind.Property:
                case SymbolKind.Event:
                case SymbolKind.Field:
                    yield return Diagnostic.Create(MemberMoreMeaningfulNameRule, symbol.Locations.First(), symbolName);
                    break;

                case SymbolKind.Parameter:
                    yield return symbol.ContainingType.TypeKind == TypeKind.Delegate
                        ? Diagnostic.Create(DelegateParameterMoreMeaningfulNameRule, symbol.Locations.First(), symbol.ContainingType.ToDisplayString(), symbolName)
                        : Diagnostic.Create(MemberParameterMoreMeaningfulNameRule, symbol.Locations.First(), symbol.ContainingSymbol.ToDisplayString(), symbolName);
                    break;

                case SymbolKind.TypeParameter:
                    yield return symbol.ContainingSymbol.Kind == SymbolKind.Method
                        ? Diagnostic.Create(MethodTypeParameterMoreMeaningfulNameRule, symbol.Locations.First(), symbol.ContainingSymbol.ToDisplayString(), symbol.Name)
                        : Diagnostic.Create(TypeTypeParameterMoreMeaningfulNameRule, symbol.Locations.First(), symbol.ContainingSymbol.ToDisplayString(), symbol.Name);
                    break;

                default:
                    throw new NotImplementedException($"Unknown SymbolKind: {symbol.Kind}");
            }
        }

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
                return fileProvider.GetMatchingFiles(@"(?:dictionary|custom).*?\.(?:xml|dic)$")
                    .Select(CreateDictionaryFromAdditionalText)
                    .Where(x => x != null);

                CodeAnalysisDictionary CreateDictionaryFromAdditionalText(AdditionalText additionalFile)
                {
                    var text = additionalFile.GetText(compilationStartContext.CancellationToken);
                    var isXml = additionalFile.Path.EndsWith("xml", StringComparison.OrdinalIgnoreCase);
                    var provider = isXml ? _xmlDictionaryProvider : _dicDictionaryProvider;

                    if (!compilationStartContext.TryGetValue(text, provider, out var dictionary))
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
                if (parser.PeekWord() != null)
                {
                    var word = parser.NextWord();

                    do
                    {
                        if (IsWordAcronym(word) || IsWordNumeric(word) || IsWordSpelledCorrectly(word))
                        {
                            continue;
                        }

                        yield return word;
                    }
                    while ((word = parser.NextWord()) != null);
                }
            }

            bool IsWordAcronym(string word) => word.All(char.IsUpper);

            bool IsWordNumeric(string word) => char.IsDigit(word[0]);

            bool IsWordSpelledCorrectly(string word)
                => !projectDictionary.UnrecognizedWords.Contains(word) && projectDictionary.RecognizedWords.Contains(word);
        }
    }
}

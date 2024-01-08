using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

        private static readonly SourceTextValueProvider<(CodeAnalysisDictionary? Dictionary, Exception? Exception)> _xmlDictionaryProvider = new(text => ParseDictionary(text, isXml: true));
        private static readonly SourceTextValueProvider<(CodeAnalysisDictionary? Dictionary, Exception? Exception)> _dicDictionaryProvider = new(text => ParseDictionary(text, isXml: false));
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

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;

            var dictionaries = ReadDictionaries().Add(_mainDictionary);

            context.RegisterOperationAction(AnalyzeVariable, OperationKind.VariableDeclarator);
            context.RegisterCompilationEndAction(AnalyzeAssembly);
            context.RegisterSymbolAction(
                AnalyzeSymbol,
                SymbolKind.Namespace,
                SymbolKind.NamedType,
                SymbolKind.Method,
                SymbolKind.Property,
                SymbolKind.Event,
                SymbolKind.Field,
                SymbolKind.Parameter);

            ImmutableArray<CodeAnalysisDictionary> ReadDictionaries()
            {
                var fileProvider = AdditionalFileProvider.FromOptions(context.Options);
                return fileProvider.GetMatchingFiles(@"(?:dictionary|custom).*?\.(?:xml|dic)$")
                    .Select(GetOrCreateDictionaryFromAdditionalText)
                    .Where(x => x != null)
                    .ToImmutableArray();
            }

            CodeAnalysisDictionary GetOrCreateDictionaryFromAdditionalText(AdditionalText additionalText)
            {
                var isXml = additionalText.Path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);
                var provider = isXml ? _xmlDictionaryProvider : _dicDictionaryProvider;

                var (dictionary, exception) = context.TryGetValue(additionalText.GetTextOrEmpty(cancellationToken), provider, out var result)
                    ? result
                    : default;

                if (exception is not null)
                {
                    var diagnostic = Diagnostic.Create(FileParseRule, Location.None, additionalText.Path, exception.Message);
                    context.RegisterCompilationEndAction(x => x.ReportDiagnostic(diagnostic));
                }

                return dictionary!;
            }

            void AnalyzeVariable(OperationAnalysisContext operationContext)
            {
                var variableOperation = (IVariableDeclaratorOperation)operationContext.Operation;
                var variable = variableOperation.Symbol;

                ReportDiagnosticsForSymbol(variable, variable.Name, operationContext.ReportDiagnostic, checkForUnmeaningful: false);
            }

            void AnalyzeAssembly(CompilationAnalysisContext analysisContext)
            {
                var assembly = analysisContext.Compilation.Assembly;

                ReportDiagnosticsForSymbol(assembly, assembly.Name, analysisContext.ReportDiagnostic);
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
                    case IFieldSymbol:
                        symbolName = RemovePrefixIfPresent('_', symbolName);
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
                            ReportDiagnosticsForSymbol(typeParameter, RemovePrefixIfPresent('T', typeParameter.Name), symbolContext.ReportDiagnostic);
                        }

                        break;

                    case INamedTypeSymbol type:
                        if (type.TypeKind == TypeKind.Interface)
                        {
                            symbolName = RemovePrefixIfPresent('I', symbolName);
                        }

                        foreach (var typeParameter in type.TypeParameters)
                        {
                            ReportDiagnosticsForSymbol(typeParameter, RemovePrefixIfPresent('T', typeParameter.Name), symbolContext.ReportDiagnostic);
                        }

                        break;

                    case IParameterSymbol parameter:
                        // Check if the member this parameter is part of is an override/interface implementation
                        if (parameter.ContainingSymbol.IsImplementationOfAnyImplicitInterfaceMember()
                            || parameter.ContainingSymbol.IsImplementationOfAnyExplicitInterfaceMember()
                            || parameter.ContainingSymbol.IsOverride)
                        {
                            if (NameMatchesBase(parameter))
                            {
                                return;
                            }
                        }

                        break;
                }

                ReportDiagnosticsForSymbol(symbol, symbolName, symbolContext.ReportDiagnostic);
            }

            void ReportDiagnosticsForSymbol(ISymbol symbol, string symbolName, Action<Diagnostic> reportDiagnostic, bool checkForUnmeaningful = true)
            {
                foreach (var misspelledWord in GetMisspelledWords(symbolName))
                {
                    reportDiagnostic(GetMisspelledWordDiagnostic(symbol, misspelledWord));
                }

                if (checkForUnmeaningful && symbolName.Length == 1)
                {
                    reportDiagnostic(GetUnmeaningfulIdentifierDiagnostic(symbol, symbolName));
                }
            }

            IEnumerable<string> GetMisspelledWords(string symbolName)
            {
                var parser = new WordParser(symbolName, WordParserOptions.SplitCompoundWords);

                string? word;
                while ((word = parser.NextWord()) is not null)
                {
                    if (!IsWordAcronym(word) && !IsWordNumeric(word) && !IsWordSpelledCorrectly(word))
                    {
                        yield return word;
                    }
                }
            }

            static bool IsWordAcronym(string word) => word.All(char.IsUpper);

            static bool IsWordNumeric(string word) => char.IsDigit(word[0]);

            bool IsWordSpelledCorrectly(string word)
            {
                return !dictionaries.Any((d) => d.ContainsUnrecognizedWord(word)) && dictionaries.Any((d) => d.ContainsRecognizedWord(word));
            }
        }

        /// <summary>
        /// Check if the parameter matches the name of the parameter in any base implementation
        /// </summary>
        private static bool NameMatchesBase(IParameterSymbol parameter)
        {
            if (parameter.ContainingSymbol is IMethodSymbol methodSymbol)
            {
                ImmutableArray<IMethodSymbol> originalDefinitions = methodSymbol.GetOriginalDefinitions();

                foreach (var methodDefinition in originalDefinitions)
                {
                    if (methodDefinition.Parameters.Length > parameter.Ordinal)
                    {
                        if (methodDefinition.Parameters[parameter.Ordinal].Name == parameter.Name)
                        {
                            return true;
                        }
                    }
                }
            }
            else if (parameter.ContainingSymbol is IPropertySymbol propertySymbol)
            {
                ImmutableArray<IPropertySymbol> originalDefinitions = propertySymbol.GetOriginalDefinitions();

                foreach (var propertyDefinition in originalDefinitions)
                {
                    if (propertyDefinition.Parameters.Length > parameter.Ordinal)
                    {
                        if (propertyDefinition.Parameters[parameter.Ordinal].Name == parameter.Name)
                        {
                            return true;
                        }
                    }
                }
            }

            // Name either does not match or there was an issue getting the base implementation
            return false;
        }

        private static CodeAnalysisDictionary GetMainDictionary()
        {
            // The "main" dictionary, Dictionary.dic, was created in WSL Ubuntu with the following commands:
            //
            // Install dependencies:
            // > sudo apt install hunspell-tools hunspell-en-us
            //
            // Create dictionary:
            // > unmunch /usr/share/hunspell/en_US.dic /usr/share/hunspell/en_US.aff > Dictionary.dic
            //
            // Tweak:
            // Added the words: 'namespace'
            var text = SourceText.From(Resources.Dictionary);
            return ParseDicDictionary(text);
        }

        private static (CodeAnalysisDictionary? Dictionary, Exception? Exception) ParseDictionary(SourceText text, bool isXml)
        {
            try
            {
                return (isXml ? ParseXmlDictionary(text) : ParseDicDictionary(text), Exception: null);
            }
            catch (Exception ex)
            {
                return (null, ex);
            }
        }

        private static CodeAnalysisDictionary ParseXmlDictionary(SourceText text)
            => text.Parse(CodeAnalysisDictionary.CreateFromXml);

        private static CodeAnalysisDictionary ParseDicDictionary(SourceText text)
            => text.Parse(CodeAnalysisDictionary.CreateFromDic);

        private static string RemovePrefixIfPresent(char prefix, string name)
            => name.Length > 0 && name[0] == prefix ? name.Substring(1) : name;

        private static Diagnostic GetMisspelledWordDiagnostic(ISymbol symbol, string misspelledWord)
        {
            return symbol.Kind switch
            {
                SymbolKind.Assembly => symbol.CreateDiagnostic(AssemblyRule, misspelledWord, symbol.Name),
                SymbolKind.Namespace => symbol.CreateDiagnostic(NamespaceRule, misspelledWord, symbol.ToDisplayString()),
                SymbolKind.NamedType => symbol.CreateDiagnostic(TypeRule, misspelledWord, symbol.ToDisplayString()),
                SymbolKind.Method or SymbolKind.Property or SymbolKind.Event or SymbolKind.Field
                    => symbol.CreateDiagnostic(MemberRule, misspelledWord, symbol.ToDisplayString()),
                SymbolKind.Parameter => symbol.ContainingType.TypeKind == TypeKind.Delegate
                    ? symbol.CreateDiagnostic(DelegateParameterRule, symbol.ContainingType.ToDisplayString(), misspelledWord, symbol.Name)
                    : symbol.CreateDiagnostic(MemberParameterRule, symbol.ContainingSymbol.ToDisplayString(), misspelledWord, symbol.Name),
                SymbolKind.TypeParameter => symbol.ContainingSymbol.Kind == SymbolKind.Method
                    ? symbol.CreateDiagnostic(MethodTypeParameterRule, symbol.ContainingSymbol.ToDisplayString(), misspelledWord, symbol.Name)
                    : symbol.CreateDiagnostic(TypeTypeParameterRule, symbol.ContainingSymbol.ToDisplayString(), misspelledWord, symbol.Name),
                SymbolKind.Local => symbol.CreateDiagnostic(VariableRule, misspelledWord, symbol.ToDisplayString()),
                _ => throw new NotImplementedException($"Unknown SymbolKind: {symbol.Kind}"),
            };
        }

        private static Diagnostic GetUnmeaningfulIdentifierDiagnostic(ISymbol symbol, string symbolName)
        {
            return symbol.Kind switch
            {
                SymbolKind.Assembly => symbol.CreateDiagnostic(AssemblyMoreMeaningfulNameRule, symbolName),
                SymbolKind.Namespace => symbol.CreateDiagnostic(NamespaceMoreMeaningfulNameRule, symbolName),
                SymbolKind.NamedType => symbol.CreateDiagnostic(TypeMoreMeaningfulNameRule, symbolName),
                SymbolKind.Method or SymbolKind.Property or SymbolKind.Event or SymbolKind.Field
                    => symbol.CreateDiagnostic(MemberMoreMeaningfulNameRule, symbolName),
                SymbolKind.Parameter => symbol.ContainingType.TypeKind == TypeKind.Delegate
                    ? symbol.CreateDiagnostic(DelegateParameterMoreMeaningfulNameRule, symbol.ContainingType.ToDisplayString(), symbolName)
                    : symbol.CreateDiagnostic(MemberParameterMoreMeaningfulNameRule, symbol.ContainingSymbol.ToDisplayString(), symbolName),
                SymbolKind.TypeParameter => symbol.ContainingSymbol.Kind == SymbolKind.Method
                    ? symbol.CreateDiagnostic(MethodTypeParameterMoreMeaningfulNameRule, symbol.ContainingSymbol.ToDisplayString(), symbol.Name)
                    : symbol.CreateDiagnostic(TypeTypeParameterMoreMeaningfulNameRule, symbol.ContainingSymbol.ToDisplayString(), symbol.Name),
                _ => throw new NotImplementedException($"Unknown SymbolKind: {symbol.Kind}"),
            };
        }
    }
}

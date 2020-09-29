using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NationalInstruments.Analyzers.Correctness.StringsShouldBeInResources;
using NationalInstruments.Analyzers.Properties;
using NationalInstruments.Analyzers.Utilities;
using NationalInstruments.Analyzers.Utilities.Extensions;

namespace NationalInstruments.Analyzers.Correctness
{
    /// <summary>
    /// Scopes that NI1004 attributes can apply to.
    /// </summary>
    public enum ExemptionScope
    {
        /// <summary>
        /// The default scope if no scope or an invalid scope is specified.
        /// </summary>
        Unknown = 0,
        Disabled,
        Namespace,
        BaseClass,
        Class,
        Method,
        Constant,
        File,
        Parameter,
    }

    /// <summary>
    /// Analyzer that reports a diagnostic for each string literal that isn't exempt. This prevents unlocalized
    /// messages from being shown to customers.
    /// </summary>
    /// <remarks>
    /// String literals can be exempt in many ways:
    /// - The assembly is exempt
    /// - The file is exempt
    /// - The scope containing the literal is exempt
    /// - The invocation using the literal is exempt
    /// - The literal's value is exempt
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class StringsShouldBeInResourcesAnalyzer : NIDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "NI1004";

        private static readonly LocalizableString LocalizedTitle = new LocalizableResourceString(nameof(Resources.NI1004_Title), Resources.ResourceManager, typeof(Resources));

        public static DiagnosticDescriptor Rule { get; } = new DiagnosticDescriptor(
            DiagnosticId,
            LocalizedTitle,
            new LocalizableResourceString(nameof(Resources.NI1004_Message), Resources.ResourceManager, typeof(Resources)),
            Resources.CategoryNI,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: new LocalizableResourceString(nameof(Resources.NI1004_Description), Resources.ResourceManager, typeof(Resources)),
            helpLinkUri: "https://nitalk.jiveon.com/docs/DOC-234077");

        public static DiagnosticDescriptor FileParseRule { get; } = new DiagnosticDescriptor(
            DiagnosticId,
            LocalizedTitle,
            new LocalizableResourceString(nameof(Resources.ParseError_Message), Resources.ResourceManager, typeof(Resources)),
            Resources.CategoryNI,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor AttributeMissingTargetRule { get; } = new DiagnosticDescriptor(
            DiagnosticId,
            LocalizedTitle,
            new LocalizableResourceString(nameof(Resources.NI1004_AttributeMissingTarget_Message), Resources.ResourceManager, typeof(Resources)),
            Resources.CategoryNI,
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
                var analyzer = new StringLiteralAnalyzer(additionalFileService, compilationStartContext.Compilation, compilationStartContext.CancellationToken);
                analyzer.LoadConfigurations("LiteralExemptions");

                compilationStartContext.RegisterSyntaxTreeAction(analyzer.AnalyzeSyntaxTree);
                compilationStartContext.RegisterCompilationEndAction(additionalFileService.ReportAnyParsingDiagnostics);
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using NationalInstruments.Analyzers.Utilities;
using NationalInstruments.Analyzers.Utilities.Extensions;

namespace NationalInstruments.Analyzers.Correctness.StringsShouldBeInResources
{
    internal class StringLiteralAnalyzer : ConfigurableAnalyzer
    {
        private const string Assembly = "Assembly";
        private const string Attribute = "Attribute";
        private const string Parameter = "Parameter";

        private const string ExemptFromStringLiteralsRuleAttributeName = "ExemptFromStringLiteralsRuleAttribute";
        private const string AllowThisNonLocalizedLiteralAttributeName = "AllowThisNonLocalizedLiteralAttribute";
        private const string AcceptsStringLiteralArgumentsAttributeName = "AcceptsStringLiteralArgumentsAttribute";
        private const string ImplementationAllowedToUseStringLiteralsAttributeName = "ImplementationAllowedToUseStringLiteralsAttribute";
        private const string AllowExternalCodeToAcceptStringLiteralArgumentsAttributeName = "AllowExternalCodeToAcceptStringLiteralArgumentsAttribute";

        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1306:FieldNamesMustBeginWithLowerCaseLetter", Justification = "Effectively constant")]
        private readonly ImmutableArray<string> ExemptionAttributeNames = ImmutableArray.Create(
            ExemptFromStringLiteralsRuleAttributeName,
            AllowThisNonLocalizedLiteralAttributeName,
            AcceptsStringLiteralArgumentsAttributeName,
            ImplementationAllowedToUseStringLiteralsAttributeName,
            AllowExternalCodeToAcceptStringLiteralArgumentsAttributeName);

        // Exemptions only addded from code attributes
        private readonly ExemptionCollection _exemptFieldScopes = new ExemptionCollection();
        private readonly ExemptionCollection _exemptTypeScopes = new ExemptionCollection();
        private readonly ExemptionCollection _exemptMemberScopes = new ExemptionCollection();

        // Exemptions added from file with no attributes
        private readonly ExemptionCollection _exemptFields = new ExemptionCollection();
        private readonly ExemptionCollection _exemptStrings = new ExemptionCollection();
        private readonly ExemptionCollection _exemptFilenames = new ExemptionCollection();
        private readonly ExemptionCollection _exemptAssemblies = new ExemptionCollection();

        // Exemptions added from file with optional attributes
        private readonly ExemptionCollection _exemptTypes = new ExemptionCollection();
        private readonly ExemptionCollection _exemptMembers = new ExemptionCollection();
        private readonly ExemptionCollection _exemptNamespaces = new ExemptionCollection();

        private readonly IAdditionalFileService _additionalFileService;
        private readonly Compilation _compilation;

        /// <summary>
        /// Constructor that's called each compilation.
        /// </summary>
        /// <param name="additionalFileService">Service that allows additional files to be found and parsed.</param>
        /// <param name="compilation">An object containing properties related to the current compilation.</param>
        /// <param name="cancellationToken">Object that indicates if a cancellation was requested or not.</param>
        public StringLiteralAnalyzer(IAdditionalFileService additionalFileService, Compilation compilation, CancellationToken cancellationToken)
            : base(additionalFileService, cancellationToken)
        {
            _additionalFileService = additionalFileService;
            _compilation = compilation;
        }

        private AttributeCollection DefaultAttributes => new AttributeCollection(Tuple.Create(Assembly, _compilation.AssemblyName));

        /// <summary>
        /// Determines if a particular file should be analyzed for string literals and, if it should,
        /// reports each string literal that is not exempt.
        /// </summary>
        /// <remarks>
        /// String literals can be exempt in many ways:
        /// - The assembly is exempt
        /// - The file is exempt
        /// - The scope containing the literal is exempt
        /// - The invocation using the literal is exempt
        /// - The literal's value is exempt
        /// </remarks>
        /// <param name="context">
        /// An object containing the syntax tree and any options for this compilation.
        /// </param>
        public void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            SyntaxNode root = context.Tree.GetRoot(context.CancellationToken);
            SemanticModel semanticModel = _compilation.GetSemanticModel(context.Tree);

            // Bail out if this file is exempt
            var currentFilePath = context.Tree.FilePath;
            if (IsFileExempt(currentFilePath))
            {
                return;
            }

            try
            {
                // Populate exemptions from attributes on the assembly
                AddExemptionsFromAttributes(_compilation.Assembly.GetAttributes(), null, semanticModel);

                // Bail out if the entire assembly is exempt e.g. [assembly: <ExemptAttribute>]
                if (_exemptAssemblies.Contains(_compilation.AssemblyName) || _exemptAssemblies.Matches(_compilation.AssemblyName))
                {
                    return;
                }

                // Populate exemptions from attributes decorating syntax
                AddExemptionsFromAttributes(root.DescendantNodes().OfType<AttributeSyntax>(), semanticModel);

                // Bail out if there was an attribute indicating that this file is exempt
                if (IsFileExempt(currentFilePath))
                {
                    return;
                }

                // Find every string literal and see if it should be reported
                foreach (StringLiteral literal in StringLiteral.GetStringLiterals(root.DescendantNodes()))
                {
                    if (IsLiteralValueExempt(literal.Value))
                    {
                        continue;
                    }

                    if (IsLiteralExemptFromAncestor(literal.Syntax, semanticModel))
                    {
                        continue;
                    }

                    var diagnostic = Diagnostic.Create(StringsShouldBeInResourcesAnalyzer.Rule, literal.Syntax?.GetLocation(), literal.Value);
                    context.ReportDiagnostic(diagnostic);
                }
            }
            catch (AttributeMissingTargetException ex)
            {
                var diagnostic = Diagnostic.Create(
                    StringsShouldBeInResourcesAnalyzer.AttributeMissingTargetRule,
                    Location.None,
                    ex.AttributeName,
                    ex.ScopeName,
                    context.Tree.FilePath);

                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// Populates collections of exemptions from the XML of additional file(s).
        /// </summary>
        /// <param name="rootElement">
        /// Root node of an XML document containing exemptions for fields, string, files, assemblies, types, members, and namespaces.
        /// </param>
        /// <param name="filePath">
        /// Path to an XML file containing exemptions.
        /// </param>
        protected override void LoadConfigurations(XElement rootElement, string filePath)
        {
            if (TryGetRootElementDiagnostic(rootElement, "Exemptions", filePath, StringsShouldBeInResourcesAnalyzer.FileParseRule, out var diagnostic))
            {
                _additionalFileService.ParsingDiagnostics.Add(diagnostic);
            }

            _exemptStrings.UnionWith(rootElement.Elements("String"));
            _exemptFilenames.UnionWith(rootElement.Elements("Filename"));
            _exemptAssemblies.UnionWith(rootElement.Elements("Assembly"));
            _exemptNamespaces.UnionWith(rootElement.Elements("Namespace"));

            LoadScopeAndOrInvocationExemptions(rootElement.Elements("Field"), _exemptFieldScopes, _exemptFields);
            LoadScopeAndOrInvocationExemptions(rootElement.Elements("Type"), _exemptTypeScopes, _exemptTypes);
            LoadScopeAndOrInvocationExemptions(rootElement.Elements("Member"), _exemptMemberScopes, _exemptMembers);
        }

        private static void LoadScopeAndOrInvocationExemptions(
            IEnumerable<XElement> exemptionNodes,
            ExemptionCollection scopeExemptions,
            ExemptionCollection invocationExemptions)
        {
            // Type and member exemptions will, by default, apply to both scopes and invocations.
            // This method allows users to specify an "AppliesTo" attribute to control whether an
            // exemption applies to only one or the other.

            foreach (var exemption in exemptionNodes)
            {
                var appliesAttribute = exemption.Attribute("AppliesTo");
                if (appliesAttribute != null)
                {
                    var attributePairs = exemption.Attributes()
                        .Where(x => x != appliesAttribute)
                        .Select(x => Tuple.Create(x.Name.LocalName, x.Value)).ToArray();

                    AttributeCollection attributes = null;
                    if (attributePairs.Length > 0)
                    {
                        attributes = new AttributeCollection(attributePairs);
                    }

                    if (string.Equals(appliesAttribute.Value, "Scope", StringComparison.OrdinalIgnoreCase))
                    {
                        scopeExemptions.Add(exemption.Value, attributes);
                    }
                    else
                    {
                        invocationExemptions.Add(exemption.Value, attributes);
                    }
                }
                else
                {
                    var exemptions = new[] { exemption };

                    scopeExemptions.UnionWith(exemptions);
                    invocationExemptions.UnionWith(exemptions);
                }
            }
        }

        private static string GetParameterNameForSyntax(ISymbol member, SyntaxNode memberSyntax, SyntaxNode literalSyntax)
        {
            var parameterStartIndex = 0;
            var method = member as IMethodSymbol;
            var property = member as IPropertySymbol;

            if (method == null && property == null)
            {
                return null;
            }

            if (method?.MethodKind == MethodKind.ReducedExtension)
            {
                method = method.ReducedFrom;
                parameterStartIndex = 1;
            }

            var arguments = new List<SyntaxNode>();
            if (memberSyntax is BinaryExpressionSyntax binaryExpressionSyntax)
            {
                arguments.Add(binaryExpressionSyntax.Left);
                arguments.Add(binaryExpressionSyntax.Right);
            }
            else
            {
                arguments = memberSyntax.ChildNodes()
                    .OfType<BaseArgumentListSyntax>()
                    .FirstOrDefault()
                    ?.Arguments
                    .Cast<SyntaxNode>().ToList();
            }

            if (arguments == null || !arguments.Any())
            {
                return null;
            }

            var parameters = method?.Parameters ?? property?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;

            for (var i = 0; i < arguments.Count; ++i)
            {
                var argumentSyntax = arguments[i];
                if (StringLiteral.GetStringLiterals(argumentSyntax.DescendantNodesAndSelf()).Any(x => x.Syntax == literalSyntax))
                {
                    return parameters[Math.Min(i + parameterStartIndex, parameters.Length - 1)].Name;
                }
            }

            return null;
        }

        private bool IsFileExempt(string filePath)
        {
            return _exemptFilenames.Contains(filePath) || _exemptFilenames.Matches(filePath);
        }

        private void AddExemptionsFromAttributes(IEnumerable<AttributeSyntax> attributeSyntaxes, SemanticModel semanticModel)
        {
            foreach (var attributeSyntax in attributeSyntaxes)
            {
                // Append "Attribute" to whatever attribute name is found in the syntax. This may yield "somethingAttributeAttribute",
                // but that's okay because we're testing if "somethingAttributeAttribute".Contains(exemptionAttributeName)
                var attributeName = ExemptionAttributeNames.FirstOrDefault(string.Concat(attributeSyntax.Name, Attribute).Contains);
                if (string.IsNullOrEmpty(attributeName))
                {
                    continue;
                }

                IEnumerable<SyntaxNode> syntaxes = new[] { attributeSyntax.Parent.Parent };

                // Fields can have multiple assignments, but the attribute can only appear on the top-level FieldDeclaration
                if (syntaxes.First() is FieldDeclarationSyntax fieldSyntax)
                {
                    syntaxes = fieldSyntax.Declaration.Variables;
                }

                foreach (var syntax in syntaxes)
                {
                    var symbol = syntax.GetDeclaredOrReferencedSymbol(semanticModel);
                    if (symbol == null)
                    {
                        continue;
                    }

                    AddExemptionsFromAttributes(symbol.GetAttributes(), syntax, semanticModel);
                }
            }
        }

        private void AddExemptionsFromAttributes(IEnumerable<AttributeData> attributes, SyntaxNode decoratedSyntax, SemanticModel semanticModel)
        {
            foreach (var attribute in attributes)
            {
                var attributeName = attribute.AttributeClass.Name;

                if (ExemptionAttributeNames.Contains(attributeName))
                {
                    var exemptionAttribute = new ExemptionAttribute(attribute);

                    switch (exemptionAttribute.Name)
                    {
                        case ExemptFromStringLiteralsRuleAttributeName:
                        case ImplementationAllowedToUseStringLiteralsAttributeName:
                            AddScopeExemption(exemptionAttribute, decoratedSyntax.GetDeclaredOrReferencedSymbol(semanticModel));
                            break;

                        case AcceptsStringLiteralArgumentsAttributeName:
                        case AllowExternalCodeToAcceptStringLiteralArgumentsAttributeName:
                            AddInvocationExemption(exemptionAttribute, decoratedSyntax.GetDeclaredOrReferencedSymbol(semanticModel));
                            break;

                        case AllowThisNonLocalizedLiteralAttributeName:
                            _exemptStrings.Add(exemptionAttribute.Literal, DefaultAttributes);
                            break;
                    }
                }

                if (decoratedSyntax == null
                    && attributeName.Contains("AssemblyMetadata")
                    && attribute.ConstructorArguments[0].Value.ToString() == "Localize.Constant")
                {
                    _exemptStrings.Add(attribute.ConstructorArguments[1].Value.ToString(), DefaultAttributes);
                }
            }
        }

        private void AddScopeExemption(ExemptionAttribute exemptionAttribute, ISymbol decoratedSymbol)
        {
            string target;

            switch (exemptionAttribute.Scope)
            {
                case ExemptionScope.File:
                    target = ExemptionAttribute.GetTargetFromAttribute(exemptionAttribute);
                    if (!Path.IsPathRooted(target))
                    {
                        target = string.Concat("*", target);
                    }

                    _exemptFilenames.Add(target);
                    break;

                case ExemptionScope.Namespace:
                    target = ExemptionAttribute.GetTargetFromAttributeOrSymbol<INamespaceSymbol>(exemptionAttribute, decoratedSymbol);
                    if (!target.EndsWith("*", StringComparison.Ordinal))
                    {
                        target = string.Concat(target, "*");
                    }

                    _exemptNamespaces.Add(target);
                    break;

                case ExemptionScope.Class:
                    target = ExemptionAttribute.GetTargetFromAttributeOrSymbol<INamedTypeSymbol>(exemptionAttribute, decoratedSymbol);
                    _exemptTypeScopes.Add(target);
                    break;

                case ExemptionScope.Constant:
                    target = ExemptionAttribute.GetTargetFromAttribute(exemptionAttribute);
                    _exemptStrings.Add(target, DefaultAttributes);
                    break;

                case ExemptionScope.Disabled:
                case ExemptionScope.Unknown:
                    if (decoratedSymbol == null)
                    {
                        _exemptAssemblies.Add(_compilation.AssemblyName);
                    }
                    else
                    {
                        var decoratedSymbolName = decoratedSymbol.GetFullName();

                        if (decoratedSymbol is IFieldSymbol)
                        {
                            _exemptFieldScopes.Add(decoratedSymbolName);
                        }
                        else if (decoratedSymbol is IPropertySymbol || decoratedSymbol is IMethodSymbol)
                        {
                            _exemptMemberScopes.Add(decoratedSymbolName);
                        }
                        else if (decoratedSymbol is ITypeSymbol)
                        {
                            _exemptTypeScopes.Add(decoratedSymbolName);
                        }
                    }

                    break;
            }
        }

        private void AddInvocationExemption(ExemptionAttribute exemptionAttribute, ISymbol decoratedSymbol)
        {
            string target;
            AttributeCollection attributes = DefaultAttributes;

            switch (exemptionAttribute.Scope)
            {
                case ExemptionScope.Class:
                case ExemptionScope.BaseClass:
                    // Exempts any literal being passed to a member of this class or a class that derives from the base
                    if (decoratedSymbol != null)
                    {
                        // Must be decorating a named type
                        target = ExemptionAttribute.GetTargetFromAttributeOrSymbol<INamedTypeSymbol>(exemptionAttribute, decoratedSymbol);
                        _exemptTypes.Add(target, DefaultAttributes);
                    }
                    else
                    {
                        // Must be an assembly attribute
                        target = ExemptionAttribute.GetTargetFromAttribute(exemptionAttribute);
                        _exemptTypes.Add(target, DefaultAttributes);
                    }

                    break;

                case ExemptionScope.Method:
                    // Exempts any literal being passed to the specified method and optionally, parameter(s)
                    // Note: this can only occur as an assembly attribute
                    var targetParts = ExemptionAttribute.GetTargetFromAttribute(exemptionAttribute)
                        .Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                    target = targetParts[0];
                    if (targetParts.Length > 1)
                    {
                        attributes.Add(Parameter, string.Join("|", targetParts.Skip(1)));
                    }

                    _exemptMembers.Add(target, attributes);
                    break;

                case ExemptionScope.Parameter:
                    // Exempts the specified parameter of the associated method
                    // Note: cannot occur as an assembly attribute
                    if (decoratedSymbol != null)
                    {
                        target = ExemptionAttribute.GetTargetFromAttribute(exemptionAttribute);
                        attributes.Add(Parameter, target);
                        _exemptMembers.Add(decoratedSymbol.GetFullName(), attributes);
                    }

                    break;

                case ExemptionScope.Unknown:
                    if (decoratedSymbol != null)
                    {
                        // Exempts literals being assigned to the decorated field, property, or indexer and exempts literals being used
                        // in the invocation of the decorated method or method within the decorated type
                        var decoratedSymbolName = decoratedSymbol.GetFullName();

                        if (decoratedSymbol is IFieldSymbol)
                        {
                            _exemptFields.Add(decoratedSymbolName, DefaultAttributes);
                        }
                        else if (decoratedSymbol is IPropertySymbol || decoratedSymbol is IMethodSymbol)
                        {
                            if (!string.IsNullOrEmpty(exemptionAttribute.Target))
                            {
                                attributes.Add(Parameter, exemptionAttribute.Target);
                            }

                            _exemptMembers.Add(decoratedSymbolName, attributes);
                        }
                        else if (decoratedSymbol is ITypeSymbol)
                        {
                            _exemptTypes.Add(decoratedSymbolName, DefaultAttributes);
                        }
                    }

                    break;
            }
        }

        private bool IsLiteralValueExempt(string literal)
        {
            if (literal.Length == 1)
            {
                return true;
            }

            if (_exemptStrings.Contains(literal, DefaultAttributes))
            {
                return true;
            }

            var braceStack = new Stack<char>();
            var isOnlyCharactersWeDontCareAbout = true;
            for (var i = 0; isOnlyCharactersWeDontCareAbout && (i < literal.Length); ++i)
            {
                var isOpenBrace = literal[i] == '{';
                var isCloseBrace = literal[i] == '}';

                if (isOpenBrace)
                {
                    braceStack.Push(literal[i]);
                }

                if (isCloseBrace && braceStack.Any())
                {
                    braceStack.Pop();
                }

                isOnlyCharactersWeDontCareAbout = char.IsDigit(literal, i)
                                                  || char.IsWhiteSpace(literal, i)
                                                  || char.IsPunctuation(literal, i)
                                                  || char.IsSeparator(literal, i)
                                                  || char.IsControl(literal, i)
                                                  || char.IsSymbol(literal, i)
                                                  || literal[i] == '\\'
                                                  || isOpenBrace
                                                  || isCloseBrace
                                                  || (braceStack.Count % 2 != 0 && char.IsLetter(literal, i)); // it's a string formatting character
            }

            if (isOnlyCharactersWeDontCareAbout)
            {
                return true;
            }

            if (Guid.TryParse(literal, out var guid))
            {
                return true;
            }

            if (Regex.IsMatch(literal, @"^\s*#[\da-fA-F]{2,8}\s*$"))
            {
                // It's a color hex code e.g. #FF or #AA0033FF
                return true;
            }

            return _exemptStrings.Matches(literal, DefaultAttributes);
        }

        private bool IsLiteralExemptFromAncestor(SyntaxNode literalSyntax, SemanticModel semanticModel)
        {
            foreach (var ancestorSyntax in literalSyntax.Ancestors())
            {
                if (AreLiteralsInAncestorScopeExempt(ancestorSyntax, semanticModel))
                {
                    return true;
                }

                if ((ancestorSyntax is AssignmentExpressionSyntax || ancestorSyntax is ElementAccessExpressionSyntax)
                    && IsLiteralInAssignmentExempt(ancestorSyntax, semanticModel))
                {
                    return true;
                }

                if (ancestorSyntax.IsParameterizedMemberInvocation() && IsLiteralInInvocationExempt(literalSyntax, ancestorSyntax, semanticModel))
                {
                    return true;
                }
            }

            return false;
        }

        private bool AreLiteralsInAncestorScopeExempt(SyntaxNode ancestorSyntax, SemanticModel semanticModel)
        {
            if (ancestorSyntax is AttributeArgumentSyntax)
            {
                return true; // all attribute and parameter literals are exempt
            }

            var getSymbol = new Func<ISymbol>(() => ancestorSyntax.GetDeclaredOrReferencedSymbol(semanticModel));

            if (ancestorSyntax is VariableDeclaratorSyntax && IsSymbolExempt(getSymbol(), _exemptFieldScopes))
            {
                return true;
            }

            if ((ancestorSyntax is AccessorDeclarationSyntax
                 || ancestorSyntax is BasePropertyDeclarationSyntax
                 || ancestorSyntax is BaseMethodDeclarationSyntax)
                && IsSymbolExempt(getSymbol(), _exemptMemberScopes))
            {
                return true;
            }

            if (ancestorSyntax is BaseTypeDeclarationSyntax)
            {
                ISymbol type = getSymbol();
                if (IsSymbolExempt(type, _exemptTypeScopes) || IsSymbolExempt(type?.ContainingNamespace, _exemptNamespaces))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsLiteralInAssignmentExempt(SyntaxNode ancestorSyntax, SemanticModel semanticModel)
        {
            var exempt = false;

            foreach (var childSyntax in ancestorSyntax.DescendantNodesAndSelf())
            {
                var isElementAccessExpressionSyntax = childSyntax is ElementAccessExpressionSyntax;

                // IdentifierNameSyntax: assignment to type's member
                // MemberAccessExpressionSyntax: assignment to another types' member
                // ElementAccessExpressionSyntax: assignment or retrieval of an indexer
                if (childSyntax is IdentifierNameSyntax
                    || childSyntax is MemberAccessExpressionSyntax
                    || isElementAccessExpressionSyntax)
                {
                    var member = childSyntax.GetDeclaredOrReferencedSymbol(semanticModel);
                    if (member == null)
                    {
                        continue;
                    }

                    if (member is IFieldSymbol)
                    {
                        exempt = IsSymbolExempt(member, _exemptFields) || IsMemberInBaseTypeOrInterfaceExempt(member, null);
                    }
                    else if (member is IPropertySymbol property)
                    {
                        if (property.IsIndexer)
                        {
                            // All key literals used in indexer accessors are exempt as they can never be customer facing
                            exempt = true;
                        }
                        else
                        {
                            // Only need to check a property's 'set' accessor because literals can't be used with 'get'
                            exempt = IsSymbolExempt(property.SetMethod, _exemptMembers)
                                     || IsMemberInBaseTypeOrInterfaceExempt(property.SetMethod, null);
                        }
                    }
                    else
                    {
                        exempt = IsSymbolExempt(member, _exemptMembers)
                                 || IsMemberInBaseTypeOrInterfaceExempt(member, null);
                    }
                }

                if (exempt)
                {
                    break;
                }
            }

            return exempt;
        }

        private bool IsLiteralInInvocationExempt(SyntaxNode literalSyntax, SyntaxNode ancestorSyntax, SemanticModel semanticModel)
        {
            var member = ancestorSyntax.GetDeclaredOrReferencedSymbol(semanticModel);
            if (member == null)
            {
                return false;
            }

            var parameterName = GetParameterNameForSyntax(member, ancestorSyntax, literalSyntax);
            var symbolExempt = string.IsNullOrEmpty(parameterName)
                ? IsSymbolExempt(member, _exemptMembers)
                : IsSymbolExempt(member, _exemptMembers, Tuple.Create(Parameter, parameterName));

            return symbolExempt
                   || IsMembersStringLiteralAccepted(member, member, parameterName)
                   || IsMemberInBaseTypeOrInterfaceExempt(member, parameterName);
        }

        private bool IsSymbolExempt(ISymbol symbol, ExemptionCollection exemptions, params Tuple<string, string>[] additionalAttributes)
        {
            if (symbol == null)
            {
                return false;
            }

            var attributes = DefaultAttributes;
            foreach (var additionalAttribute in additionalAttributes)
            {
                attributes.Add(additionalAttribute.Item1, additionalAttribute.Item2);
            }

            var symbolName = symbol.GetFullName();
            if (exemptions.Contains(symbolName, attributes) || exemptions.Matches(symbolName, attributes))
            {
                return true;
            }

            return false;
        }

        private bool IsMemberInBaseTypeOrInterfaceExempt(ISymbol member, string parameterName)
        {
            if (member == null)
            {
                return false;
            }

            if (member is IMethodSymbol method && method.IsOverride)
            {
                if (IsMembersStringLiteralAccepted(method, method.OverriddenMethod, parameterName))
                {
                    return true;
                }
            }

            foreach (var type in member.ContainingType.GetBaseTypesAndThis())
            {
                var symbolExempt = string.IsNullOrEmpty(parameterName)
                    ? IsSymbolExempt(type, _exemptTypes)
                    : IsSymbolExempt(type, _exemptTypes, Tuple.Create(Parameter, parameterName));

                if (symbolExempt || IsMembersStringLiteralAccepted(member, type))
                {
                    return true;
                }

                foreach (var @interface in type.AllInterfaces)
                {
                    if (IsMembersStringLiteralAccepted(member, @interface))
                    {
                        return true;
                    }

                    foreach (var interfaceMember in @interface.GetMembers())
                    {
                        if (type.FindImplementationForInterfaceMember(interfaceMember)?.Equals(member) ?? false)
                        {
                            if (IsMembersStringLiteralAccepted(member, interfaceMember, parameterName))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private bool IsMembersStringLiteralAccepted(ISymbol member, ISymbol baseSymbol, string parameterName = null)
        {
            if (baseSymbol == null)
            {
                return false;
            }

            var acceptsStringLiteralArgumentsAttributes = baseSymbol.GetAttributes()
                .Where(x => AcceptsStringLiteralArgumentsAttributeName.Contains(x.AttributeClass.Name))
                .Select(x => new ExemptionAttribute(x));

            foreach (var attribute in acceptsStringLiteralArgumentsAttributes)
            {
                if (string.IsNullOrEmpty(parameterName))
                {
                    AddInvocationExemption(attribute, baseSymbol);
                    if (IsSymbolExempt(baseSymbol, _exemptTypes))
                    {
                        return true;
                    }
                }
                else
                {
                    AddInvocationExemption(attribute, member);
                    if (IsSymbolExempt(member, _exemptMembers, Tuple.Create(Parameter, parameterName)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

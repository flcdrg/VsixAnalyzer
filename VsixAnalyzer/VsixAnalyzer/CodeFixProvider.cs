using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace VsixAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(VsixAnalyzerCodeFixProvider)), Shared]
    public class VsixAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string title = "Add AllowsBackgroundLoading";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(VsixAnalyzerAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedSolution: c => AllowsBackgroundLoadingAsync(context.Document, declaration, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Solution> AllowsBackgroundLoadingAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            // Credit to Sam Harwell for https://github.com/openstacknetsdk/OpenStackNetAnalyzers/blob/master/OpenStackNetAnalyzers/OpenStackNetAnalyzers/JsonObjectOptInCodeFix.cs 
            // upon which this was based.
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            AttributeSyntax syntax = null;
            foreach (var attributeList in typeDecl.AttributeLists)
            {
                syntax = attributeList.Attributes.FirstOrDefault(
                    a => GetSimpleNameFromNode(a).Identifier.Text.StartsWith("PackageRegistration"));
                if (syntax != null)
                    break;
            }

            if (syntax == null)
                return document.Project.Solution;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            ExpressionSyntax expression =
                    SyntaxFactory.ParseExpression("true")
.WithAdditionalAnnotations(Simplifier.Annotation);

            AttributeSyntax newAttribute = null;
            AttributeArgumentListSyntax argumentList = syntax.ArgumentList;
            if (argumentList != null)
            {
                AttributeArgumentSyntax existingArgument = null;
                foreach (var attributeArgument in argumentList.Arguments)
                {
                    if (string.Equals("AllowsBackgroundLoading", attributeArgument.NameEquals?.Name?.Identifier.ValueText, StringComparison.Ordinal))
                    {
                        existingArgument = attributeArgument;
                        break;
                    }
                }

                if (existingArgument == null)
                {
                    foreach (var attributeArgument in argumentList.Arguments)
                    {
                        // this time only interested in arguments (no NameEquals clause)
                        if (attributeArgument.NameEquals != null)
                            continue;

                        if (string.Equals("AllowsBackgroundLoading", attributeArgument.NameColon?.Name?.Identifier.ValueText, StringComparison.Ordinal))
                        {
                            existingArgument = attributeArgument;
                            break;
                        }

                        if (attributeArgument.NameColon != null)
                            continue;

/*
                        if (IsMemberSerializationArgument(semanticModel, attributeArgument.Expression, context.CancellationToken))
                        {
                            existingArgument = attributeArgument;
                            break;
                        }
*/
                    }
                }

                if (existingArgument != null)
                {
                    var newArgument =
                        existingArgument
                            .WithExpression(expression)
                            .WithTriviaFrom(existingArgument);

                    newAttribute = syntax.ReplaceNode(existingArgument, newArgument);
                }
            }


            if (newAttribute == null)
            {
                if (argumentList == null)
                    argumentList = SyntaxFactory.AttributeArgumentList();

                NameEqualsSyntax nameEquals;
                //if (argumentList.Arguments.Any(argument => argument.NameEquals == null))
                    nameEquals = SyntaxFactory.NameEquals("AllowsBackgroundLoading");
                //else
                //    nameEquals = null;

                AttributeArgumentSyntax defaultValueArgument = SyntaxFactory.AttributeArgument(nameEquals, null, expression);

                argumentList = argumentList.AddArguments(defaultValueArgument);
                newAttribute = syntax.WithArgumentList(argumentList);
            }

            SyntaxNode newRoot = root.ReplaceNode(syntax, newAttribute);
            Document newDocument = document.WithSyntaxRoot(newRoot);
            //var identifierToken = typeDecl.Identifier;
            //var newName = identifierToken.Text.ToUpperInvariant();

/*            // Get the symbol representing the type to be renamed.
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

            // Produce a new solution that has all references to that type renamed, including the declaration.
            var originalSolution = document.Project.Solution;
            var optionSet = originalSolution.Workspace.Options;
            var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken).ConfigureAwait(false);

            // Return the new solution with the now-uppercase type name.
            return newSolution;*/

            return newDocument.Project.Solution;
        }

        private static SimpleNameSyntax GetSimpleNameFromNode(AttributeSyntax node)
        {
            var identifierNameSyntax = node.Name as IdentifierNameSyntax;
            var qualifiedNameSyntax = node.Name as QualifiedNameSyntax;

            return
                identifierNameSyntax
                ??
                qualifiedNameSyntax?.Right
                ??
                (node.Name as AliasQualifiedNameSyntax).Name;
        }
    }
}
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeFixCli.Services;

public class CodeModifier
{
    public async Task<Solution> ApplyCodeChanges(Solution solution)
    {
        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                var syntaxTree = await document.GetSyntaxTreeAsync();
                if (syntaxTree == null) continue;

                var root = await syntaxTree.GetRootAsync();
                var rewriter = new MethodRenamerRewriter();
                var newRoot = rewriter.Visit(root);

                solution = solution.WithDocumentSyntaxRoot(document.Id, newRoot);
            }
        }
        return solution;
    }

    private class MethodRenamerRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.Identifier.Text.StartsWith("Old"))
            {
                var newIdentifier = SyntaxFactory.Identifier("New" + node.Identifier.Text.Substring(3));
                node = node.WithIdentifier(newIdentifier);
            }
            return base.VisitMethodDeclaration(node);
        }
    }
}
using Microsoft.CodeAnalysis;

namespace CodeFixCli.Extensions
{

    public static class SyntaxNodeExtensions
    {
        public static bool IsPublic(this SyntaxNode node)
        {
            // Example: Check for public methods
            return node.ToString().Contains("public");
        }
    }

}

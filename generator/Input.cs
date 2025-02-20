using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Rendering.Generator
{
    public class Input
    {
        public readonly TypeDeclarationSyntax typeDeclaration;
        public readonly ITypeSymbol typeSymbol;
        public readonly string? containingNamespace;
        public readonly string typeName;
        public readonly string fullTypeName;

        public Input(TypeDeclarationSyntax typeDeclaration, ITypeSymbol typeSymbol)
        {
            this.typeDeclaration = typeDeclaration;
            this.typeSymbol = typeSymbol;
            containingNamespace = typeSymbol.ContainingNamespace?.ToDisplayString();
            typeName = typeSymbol.Name;
            fullTypeName = typeSymbol.ToDisplayString();
        }
    }
}
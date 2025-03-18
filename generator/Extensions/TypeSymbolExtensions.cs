using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Rendering.Systems
{
    public static class TypeSymbolExtensions
    {
        //todo: tried to use a pool for the stack but for some god forsaken reason it just wouldnt not work
        //either the pool would be null, or the stack rented would be null, or elements in the stack would be null
        //i gave up

        /// <summary>
        /// Checks if the type is a true value type and doesnt contain any references.
        /// </summary>
        public static bool IsUnmanaged(this ITypeSymbol type)
        {
            if (type.IsReferenceType || type.IsRefLikeType)
            {
                return false;
            }

            //check if the entire type is a true value type and doesnt contain references
            Stack<ITypeSymbol> stack = new();
            stack.Push(type);

            while (stack.Count > 0)
            {
                ITypeSymbol current = stack.Pop();
                if (current.IsReferenceType)
                {
                    return false;
                }

                foreach (IFieldSymbol field in GetFields(current))
                {
                    stack.Push(field.Type);
                }
            }

            return true;
        }

        /// <summary>
        /// Iterates through all fields declared by the type.
        /// </summary>
        public static IEnumerable<IFieldSymbol> GetFields(this ITypeSymbol type)
        {
            foreach (ISymbol typeMember in type.GetMembers())
            {
                if (typeMember is IFieldSymbol field)
                {
                    if (field.HasConstantValue || field.IsStatic)
                    {
                        continue;
                    }

                    yield return field;
                }
            }
        }

        public static IEnumerable<IMethodSymbol> GetMethods(this ITypeSymbol type)
        {
            foreach (ISymbol typeMember in type.GetMembers())
            {
                if (typeMember is IMethodSymbol method)
                {
                    if (method.MethodKind == MethodKind.Constructor)
                    {
                        continue;
                    }

                    yield return method;
                }
            }
        }

        public static IEnumerable<IMethodSymbol> GetConstructors(this ITypeSymbol type)
        {
            foreach (ISymbol typeMember in type.GetMembers())
            {
                if (typeMember is IMethodSymbol method)
                {
                    if (method.MethodKind == MethodKind.Constructor)
                    {
                        yield return method;
                    }
                }
            }
        }

        public static string GetFullTypeName(this ITypeSymbol symbol)
        {
            SpecialType special = symbol.SpecialType;
            if (special == SpecialType.System_Boolean)
            {
                return "System.Boolean";
            }
            else if (special == SpecialType.System_Byte)
            {
                return "System.Byte";
            }
            else if (special == SpecialType.System_SByte)
            {
                return "System.SByte";
            }
            else if (special == SpecialType.System_Int16)
            {
                return "System.Int16";
            }
            else if (special == SpecialType.System_UInt16)
            {
                return "System.UInt16";
            }
            else if (special == SpecialType.System_Int32)
            {
                return "System.Int32";
            }
            else if (special == SpecialType.System_UInt32)
            {
                return "System.UInt32";
            }
            else if (special == SpecialType.System_Int64)
            {
                return "System.Int64";
            }
            else if (special == SpecialType.System_UInt64)
            {
                return "System.UInt64";
            }
            else if (special == SpecialType.System_Single)
            {
                return "System.Single";
            }
            else if (special == SpecialType.System_Double)
            {
                return "System.Double";
            }
            else if (special == SpecialType.System_Decimal)
            {
                return "System.Decimal";
            }
            else if (special == SpecialType.System_Char)
            {
                return "System.Char";
            }
            else if (special == SpecialType.System_IntPtr)
            {
                return "System.IntPtr";
            }
            else if (special == SpecialType.System_UIntPtr)
            {
                return "System.UIntPtr";
            }
            else
            {
                return symbol.ToDisplayString();
            }
        }

        /// <summary>
        /// Checks if the type contains an attribute with the given <paramref name="fullAttributeName"/>.
        /// </summary>
        public static bool HasAttribute(this ISymbol symbol, string fullAttributeName)
        {
            Stack<ITypeSymbol> stack = new();
            foreach (AttributeData attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass is INamedTypeSymbol attributeType)
                {
                    stack.Push(attributeType);
                }
            }

            while (stack.Count > 0)
            {
                ITypeSymbol current = stack.Pop();
                if (fullAttributeName == current.ToDisplayString())
                {
                    return true;
                }
                else
                {
                    if (current.BaseType is INamedTypeSymbol currentBaseType)
                    {
                        stack.Push(currentBaseType);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the type implements the <typeparamref name="T"/> interface.
        /// </summary>
        public static bool HasInterface<T>(this ITypeSymbol type) where T : class
        {
            string? fullInterfaceName = typeof(T)?.FullName;
            if (fullInterfaceName is null)
            {
                throw new InvalidOperationException("Type name is null when checking interface");
            }

            return type.HasInterface(fullInterfaceName);
        }

        /// <summary>
        /// Checks if the type implements the interface with the given <paramref name="fullInterfaceName"/>.
        /// </summary>
        public static bool HasInterface(this ITypeSymbol type, string fullInterfaceName)
        {
            Stack<ITypeSymbol> stack = new();
            foreach (ITypeSymbol interfaceType in type.AllInterfaces)
            {
                stack.Push(interfaceType);
            }

            while (stack.Count > 0)
            {
                ITypeSymbol current = stack.Pop();
                if (current.ToDisplayString() == fullInterfaceName)
                {
                    return true;
                }
                else
                {
                    foreach (ITypeSymbol interfaceType in current.AllInterfaces)
                    {
                        stack.Push(interfaceType);
                    }

                    if (current.BaseType is INamedTypeSymbol currentBaseType)
                    {
                        stack.Push(currentBaseType);
                    }
                }
            }

            return false;
        }
    }
}
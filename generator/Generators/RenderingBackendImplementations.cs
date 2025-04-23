using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;

namespace Rendering.Systems.Generators
{
    [Generator(LanguageNames.CSharp)]
    public class RenderingBackendImplementations : IIncrementalGenerator
    {
        private const string RenderingBackendInterface = "IRenderingBackend";
        private const string FullRenderingBackendInterface = $"Rendering.{RenderingBackendInterface}";
        private const string MemoryAddressType = "Unmanaged.MemoryAddress";

        void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<Input?> filter = context.SyntaxProvider.CreateSyntaxProvider(Predicate, Transform);
            context.RegisterSourceOutput(filter, Generate);
        }

        private static bool Predicate(SyntaxNode node, CancellationToken token)
        {
            return node.IsKind(SyntaxKind.StructDeclaration);
        }

        private static Input? Transform(GeneratorSyntaxContext context, CancellationToken token)
        {
            if (context.Node is TypeDeclarationSyntax typeDeclaration)
            {
                if (context.SemanticModel.GetDeclaredSymbol(typeDeclaration) is ITypeSymbol typeSymbol)
                {
                    if (typeSymbol.HasInterface(FullRenderingBackendInterface))
                    {
                        return new Input(typeDeclaration, typeSymbol);
                    }
                }
            }

            return null;
        }

        private static void Generate(SourceProductionContext context, Input? input)
        {
            if (input is not null)
            {
                context.AddSource($"{input.fullTypeName}.generated.cs", Generate(input));
            }
        }

        internal static string Generate(Input input)
        {
            SourceBuilder source = new();
            source.AppendLine("#pragma warning disable CS0465 //gc isnt even used");
            source.AppendLine("using System;");
            source.AppendLine("using Unmanaged;");
            source.AppendLine("using Worlds;");
            source.AppendLine("using Simulation;");
            source.AppendLine("using Rendering;");
            source.AppendLine("using Rendering.Systems;");
            source.AppendLine("using Rendering.Functions;");
            source.AppendLine("using System.Runtime.InteropServices;");
            source.AppendLine("using System.ComponentModel;");
            source.AppendLine("using System.Numerics;");
            source.AppendLine();

            if (input.containingNamespace is not null)
            {
                source.Append("namespace ");
                source.Append(input.containingNamespace);
                source.AppendLine();
                source.BeginGroup();
            }

            if (input.typeSymbol.IsReadOnly)
            {
                source.AppendLine("public unsafe readonly partial struct ");
            }
            else
            {
                source.AppendLine("public unsafe partial struct ");
            }

            source.Append(input.typeName);
            source.AppendLine();

            source.BeginGroup();
            {
                source.Append("readonly StartFunction ");
                source.Append(RenderingBackendInterface);
                source.Append(".StartFunction");
                source.AppendLine();

                source.BeginGroup();
                {
                    source.AppendLine("get");
                    source.BeginGroup();
                    {
                        source.AppendLine("return new(&Start);");
                        source.AppendLine();
                        source.AppendLine("[UnmanagedCallersOnly]");
                        
                        source.Append("static void Start(");
                        source.Append(MemoryAddressType);
                        source.Append(" backend)");
                        source.AppendLine();

                        source.BeginGroup();
                        {
                            source.AppendLine($"ref {input.typeName} rendererBackend = ref backend.Read<{input.typeName}>();");
                            source.AppendLine("RenderingBackendExtensions.PerformStart(ref rendererBackend);");
                        }
                        source.EndGroup();
                    }
                    source.EndGroup();
                }
                source.EndGroup();
                source.AppendLine();

                source.Append("readonly FinishFunction ");
                source.Append(RenderingBackendInterface);
                source.Append(".FinishFunction");
                source.AppendLine();

                source.BeginGroup();
                {
                    source.AppendLine("get");
                    source.BeginGroup();
                    {
                        source.AppendLine("return new(&Finish);");
                        source.AppendLine();
                        source.AppendLine("[UnmanagedCallersOnly]");

                        source.Append("static void Finish(");
                        source.Append(MemoryAddressType);
                        source.Append(" backend)");
                        source.AppendLine();

                        source.BeginGroup();
                        {
                            source.AppendLine($"ref {input.typeName} rendererBackend = ref backend.Read<{input.typeName}>();");
                            source.AppendLine("RenderingBackendExtensions.PerformFinish(ref rendererBackend);");
                        }
                        source.EndGroup();
                    }
                    source.EndGroup();
                }
                source.EndGroup();
                source.AppendLine();

                source.Append("readonly Create ");
                source.Append(RenderingBackendInterface);
                source.Append(".CreateFunction");
                source.AppendLine();

                source.BeginGroup();
                {
                    source.AppendLine("get");
                    source.BeginGroup();
                    {
                        source.AppendLine("return new(&Create);");
                        source.AppendLine();
                        source.AppendLine("[UnmanagedCallersOnly]");
                        source.AppendLine("static Create.Output Create(Create.Input input)");
                        source.BeginGroup();
                        {
                            source.AppendLine($"ref {input.typeName} rendererBackend = ref input.backend.Read<{input.typeName}>();");
                            source.AppendLine("return RenderingBackendExtensions.Create(ref rendererBackend, input);");
                        }
                        source.EndGroup();
                    }
                    source.EndGroup();
                }
                source.EndGroup();
                source.AppendLine();

                source.Append("readonly Dispose ");
                source.Append(RenderingBackendInterface);
                source.Append(".DisposeFunction");
                source.AppendLine();

                source.BeginGroup();
                {
                    source.AppendLine("get");
                    source.BeginGroup();
                    {
                        source.AppendLine("return new(&Dispose);");
                        source.AppendLine();
                        source.AppendLine("[UnmanagedCallersOnly]");

                        source.Append("static void Dispose(Dispose.Input input)");
                        source.AppendLine();

                        source.BeginGroup();
                        {
                            source.AppendLine($"ref {input.typeName} rendererBackend = ref input.backend.Read<{input.typeName}>();");
                            source.AppendLine("RenderingBackendExtensions.Dispose(ref rendererBackend, input);");
                        }
                        source.EndGroup();
                    }
                    source.EndGroup();
                }
                source.EndGroup();
                source.AppendLine();

                source.Append("readonly SurfaceCreated ");
                source.Append(RenderingBackendInterface);
                source.Append(".SurfaceCreatedFunction");
                source.AppendLine();

                source.BeginGroup();
                {
                    source.AppendLine("get");
                    source.BeginGroup();
                    {
                        source.AppendLine("return new(&SurfaceCreated);");
                        source.AppendLine();
                        source.AppendLine("[UnmanagedCallersOnly]");

                        source.Append("static void SurfaceCreated(");
                        source.Append(MemoryAddressType);
                        source.Append(" backend, ");
                        source.Append(MemoryAddressType);
                        source.Append(" renderer, ");
                        source.Append(MemoryAddressType);
                        source.Append(" surface)");
                        source.AppendLine();

                        source.BeginGroup();
                        {
                            source.AppendLine($"ref {input.typeName} rendererBackend = ref backend.Read<{input.typeName}>();");
                            source.AppendLine("RenderingBackendExtensions.SurfaceCreated(ref rendererBackend, renderer, surface);");
                        }
                        source.EndGroup();
                    }
                    source.EndGroup();
                }
                source.EndGroup();
                source.AppendLine();

                source.Append("readonly BeginRender ");
                source.Append(RenderingBackendInterface);
                source.Append(".BeginRenderFunction");
                source.AppendLine();

                source.BeginGroup();
                {
                    source.AppendLine("get");
                    source.BeginGroup();
                    {
                        source.AppendLine("return new(&BeginRender);");
                        source.AppendLine();
                        source.AppendLine("[UnmanagedCallersOnly]");
                        
                        source.Append("static StatusCode BeginRender(");
                        source.Append(MemoryAddressType);
                        source.Append(" backend, ");
                        source.Append(MemoryAddressType);
                        source.Append(" renderer, Vector4 clearColor)");
                        source.AppendLine();

                        source.BeginGroup();
                        {
                            source.AppendLine($"ref {input.typeName} rendererBackend = ref backend.Read<{input.typeName}>();");
                            source.AppendLine("return RenderingBackendExtensions.BeginRender(ref rendererBackend, renderer, clearColor);");
                        }
                        source.EndGroup();
                    }
                    source.EndGroup();
                }
                source.EndGroup();
                source.AppendLine();

                source.Append("readonly Render ");
                source.Append(RenderingBackendInterface);
                source.Append(".RenderFunction");
                source.AppendLine();

                source.BeginGroup();
                {
                    source.AppendLine("get");
                    source.BeginGroup();
                    {
                        source.AppendLine("return new(&Render);");
                        source.AppendLine();
                        source.AppendLine("[UnmanagedCallersOnly]");
                        source.AppendLine("static void Render(Render.Input input)");
                        source.BeginGroup();
                        {
                            source.AppendLine($"ref {input.typeName} rendererBackend = ref input.backend.Read<{input.typeName}>();");
                            source.AppendLine("RenderingBackendExtensions.Render(ref rendererBackend, input);");
                        }
                        source.EndGroup();
                    }
                    source.EndGroup();
                }
                source.EndGroup();
                source.AppendLine();

                source.Append("readonly EndRender ");
                source.Append(RenderingBackendInterface);
                source.Append(".EndRenderFunction");
                source.AppendLine();

                source.BeginGroup();
                {
                    source.AppendLine("get");
                    source.BeginGroup();
                    {
                        source.AppendLine("return new(&EndRender);");
                        source.AppendLine();
                        source.AppendLine("[UnmanagedCallersOnly]");

                        source.Append("static void EndRender(");
                        source.Append(MemoryAddressType);
                        source.Append(" backend, ");
                        source.Append(MemoryAddressType);
                        source.Append(" renderer)");
                        source.AppendLine();

                        source.BeginGroup();
                        {
                            source.AppendLine($"ref {input.typeName} rendererBackend = ref backend.Read<{input.typeName}>();");
                            source.AppendLine("RenderingBackendExtensions.EndRender(ref rendererBackend, renderer);");
                        }
                        source.EndGroup();
                    }
                    source.EndGroup();
                }
                source.EndGroup();
            }
            source.EndGroup();

            if (input.containingNamespace is not null)
            {
                source.EndGroup();
            }

            return source.ToString();
        }
    }
}
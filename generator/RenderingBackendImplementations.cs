using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Threading;
using Types;

namespace Rendering.Generator
{
    [Generator(LanguageNames.CSharp)]
    public class RenderingBackendImplementations : IIncrementalGenerator
    {
        private static readonly SourceBuilder source = new();

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
                    ImmutableArray<INamedTypeSymbol> interfaces = typeSymbol.AllInterfaces;
                    foreach (INamedTypeSymbol interfaceSymbol in interfaces)
                    {
                        if (interfaceSymbol.ToDisplayString() == Shared.RenderingBackendInterfaceName)
                        {
                            return new Input(typeDeclaration, typeSymbol);
                        }
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

        public static string Generate(Input input)
        {
            source.Clear();
            source.AppendLine("#pragma warning disable CS0465 //gc isnt even used");
            source.AppendLine("using System;");
            source.AppendLine("using Unmanaged;");
            source.AppendLine("using Worlds;");
            source.AppendLine("using Simulation;");
            source.AppendLine("using Rendering;");
            source.AppendLine("using Rendering.Functions;");
            source.AppendLine("using System.Runtime.InteropServices;");
            source.AppendLine("using System.ComponentModel;");
            source.AppendLine("using System.Numerics;");
            source.AppendLine();
            source.AppendLine($"namespace {input.containingNamespace}");
            source.BeginGroup();
            {
                source.AppendLine("[EditorBrowsable(EditorBrowsableState.Never)]");
                if (input.typeSymbol.IsReadOnly)
                {
                    source.AppendLine($"public unsafe readonly partial struct {input.typeName}");
                }
                else
                {
                    source.AppendLine($"public unsafe partial struct {input.typeName}");
                }

                source.BeginGroup();
                {
                    source.AppendLine("readonly Initialize IRenderingBackend.InitializeFunction");
                    source.BeginGroup();
                    {
                        source.AppendLine("get");
                        source.BeginGroup();
                        {
                            source.AppendLine("return new(&Initialize);");
                            source.AppendLine();
                            source.AppendLine("[UnmanagedCallersOnly]");
                            source.AppendLine("static void Initialize(Allocation backend)");
                            source.BeginGroup();
                            {
                                source.AppendLine($"ref {input.typeName} rendererBackend = ref backend.Read<{input.typeName}>();");
                                source.AppendLine("rendererBackend.Initialize();");
                            }
                            source.EndGroup();
                        }
                        source.EndGroup();
                    }
                    source.EndGroup();
                    source.AppendLine();
                    source.AppendLine("readonly Finalize IRenderingBackend.FinalizeFunction");
                    source.BeginGroup();
                    {
                        source.AppendLine("get");
                        source.BeginGroup();
                        {
                            source.AppendLine("return new(&Finalize);");
                            source.AppendLine();
                            source.AppendLine("[UnmanagedCallersOnly]");
                            source.AppendLine("static void Finalize(Allocation backend)");
                            source.BeginGroup();
                            {
                                source.AppendLine($"ref {input.typeName} rendererBackend = ref backend.Read<{input.typeName}>();");
                                source.AppendLine("rendererBackend.PerformFinalize();");
                            }
                            source.EndGroup();
                        }
                        source.EndGroup();
                    }
                    source.EndGroup();
                    source.AppendLine();
                    source.AppendLine("readonly Create IRenderingBackend.CreateFunction");
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
                                source.AppendLine("(Allocation renderer, Allocation instance) = rendererBackend.Create(input.destination, input.ExtensionNames);");
                                source.AppendLine("return new(renderer, instance);");
                            }
                            source.EndGroup();
                        }
                        source.EndGroup();
                    }
                    source.EndGroup();
                    source.AppendLine();
                    source.AppendLine("readonly Dispose IRenderingBackend.DisposeFunction");
                    source.BeginGroup();
                    {
                        source.AppendLine("get");
                        source.BeginGroup();
                        {
                            source.AppendLine("return new(&Dispose);");
                            source.AppendLine();
                            source.AppendLine("[UnmanagedCallersOnly]");
                            source.AppendLine("static void Dispose(Allocation backend, Allocation renderer)");
                            source.BeginGroup();
                            {
                                source.AppendLine($"ref {input.typeName} rendererBackend = ref backend.Read<{input.typeName}>();");
                                source.AppendLine("rendererBackend.Dispose(renderer);");
                            }
                            source.EndGroup();
                        }
                        source.EndGroup();
                    }
                    source.EndGroup();
                    source.AppendLine();
                    source.AppendLine("readonly SurfaceCreated IRenderingBackend.SurfaceCreatedFunction");
                    source.BeginGroup();
                    {
                        source.AppendLine("get");
                        source.BeginGroup();
                        {
                            source.AppendLine("return new(&SurfaceCreated);");
                            source.AppendLine();
                            source.AppendLine("[UnmanagedCallersOnly]");
                            source.AppendLine("static void SurfaceCreated(Allocation backend, Allocation renderer, Allocation surface)");
                            source.BeginGroup();
                            {
                                source.AppendLine($"ref {input.typeName} rendererBackend = ref backend.Read<{input.typeName}>();");
                                source.AppendLine("rendererBackend.SurfaceCreated(renderer, surface);");
                            }
                            source.EndGroup();
                        }
                        source.EndGroup();
                    }
                    source.EndGroup();
                    source.AppendLine();
                    source.AppendLine("readonly BeginRender IRenderingBackend.BeginRenderFunction");
                    source.BeginGroup();
                    {
                        source.AppendLine("get");
                        source.BeginGroup();
                        {
                            source.AppendLine("return new(&BeginRender);");
                            source.AppendLine();
                            source.AppendLine("[UnmanagedCallersOnly]");
                            source.AppendLine("static StatusCode BeginRender(Allocation backend, Allocation renderer, Vector4 clearColor)");
                            source.BeginGroup();
                            {
                                source.AppendLine($"ref {input.typeName} rendererBackend = ref backend.Read<{input.typeName}>();");
                                source.AppendLine("return rendererBackend.BeginRender(renderer, clearColor);");
                            }
                            source.EndGroup();
                        }
                        source.EndGroup();
                    }
                    source.EndGroup();
                    source.AppendLine();
                    source.AppendLine("readonly Render IRenderingBackend.RenderFunction");
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
                                source.AppendLine("rendererBackend.Render(input.renderer, input.Entities, input.material, input.mesh, input.vertexShader, input.fragmentShader);");
                            }
                            source.EndGroup();
                        }
                        source.EndGroup();
                    }
                    source.EndGroup();
                    source.AppendLine();
                    source.AppendLine("readonly EndRender IRenderingBackend.EndRenderFunction");
                    source.BeginGroup();
                    {
                        source.AppendLine("get");
                        source.BeginGroup();
                        {
                            source.AppendLine("return new(&EndRender);");
                            source.AppendLine();
                            source.AppendLine("[UnmanagedCallersOnly]");
                            source.AppendLine("static void EndRender(Allocation backend, Allocation renderer)");
                            source.BeginGroup();
                            {
                                source.AppendLine($"ref {input.typeName} rendererBackend = ref backend.Read<{input.typeName}>();");
                                source.AppendLine("rendererBackend.EndRender(renderer);");
                            }
                            source.EndGroup();
                        }
                        source.EndGroup();
                    }
                    source.EndGroup();
                }
                source.EndGroup();
            }
            source.EndGroup();
            return source.ToString();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using REBUSS.VSexy.Model;
using REBUSS.VSexy.Model.Enums;

namespace REBUSS.VSexy.ClassDiagram
{
    public class ProjectAnalyzer
    {
        public async Task<List<RTypeInfo>> AnalyzeProjectAsync(Project project)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            var types = new List<RTypeInfo>();
            var compilation = await project.GetCompilationAsync();

            if (compilation == null)
                return types;

            foreach (var document in project.Documents)
            {
                try
                {
                    var documentTypes = await AnalyzeDocumentAsync(document, compilation);
                    types.AddRange(documentTypes);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error analyzing document {document.Name}: {ex.Message}");
                }
            }

            return types;
        }

        public async Task<List<RTypeInfo>> AnalyzeDocumentAsync(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var compilation = await document.Project.GetCompilationAsync();
            if (compilation == null)
                return new List<RTypeInfo>();

            return await AnalyzeDocumentAsync(document, compilation);
        }

        private async Task<List<RTypeInfo>> AnalyzeDocumentAsync(Document document, Compilation compilation)
        {
            var types = new List<RTypeInfo>();

            var syntaxTree = await document.GetSyntaxTreeAsync();
            if (syntaxTree == null)
                return types;

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = await syntaxTree.GetRootAsync();

            var typeDeclarations = root.DescendantNodes()
                .Where(n => n is ClassDeclarationSyntax || 
                           n is InterfaceDeclarationSyntax || 
                           n is EnumDeclarationSyntax ||
                           n is RecordDeclarationSyntax ||
                           n is StructDeclarationSyntax);

            foreach (var typeDeclaration in typeDeclarations)
            {
                try
                {
                    var typeInfo = AnalyzeType(typeDeclaration, semanticModel);
                    if (typeInfo != null)
                    {
                        types.Add(typeInfo);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error analyzing type: {ex.Message}");
                }
            }

            return types;
        }

        private RTypeInfo AnalyzeType(SyntaxNode typeDeclaration, SemanticModel semanticModel)
        {
            var symbol = semanticModel.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol;
            if (symbol == null)
                return null;

            var typeInfo = new RTypeInfo
            {
                Name = symbol.Name,
                FullName = symbol.ToDisplayString(),
                Namespace = symbol.ContainingNamespace?.ToDisplayString()
            };

            if (typeDeclaration is ClassDeclarationSyntax)
            {
                typeInfo.Kind = RTypeKind.Class;
                typeInfo.IsAbstract = symbol.IsAbstract;
                typeInfo.IsSealed = symbol.IsSealed;
                typeInfo.IsStatic = symbol.IsStatic;
            }
            else if (typeDeclaration is InterfaceDeclarationSyntax)
            {
                typeInfo.Kind = RTypeKind.Interface;
            }
            else if (typeDeclaration is EnumDeclarationSyntax)
            {
                typeInfo.Kind = RTypeKind.Enum;
            }
            else if (typeDeclaration is RecordDeclarationSyntax)
            {
                typeInfo.Kind = RTypeKind.Record;
            }
            else if (typeDeclaration is StructDeclarationSyntax)
            {
                typeInfo.Kind = RTypeKind.Struct;
            }

            if (symbol.BaseType != null && symbol.BaseType.SpecialType != SpecialType.System_Object)
            {
                typeInfo.BaseType = symbol.BaseType.Name;
                typeInfo.BaseTypeFullName = symbol.BaseType.ToDisplayString();
            }

            foreach (var iface in symbol.Interfaces)
            {
                typeInfo.Interfaces.Add(new InterfaceReference
                {
                    Name = iface.Name,
                    FullName = iface.ToDisplayString()
                });
            }

            foreach (var member in symbol.GetMembers())
            {
                try
                {
                    switch (member.Kind)
                    {
                        case SymbolKind.Property:
                            var property = member as IPropertySymbol;
                            if (property != null && !property.IsImplicitlyDeclared)
                            {
                                typeInfo.Properties.Add(AnalyzeProperty(property));
                            }
                            break;

                        case SymbolKind.Method:
                            var method = member as IMethodSymbol;
                            if (method != null && !method.IsImplicitlyDeclared && 
                                method.MethodKind == MethodKind.Ordinary)
                            {
                                typeInfo.Methods.Add(AnalyzeMethod(method));
                            }
                            break;

                        case SymbolKind.Field:
                            var field = member as IFieldSymbol;
                            if (field != null && !field.IsImplicitlyDeclared)
                            {
                                if (typeInfo.Kind == RTypeKind.Enum)
                                {
                                    typeInfo.EnumMembers.Add(field.Name);
                                }
                                else
                                {
                                    typeInfo.Fields.Add(AnalyzeField(field));
                                }
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error analyzing member {member.Name}: {ex.Message}");
                }
            }

            return typeInfo;
        }

        private PropertyMemberInfo AnalyzeProperty(IPropertySymbol property)
        {
            return new PropertyMemberInfo
            {
                Name = property.Name,
                Type = property.Type.Name,
                TypeFullName = property.Type.ToDisplayString(),
                Accessibility = GetAccessibility(property.DeclaredAccessibility),
                IsStatic = property.IsStatic,
                IsReadOnly = property.IsReadOnly,
                IsVirtual = property.IsVirtual,
                IsOverride = property.IsOverride,
                IsAbstract = property.IsAbstract,
                HasGetter = property.GetMethod != null,
                HasSetter = property.SetMethod != null
            };
        }

        private MethodMemberInfo AnalyzeMethod(IMethodSymbol method)
        {
            var methodInfo = new MethodMemberInfo
            {
                Name = method.Name,
                ReturnType = method.ReturnType.Name,
                ReturnTypeFullName = method.ReturnType.ToDisplayString(),
                Accessibility = GetAccessibility(method.DeclaredAccessibility),
                IsStatic = method.IsStatic,
                IsVirtual = method.IsVirtual,
                IsOverride = method.IsOverride,
                IsAbstract = method.IsAbstract,
                IsAsync = method.IsAsync
            };

            // Parametry
            foreach (var parameter in method.Parameters)
            {
                methodInfo.Parameters.Add(new ParameterInfo
                {
                    Name = parameter.Name,
                    Type = parameter.Type.Name,
                    TypeFullName = parameter.Type.ToDisplayString(),
                    IsOptional = parameter.IsOptional,
                    HasDefaultValue = parameter.HasExplicitDefaultValue,
                    DefaultValue = parameter.HasExplicitDefaultValue ? parameter.ExplicitDefaultValue?.ToString() : null
                });
            }

            return methodInfo;
        }

        private FieldMemberInfo AnalyzeField(IFieldSymbol field)
        {
            return new FieldMemberInfo
            {
                Name = field.Name,
                Type = field.Type.Name,
                TypeFullName = field.Type.ToDisplayString(),
                Accessibility = GetAccessibility(field.DeclaredAccessibility),
                IsStatic = field.IsStatic,
                IsReadOnly = field.IsReadOnly,
                IsConst = field.IsConst,
                ConstantValue = field.HasConstantValue ? field.ConstantValue?.ToString() : null
            };
        }

        private AccessibilityLevel GetAccessibility(Accessibility accessibility)
        {
            switch(accessibility)
            {
                case Accessibility.Public: return AccessibilityLevel.Public;
                case Accessibility.Private: return AccessibilityLevel.Private;
                case Accessibility.Protected: return AccessibilityLevel.Protected;
                case Accessibility.Internal: return AccessibilityLevel.Internal;
                case Accessibility.ProtectedOrInternal: return AccessibilityLevel.ProtectedInternal;
                case Accessibility.ProtectedAndInternal: return AccessibilityLevel.PrivateProtected;
                default: return AccessibilityLevel.Private;
            }
        }
    }
}

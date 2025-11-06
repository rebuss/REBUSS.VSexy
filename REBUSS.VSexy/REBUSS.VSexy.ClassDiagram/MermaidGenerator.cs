using REBUSS.VSexy.Model;
using REBUSS.VSexy.Model.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace REBUSS.VSexy.ClassDiagram
{
    public class MermaidGenerator
    {
        private readonly ClassDiagramOptions _options;

        public MermaidGenerator() : this(new ClassDiagramOptions())
        {
        }

        public MermaidGenerator(ClassDiagramOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public string Generate(List<RTypeInfo> types)
        {
            if (types == null || types.Count == 0)
            {
                return "classDiagram\n    note \"No types found\"";
            }

            var sb = new StringBuilder();
            sb.AppendLine("classDiagram");

            var filteredTypes = FilterTypes(types);

            foreach (var type in filteredTypes)
            {
                GenerateTypeDefinition(sb, type);
            }

            GenerateRelationships(sb, filteredTypes);

            return sb.ToString();
        }

        private List<RTypeInfo> FilterTypes(List<RTypeInfo> types)
        {
            var filtered = types.AsEnumerable();

            if (!_options.IncludePrivateMembers)
            {
                // Remove types that are not public
            }

            if (_options.IncludedNamespaces.Any())
            {
                filtered = filtered.Where(t => 
                    _options.IncludedNamespaces.Any(ns => 
                        t.Namespace?.StartsWith(ns, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (_options.ExcludedNamespaces.Any())
            {
                filtered = filtered.Where(t => 
                    !_options.ExcludedNamespaces.Any(ns => 
                        t.Namespace?.StartsWith(ns, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return filtered.ToList();
        }

        private void GenerateTypeDefinition(StringBuilder sb, RTypeInfo type)
        {
            sb.AppendLine($"    class {type.Name} {{");

            string stereotype = GetStereotype(type);
            if (!string.IsNullOrEmpty(stereotype))
            {
                sb.AppendLine($"        {stereotype}");
            }

            if (type.Kind == RTypeKind.Enum)
            {
                GenerateEnumMembers(sb, type);
            }
            else
            {
                if (_options.IncludeFields)
                {
                    GenerateFields(sb, type);
                }

                if (_options.IncludeProperties)
                {
                    GenerateProperties(sb, type);
                }

                if (_options.IncludeMethods)
                {
                    GenerateMethods(sb, type);
                }
            }

            sb.AppendLine("    }");
        }

        private string GetStereotype(RTypeInfo type)
        {
            switch(type.Kind)
            {
                case RTypeKind.Interface: return "<<interface>>";
                case RTypeKind.Enum: return "<<enumeration>>";
                case RTypeKind.Record: return "<<record>>";
                case RTypeKind.Struct: return "<<struct>>";
                case RTypeKind.Class:
                    if (type.IsAbstract) return "<<abstract>>";
                    if (type.IsStatic) return "<<static>>";
                    return null;
                default: return null;
            };
        }

        private void GenerateEnumMembers(StringBuilder sb, RTypeInfo type)
        {
            foreach (var member in type.EnumMembers)
            {
                sb.AppendLine($"        {member}");
            }
        }

        private void GenerateFields(StringBuilder sb, RTypeInfo type)
        {
            var fields = type.Fields;

            if (!_options.IncludePrivateMembers)
            {
                fields = fields.Where(f => f.Accessibility == AccessibilityLevel.Public).ToList();
            }

            foreach (var field in fields)
            {
                string modifier = GetAccessModifier(field.Accessibility);
                string staticModifier = field.IsStatic ? "static " : "";
                string readonlyModifier = field.IsReadOnly ? "readonly " : "";
                string constModifier = field.IsConst ? "const " : "";
                
                sb.AppendLine($"        {modifier}{staticModifier}{constModifier}{readonlyModifier}{field.Type} {field.Name}");
            }
        }

        private void GenerateProperties(StringBuilder sb, RTypeInfo type)
        {
            var properties = type.Properties;

            if (!_options.IncludePrivateMembers)
            {
                properties = properties.Where(p => p.Accessibility == AccessibilityLevel.Public).ToList();
            }

            foreach (var property in properties)
            {
                string modifier = GetAccessModifier(property.Accessibility);
                string staticModifier = property.IsStatic ? "static " : "";
                string virtualModifier = property.IsVirtual ? "virtual " : "";
                string overrideModifier = property.IsOverride ? "override " : "";
                string abstractModifier = property.IsAbstract ? "abstract " : "";
                
                string accessors = "";
                if (_options.ShowPropertyAccessors)
                {
                    if (property.HasGetter && property.HasSetter)
                        accessors = " { get; set; }";
                    else if (property.HasGetter)
                        accessors = " { get; }";
                    else if (property.HasSetter)
                        accessors = " { set; }";
                }

                sb.AppendLine($"        {modifier}{staticModifier}{abstractModifier}{virtualModifier}{overrideModifier}{property.Type} {property.Name}{accessors}");
            }
        }

        private void GenerateMethods(StringBuilder sb, RTypeInfo type)
        {
            var methods = type.Methods;

            if (!_options.IncludePrivateMembers)
            {
                methods = methods.Where(m => m.Accessibility == AccessibilityLevel.Public).ToList();
            }

            foreach (var method in methods)
            {
                string modifier = GetAccessModifier(method.Accessibility);
                string staticModifier = method.IsStatic ? "static " : "";
                string virtualModifier = method.IsVirtual ? "virtual " : "";
                string overrideModifier = method.IsOverride ? "override " : "";
                string abstractModifier = method.IsAbstract ? "abstract " : "";
                string asyncModifier = method.IsAsync ? "async " : "";

                string parameters = _options.ShowMethodParameters
                    ? string.Join(", ", method.Parameters.Select(FormatParameter))
                    : method.Parameters.Any() ? "..." : "";

                sb.AppendLine($"        {modifier}{staticModifier}{abstractModifier}{asyncModifier}{virtualModifier}{overrideModifier}{method.Name}({parameters}) {method.ReturnType}");
            }
        }

        private string FormatParameter(ParameterInfo param)
        {
            string result = $"{param.Type} {param.Name}";
            
            if (param.HasDefaultValue && !string.IsNullOrEmpty(param.DefaultValue))
            {
                result += $" = {param.DefaultValue}";
            }

            return result;
        }

        private void GenerateRelationships(StringBuilder sb, List<RTypeInfo> types)
        {
            var typeDict = new Dictionary<string, RTypeInfo>();
            
            foreach (var type in types)
            {
                var key = string.IsNullOrEmpty(type.FullName) ? type.Name : type.FullName;
                if (!typeDict.ContainsKey(key))
                {
                    typeDict.Add(key, type);
                }
            }

            foreach (var type in types)
            {
                var typeKey = string.IsNullOrEmpty(type.FullName) ? type.Name : type.FullName;
                
                if (!string.IsNullOrEmpty(type.BaseTypeFullName) && typeDict.ContainsKey(type.BaseTypeFullName))
                {
                    var baseType = typeDict[type.BaseTypeFullName];
                    if (!string.Equals(baseType.FullName, type.FullName, StringComparison.Ordinal))
                    {
                        sb.AppendLine($"    {baseType.Name} <|-- {type.Name}");
                    }
                }
                else if (!string.IsNullOrEmpty(type.BaseType) && typeDict.ContainsKey(type.BaseType))
                {
                    var baseType = typeDict[type.BaseType];
                    if (!string.Equals(baseType.Name, type.Name, StringComparison.Ordinal))
                    {
                        sb.AppendLine($"    {baseType.Name} <|-- {type.Name}");
                    }
                }

                foreach (var iface in type.Interfaces)
                {
                    var ifaceKey = string.IsNullOrEmpty(iface.FullName) ? iface.Name : iface.FullName;
                    if (typeDict.ContainsKey(ifaceKey))
                    {
                        var ifaceType = typeDict[ifaceKey];
                        if (!string.Equals(ifaceType.FullName, type.FullName, StringComparison.Ordinal))
                        {
                            sb.AppendLine($"    {ifaceType.Name} <|.. {type.Name}");
                        }
                    }
                }

                if (_options.ShowCompositionRelationships)
                {
                    GenerateCompositionRelationships(sb, type, typeDict);
                }
            }
        }

        private void GenerateCompositionRelationships(StringBuilder sb, RTypeInfo type, Dictionary<string, RTypeInfo> typeDict)
        {
            var relatedTypes = new HashSet<string>();

            foreach (var field in type.Fields)
            {
                var fieldKey = string.IsNullOrEmpty(field.TypeFullName) ? field.Type : field.TypeFullName;
                if (typeDict.ContainsKey(fieldKey))
                {
                    var fieldType = typeDict[fieldKey];
                    if (!string.Equals(fieldType.FullName, type.FullName, StringComparison.Ordinal))
                    {
                        relatedTypes.Add(fieldType.Name);
                    }
                }
            }

            foreach (var property in type.Properties)
            {
                var propertyKey = string.IsNullOrEmpty(property.TypeFullName) ? property.Type : property.TypeFullName;
                if (typeDict.ContainsKey(propertyKey))
                {
                    var propertyType = typeDict[propertyKey];
                    if (!string.Equals(propertyType.FullName, type.FullName, StringComparison.Ordinal))
                    {
                        relatedTypes.Add(propertyType.Name);
                    }
                }
            }

            foreach (var relatedTypeName in relatedTypes)
            {
                if (!string.Equals(relatedTypeName, type.Name, StringComparison.Ordinal))
                {
                    sb.AppendLine($"    {type.Name} --> {relatedTypeName}");
                }
            }
        }
        
        private string GetAccessModifier(AccessibilityLevel accessibility)
        {
            switch(accessibility)
            {
                case AccessibilityLevel.Public: return "+";
                case AccessibilityLevel.Private: return "-";
                case AccessibilityLevel.Protected: return "#";
                case AccessibilityLevel.Internal: return "~";
                case AccessibilityLevel.ProtectedInternal: return "#~";
                case AccessibilityLevel.PrivateProtected: return "-#";
                default: return string.Empty;
            };
        }
    }
}

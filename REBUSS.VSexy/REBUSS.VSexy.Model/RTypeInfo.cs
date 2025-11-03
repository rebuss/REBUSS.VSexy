using REBUSS.VSexy.Model.Enums;
using System.Collections.Generic;

namespace REBUSS.VSexy.Model
{
    public class RTypeInfo
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Namespace { get; set; }
        public RTypeKind Kind { get; set; }

        public bool IsAbstract { get; set; }
        public bool IsSealed { get; set; }
        public bool IsStatic { get; set; }

        public string BaseType { get; set; }
        public string BaseTypeFullName { get; set; }

        public List<InterfaceReference> Interfaces { get; set; } = new List<InterfaceReference>();
        public List<PropertyMemberInfo> Properties { get; set; } = new List<PropertyMemberInfo>();
        public List<MethodMemberInfo> Methods { get; set; } = new List<MethodMemberInfo>();
        public List<FieldMemberInfo> Fields { get; set; } = new List<FieldMemberInfo>();
        public List<string> EnumMembers { get; set; } = new List<string>();
        public bool IsClass => Kind == RTypeKind.Class;
    }
}

using REBUSS.VSexy.Model.Enums;
using System.Collections.Generic;

namespace REBUSS.VSexy.Model
{
    public class MethodMemberInfo
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public string ReturnTypeFullName { get; set; }
        public AccessibilityLevel Accessibility { get; set; }

        public bool IsStatic { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsOverride { get; set; }
        public bool IsAbstract { get; set; }
        public bool IsAsync { get; set; }

        public List<ParameterInfo> Parameters { get; set; } = new List<ParameterInfo>();
    }
}

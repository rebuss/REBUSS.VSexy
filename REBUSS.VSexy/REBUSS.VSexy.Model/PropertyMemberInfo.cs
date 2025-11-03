using REBUSS.VSexy.Model.Enums;

namespace REBUSS.VSexy.Model
{
    public class PropertyMemberInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string TypeFullName { get; set; }
        public AccessibilityLevel Accessibility { get; set; }

        public bool IsStatic { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsOverride { get; set; }
        public bool IsAbstract { get; set; }

        public bool HasGetter { get; set; }
        public bool HasSetter { get; set; }
    }
}

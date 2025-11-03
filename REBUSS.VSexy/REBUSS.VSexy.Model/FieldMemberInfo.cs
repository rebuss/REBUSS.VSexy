using REBUSS.VSexy.Model.Enums;

namespace REBUSS.VSexy.Model
{
    public class FieldMemberInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string TypeFullName { get; set; }
        public AccessibilityLevel Accessibility { get; set; }

        public bool IsStatic { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsConst { get; set; }
        public string ConstantValue { get; set; }
    }
}

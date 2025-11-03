namespace REBUSS.VSexy.Model
{
    public class ParameterInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string TypeFullName { get; set; }
        public bool IsOptional { get; set; }
        public bool HasDefaultValue { get; set; }
        public string DefaultValue { get; set; }
    }
}

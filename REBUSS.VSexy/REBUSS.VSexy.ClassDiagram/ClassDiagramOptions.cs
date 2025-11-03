using System.Collections.Generic;

namespace REBUSS.VSexy.ClassDiagram
{
    public class ClassDiagramOptions
    {
        public bool IncludeFields { get; set; } = true;
        public bool IncludeProperties { get; set; } = true;
        public bool IncludeMethods { get; set; } = true;
        public bool IncludePrivateMembers { get; set; } = false;
        public bool ShowMethodParameters { get; set; } = true;
        public bool ShowPropertyAccessors { get; set; } = false;
        public bool ShowCompositionRelationships { get; set; } = true;
        public List<string> IncludedNamespaces { get; set; } = new List<string>();
        public List<string> ExcludedNamespaces { get; set; } = new List<string>();
    }
}

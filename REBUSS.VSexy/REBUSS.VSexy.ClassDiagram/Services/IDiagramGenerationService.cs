using EnvDTE;
using System.Threading.Tasks;

namespace REBUSS.VSexy.ClassDiagram
{
    /// <summary>
    /// Service for generating Mermaid diagrams.
    /// </summary>
    internal interface IDiagramGenerationService
    {
        /// <summary>
        /// Generates a diagram for the selected item.
        /// </summary>
        Task<DiagramGenerationResult> GenerateAsync(SelectedItem selectedItem);
    }
}
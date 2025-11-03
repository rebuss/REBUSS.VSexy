using System.Threading.Tasks;

namespace REBUSS.VSexy.ClassDiagram
{
    /// <summary>
    /// Service for file operations.
    /// </summary>
    internal interface IFileService
    {
        /// <summary>
        /// Saves the diagram to a file.
        /// </summary>
        Task<string> SaveDiagramAsync(DiagramGenerationResult result);

        /// <summary>
        /// Opens a file in Visual Studio.
        /// </summary>
        Task OpenFileAsync(string filePath);
    }
}
using System.Threading.Tasks;

namespace REBUSS.VSexy.ClassDiagram
{
    /// <summary>
    /// Service for displaying dialogs to the user.
    /// </summary>
    internal interface IDialogService
    {
        /// <summary>
        /// Shows an error message.
        /// </summary>
        Task ShowErrorAsync(string message);

        /// <summary>
        /// Shows an informational message.
        /// </summary>
        Task ShowInfoAsync(string message);

        /// <summary>
        /// Shows diagram generation results.
        /// </summary>
        Task ShowDiagramInfoAsync(DiagramGenerationResult result, string filePath);
    }
}
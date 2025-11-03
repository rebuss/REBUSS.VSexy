using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using System.Threading.Tasks;

namespace REBUSS.VSexy.ClassDiagram
{
    /// <summary>
    /// Implementation of dialog service using Visual Studio shell utilities.
    /// </summary>
    internal class DialogService : IDialogService
    {
        private readonly AsyncPackage _package;

        public DialogService(AsyncPackage package)
        {
            _package = package;
        }

        /// <summary>
        /// Shows an error message.
        /// </summary>
        public async Task ShowErrorAsync(string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            VsShellUtilities.ShowMessageBox(
                _package,
                message,
                "Error",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        /// <summary>
        /// Shows an informational message.
        /// </summary>
        public async Task ShowInfoAsync(string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            VsShellUtilities.ShowMessageBox(
                _package,
                message,
                "Information",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        /// <summary>
        /// Shows diagram generation results.
        /// </summary>
        public async Task ShowDiagramInfoAsync(DiagramGenerationResult result, string filePath)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var message = BuildDiagramInfoMessage(result.TypeCount, filePath);

            VsShellUtilities.ShowMessageBox(
                _package,
                result.MermaidDiagram,
                "Mermaid Class Diagram",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private string BuildDiagramInfoMessage(int typeCount, string filePath)
        {
            var message = $"Total types found: {typeCount}\n\n";
            
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                message += $"Mermaid diagram saved to:\n{filePath}\n\n";
                message += "Click 'Open Diagram' to view in Visual Studio.";
            }

            return message;
        }
    }
}
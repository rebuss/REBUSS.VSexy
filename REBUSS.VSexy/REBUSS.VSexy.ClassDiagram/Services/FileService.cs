using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace REBUSS.VSexy.ClassDiagram
{
    /// <summary>
    /// Service for file operations in Visual Studio.
    /// </summary>
    internal class FileService : IFileService
    {
        private readonly AsyncPackage _package;

        public FileService(AsyncPackage package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
        }

        /// <summary>
        /// Saves the diagram to a .mermaid file.
        /// </summary>
        public async Task<string> SaveDiagramAsync(DiagramGenerationResult result)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var outputPath = DetermineOutputPath(result);
                if (string.IsNullOrEmpty(outputPath))
                {
                    System.Diagnostics.Debug.WriteLine("Could not determine output path for mermaid file.");
                    return null;
                }

                File.WriteAllText(outputPath, result.MermaidDiagram, Encoding.UTF8);
                System.Diagnostics.Debug.WriteLine($"Saved mermaid diagram to {outputPath}");
                
                return outputPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving mermaid file: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Opens a file in Visual Studio editor.
        /// </summary>
        public async Task OpenFileAsync(string filePath)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"File does not exist: {filePath}");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Opening file: {filePath}");

                if (await TryOpenWithVsShellUtilitiesAsync(filePath))
                    return;

                await OpenWithDteAsync(filePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening file: {ex}");
            }
        }

        private string DetermineOutputPath(DiagramGenerationResult result)
        {
            string outputDir = null;
            string baseName = null;

            if (!string.IsNullOrEmpty(result.ProjectPath) && File.Exists(result.ProjectPath))
            {
                outputDir = Path.GetDirectoryName(result.ProjectPath);
                baseName = Path.GetFileNameWithoutExtension(result.ProjectPath);
            }
            else if (!string.IsNullOrEmpty(result.SelectedFilePath) && File.Exists(result.SelectedFilePath))
            {
                outputDir = Path.GetDirectoryName(result.SelectedFilePath);
                baseName = Path.GetFileNameWithoutExtension(result.SelectedFilePath);
            }

            if (string.IsNullOrEmpty(outputDir) || string.IsNullOrEmpty(baseName))
                return null;

            return Path.Combine(outputDir, baseName + ".mermaid");
        }

        private async Task<bool> TryOpenWithVsShellUtilitiesAsync(string filePath)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                Guid viewGuid = VSConstants.LOGVIEWID_TextView;
                IVsUIHierarchy hierarchy;
                uint itemid;
                IVsWindowFrame windowFrame;

                VsShellUtilities.OpenDocument(_package, filePath, viewGuid, out hierarchy, out itemid, out windowFrame);

                if (windowFrame != null)
                {
                    windowFrame.Show();
                    System.Diagnostics.Debug.WriteLine("Opened file via VsShellUtilities.OpenDocument.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VsShellUtilities.OpenDocument failed: {ex}");
            }

            return false;
        }

        private async Task OpenWithDteAsync(string filePath)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await _package.GetServiceAsync(typeof(DTE)) as DTE2;
            if (dte != null)
            {
                dte.ItemOperations.OpenFile(filePath);
                System.Diagnostics.Debug.WriteLine("Opened file via DTE.ItemOperations.OpenFile.");
            }
        }
    }
}
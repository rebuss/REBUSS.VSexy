using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using REBUSS.VSexy.Model;
using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;

namespace REBUSS.VSexy.ClassDiagram
{
    /// <summary>
    /// Command handler for opening 3D diagram window.
    /// </summary>
    internal sealed class Generate3DDiagramCommand
    {
        private const int CommandId = 0x0101;
        private static readonly Guid CommandSet = new Guid("8962c9e8-bda2-49e2-9591-4bf8c0b4c23f");

        private readonly AsyncPackage _package;
        private readonly WorkspaceService _workspaceService;
        private readonly ProjectAnalyzer _projectAnalyzer;

        private Generate3DDiagramCommand(
            AsyncPackage package, 
            OleMenuCommandService commandService,
            WorkspaceService workspaceService,
            ProjectAnalyzer projectAnalyzer)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _workspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService));
            _projectAnalyzer = projectAnalyzer ?? throw new ArgumentNullException(nameof(projectAnalyzer));

            if (commandService == null)
                throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static Generate3DDiagramCommand Instance { get; private set; }

        /// <summary>
        /// Initializes the command instance.
        /// </summary>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            var workspaceService = new WorkspaceService(package);
            var projectAnalyzer = new ProjectAnalyzer();

            Instance = new Generate3DDiagramCommand(package, commandService, workspaceService, projectAnalyzer);
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await ExecuteAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in Generate3DDiagramCommand: {ex}");
                    VsShellUtilities.ShowMessageBox(
                        _package,
                        $"Error: {ex.Message}",
                        "3D Diagram Error",
                        OLEMSGICON.OLEMSGICON_CRITICAL,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                }
            }).FileAndForget("REBUSS.VSexy.ClassDiagram/Generate3DDiagramCommand");
        }

        /// <summary>
        /// Executes the command asynchronously.
        /// </summary>
        private async Task ExecuteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await _package.GetServiceAsync(typeof(DTE)) as DTE2;
            if (dte == null)
            {
                VsShellUtilities.ShowMessageBox(
                    _package,
                    "Could not get DTE service",
                    "3D Diagram Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            var selectedItem = GetSelectedItem(dte);
            if (selectedItem == null || string.IsNullOrEmpty(selectedItem.Name))
            {
                VsShellUtilities.ShowMessageBox(
                    _package,
                    "Please select a C# file in Solution Explorer",
                    "3D Diagram",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            var filePath = GetSelectedFilePath(selectedItem);
            if (string.IsNullOrEmpty(filePath) || !filePath.EndsWith(".cs"))
            {
                VsShellUtilities.ShowMessageBox(
                    _package,
                    "Please select a C# file (.cs)",
                    "3D Diagram",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            var typeInfo = await AnalyzeSelectedFileAsync(filePath);
            if (typeInfo == null)
            {
                VsShellUtilities.ShowMessageBox(
                    _package,
                    "Could not analyze the selected file. Make sure it contains a class, interface, struct, enum or record.",
                    "3D Diagram",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            await ShowDiagramWindowAsync(typeInfo);
        }

        /// <summary>
        /// Gets the selected item from Solution Explorer.
        /// </summary>
        private SelectedItem GetSelectedItem(DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (dte.SelectedItems.Count == 0)
                return null;

            return dte.SelectedItems.Item(1);
        }

        /// <summary>
        /// Gets the file path of the selected item.
        /// </summary>
        private string GetSelectedFilePath(SelectedItem selectedItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (selectedItem.ProjectItem != null)
                {
                    return selectedItem.ProjectItem.FileNames[1];
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting file path: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Analyzes the selected file and returns the first type found.
        /// </summary>
        private async Task<RTypeInfo> AnalyzeSelectedFileAsync(string filePath)
        {
            try
            {
                var document = await _workspaceService.GetDocumentByPathAsync(filePath);
                if (document == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Document not found: {filePath}");
                    return null;
                }

                var types = await _projectAnalyzer.AnalyzeDocumentAsync(document);
                return types.FirstOrDefault();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error analyzing file: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Shows the 3D diagram window with the analyzed type.
        /// </summary>
        private async Task ShowDiagramWindowAsync(RTypeInfo typeInfo)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var window = await _package.ShowToolWindowAsync(
                typeof(Diagram3DToolWindow),
                0,
                create: true,
                _package.DisposalToken);

            if (window?.Frame == null)
            {
                throw new NotSupportedException("Cannot create 3D Diagram window");
            }

            if (window.Content is Diagram3DToolWindowControl control)
            {
                control.SetTypeInfo(typeInfo);
            }

            var windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}
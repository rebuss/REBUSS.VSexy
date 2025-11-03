using EnvDTE;
using EnvDTE80;
using Microsoft.CodeAnalysis.Host;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace REBUSS.VSexy.ClassDiagram
{
    internal sealed class GenerateMermaidCommand
    {
        private const int CommandId = 0x0100;
        private static readonly Guid CommandSet = new Guid("8962c9e8-bda2-49e2-9591-4bf8c0b4c23f");
        private readonly AsyncPackage package;

        private WorkspaceService _workspaceService;
        private ProjectAnalyzer _projectAnalyzer;
        private MermaidGenerator _mermaidGenerator;

        private GenerateMermaidCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            _workspaceService = new WorkspaceService(package);
            _projectAnalyzer = new ProjectAnalyzer();

            var options = new ClassDiagramOptions
            {
                IncludeFields = true,
                IncludeProperties = true,
                IncludeMethods = true,
                IncludePrivateMembers = false,
                ShowMethodParameters = true,
                ShowPropertyAccessors = false,
                ShowCompositionRelationships = true
            };
            _mermaidGenerator = new MermaidGenerator(options);

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static GenerateMermaidCommand Instance { get; private set; }

        private IAsyncServiceProvider ServiceProvider => this.package;

        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(System.Threading.CancellationToken.None);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new GenerateMermaidCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                System.Diagnostics.Debug.WriteLine("GenerateMermaidCommand.Execute called!");

                // Pobierz DTE
                var dte = ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    return await ServiceProvider.GetServiceAsync(typeof(DTE)) as DTE2;
                });

                if (dte == null)
                {
                    ShowError("Could not get DTE service");
                    return;
                }

                if (dte.SelectedItems.Count == 0)
                {
                    ShowInfo("Please select a project or file in Solution Explorer");
                    return;
                }

                var selectedItem = dte.SelectedItems.Item(1);

                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await GenerateDiagramAsync(selectedItem);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Execute: {ex}");
                ShowError($"Error: {ex.Message}\n\nStack trace:\n{ex.StackTrace}");
            }
        }

        private async Task GenerateDiagramAsync(SelectedItem selectedItem)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                string mermaidDiagram = null;
                int totalClassCount = 0;
                string projectPath = null;
                string mermaidFilePath = null;
                string selectedFilePath = null;

                if (selectedItem.Project != null)
                {
                    var projectName = selectedItem.Project.Name;
                    System.Diagnostics.Debug.WriteLine($"Generating diagram for project: {projectName}");
                    projectPath = selectedItem.Project.FileName;

                    var project = await _workspaceService.GetProjectByNameAsync(projectName);
                    if (project == null)
                    {
                        ShowError($"Could not find Roslyn project: {projectName}");
                        return;
                    }

                    var types = await _projectAnalyzer.AnalyzeProjectAsync(project);
                    totalClassCount = types.Count(t => t.IsClass);
                    System.Diagnostics.Debug.WriteLine($"Found {types.Count} types");

                    mermaidDiagram = _mermaidGenerator.Generate(types);
                }
                else if (selectedItem.ProjectItem != null)
                {
                    var fileName = selectedItem.ProjectItem.Name;
                    System.Diagnostics.Debug.WriteLine($"Generating diagram for file: {fileName}");

                    selectedFilePath = selectedItem.ProjectItem.FileNames[1];
                    if (string.IsNullOrEmpty(selectedFilePath) || !selectedFilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    {
                        ShowInfo("Please select a C# file (.cs)");
                        return;
                    }

                    projectPath = selectedItem.ProjectItem.ContainingProject?.FileName;

                    var document = await _workspaceService.GetDocumentByPathAsync(selectedFilePath);
                    if (document == null)
                    {
                        ShowError($"Could not find Roslyn document: {selectedFilePath}");
                        return;
                    }

                    var types = await _projectAnalyzer.AnalyzeDocumentAsync(document);
                    totalClassCount = types.Count();
                    System.Diagnostics.Debug.WriteLine($"Found {types.Count} types");

                    mermaidDiagram = _mermaidGenerator.Generate(types);
                }

                if (string.IsNullOrEmpty(mermaidDiagram))
                {
                    ShowInfo("Could not generate diagram. Please select a project or C# file.");
                    return;
                }

                // Save the mermaid diagram to a .mermaid file next to the project or source file
                try
                {
                    string outputDir = null;
                    string baseName = null;

                    if (!string.IsNullOrEmpty(projectPath) && File.Exists(projectPath))
                    {
                        outputDir = Path.GetDirectoryName(projectPath);
                        baseName = Path.GetFileNameWithoutExtension(projectPath);
                    }
                    else if (!string.IsNullOrEmpty(selectedFilePath) && File.Exists(selectedFilePath))
                    {
                        outputDir = Path.GetDirectoryName(selectedFilePath);
                        baseName = Path.GetFileNameWithoutExtension(selectedFilePath);
                    }

                    if (!string.IsNullOrEmpty(outputDir) && !string.IsNullOrEmpty(baseName))
                    {
                        mermaidFilePath = Path.Combine(outputDir, baseName + ".mermaid");
                        File.WriteAllText(mermaidFilePath, mermaidDiagram, Encoding.UTF8);
                        System.Diagnostics.Debug.WriteLine($"Saved mermaid diagram to {mermaidFilePath}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Could not determine output path for mermaid file.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving mermaid file: {ex}");
                    ShowError($"Could not save mermaid diagram: {ex.Message}");
                }

                ShowDiagramInfo(totalClassCount, mermaidFilePath, mermaidDiagram);
                System.Diagnostics.Debug.WriteLine("Diagram generated successfully!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating diagram: {ex}");
                ShowError($"Error generating diagram: {ex.Message}");
            }
        }

        private void ShowDiagramInfo(int typeCount, string mermaidFilePath, string mermaidDiagram)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var message = $"Total types found: {typeCount}\n\n";
            if (!string.IsNullOrEmpty(mermaidFilePath) && File.Exists(mermaidFilePath))
            {
                message += $"Mermaid diagram saved to:\n{mermaidFilePath}\n\n";
                message += "Click 'Open Diagram' to view in Visual Studio.";

                var result = VsShellUtilities.ShowMessageBox(
                    this.package,
                    mermaidDiagram,
                    "Mermaid Class Diagram",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

                OpenFileInVisualStudio(mermaidFilePath);
            }
        }
        private void OpenFileInVisualStudio(string filePath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    ShowError($"File does not exist: {filePath}");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"OpenFileInVisualStudio: Opening {filePath}");

                // Safely get IVsUIShellOpenDocument on the UI thread (avoids blocking ContinueWith.Result)
                var openDocService = ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    return await ServiceProvider.GetServiceAsync(typeof(SVsUIShellOpenDocument)) as IVsUIShellOpenDocument;
                });

                if (openDocService != null)
                {
                    Guid viewGuid = VSConstants.LOGVIEWID_TextView;
                    IVsUIHierarchy hierarchy;
                    uint itemid;
                    IVsWindowFrame windowFrame;

                    // Use the VS helper which wraps interop differences and compiles reliably
                    try
                    {
                        VsShellUtilities.OpenDocument(this.package, filePath, viewGuid, out hierarchy, out itemid, out windowFrame);

                        if (windowFrame != null)
                        {
                            windowFrame.Show();
                            System.Diagnostics.Debug.WriteLine($"Opened file via VsShellUtilities.OpenDocument.");
                            return;
                        }

                        System.Diagnostics.Debug.WriteLine("VsShellUtilities.OpenDocument returned no window frame. Falling back to DTE.");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"VsShellUtilities.OpenDocument failed: {ex}");
                        System.Diagnostics.Debug.WriteLine("Falling back to DTE.");
                    }
                }

                // Fallback: use DTE to open the file
                var dte = ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    return await ServiceProvider.GetServiceAsync(typeof(DTE)) as DTE2;
                });

                if (dte != null)
                {
                    dte.ItemOperations.OpenFile(filePath);
                    System.Diagnostics.Debug.WriteLine("Opened file via DTE.ItemOperations.OpenFile.");
                    return;
                }

                ShowError("Could not obtain services to open the file in Visual Studio.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening file: {ex}");
                ShowError($"Could not open file: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                "Error",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private void ShowInfo(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                "Information",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}

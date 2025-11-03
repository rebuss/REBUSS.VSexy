using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Operations;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace REBUSS.VSexy.ClassDiagram
{
    /// <summary>
    /// Command handler for generating Mermaid class diagrams.
    /// </summary>
    internal sealed class GenerateMermaidCommand
    {
        private const int CommandId = 0x0100;
        private static readonly Guid CommandSet = new Guid("8962c9e8-bda2-49e2-9591-4bf8c0b4c23f");

        private readonly AsyncPackage _package;
        private readonly IDialogService _dialogService;
        private readonly IDiagramGenerationService _diagramGenerationService;
        private readonly IFileService _fileService;

        private GenerateMermaidCommand(
            AsyncPackage package,
            OleMenuCommandService commandService,
            IDialogService dialogService,
            IDiagramGenerationService diagramGenerationService,
            IFileService fileService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _diagramGenerationService = diagramGenerationService ?? throw new ArgumentNullException(nameof(diagramGenerationService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));

            if (commandService == null)
                throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static GenerateMermaidCommand Instance { get; private set; }

        /// <summary>
        /// Initializes the command instance.
        /// </summary>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            var dialogService = new DialogService(package);
            var workspaceService = new WorkspaceService(package);
            var diagramGenerationService = new DiagramGenerationService(workspaceService);
            var fileService = new FileService(package);

            Instance = new GenerateMermaidCommand(package, commandService, dialogService, diagramGenerationService, fileService);
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
                    System.Diagnostics.Debug.WriteLine($"Error in Execute: {ex}");
                    await _dialogService.ShowErrorAsync($"Error: {ex.Message}");
                }
            }).FileAndForget("REBUSS.VSexy.ClassDiagram/GenerateMermaidCommand");
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
                await _dialogService.ShowErrorAsync("Could not get DTE service");
                return;
            }

            var selectedItem = GetSelectedItem(dte);
            if (selectedItem == null)
            {
                await _dialogService.ShowInfoAsync("Please select a project or file in Solution Explorer");
                return;
            }

            await GenerateDiagramAsync(selectedItem);
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
        /// Generates the diagram for the selected item.
        /// </summary>
        private async Task GenerateDiagramAsync(SelectedItem selectedItem)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var result = await _diagramGenerationService.GenerateAsync(selectedItem);

            if (!result.Success)
            {
                await _dialogService.ShowErrorAsync(result.ErrorMessage);
                return;
            }

            if (string.IsNullOrEmpty(result.MermaidDiagram))
            {
                await _dialogService.ShowInfoAsync("Could not generate diagram. Please select a project or C# file.");
                return;
            }

            var filePath = await _fileService.SaveDiagramAsync(result);
            // await _dialogService.ShowDiagramInfoAsync(result, filePath);

            if (!string.IsNullOrEmpty(filePath))
            {
                await _fileService.OpenFileAsync(filePath);
            }
        }
    }
}
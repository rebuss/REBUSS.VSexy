using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
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

        private Generate3DDiagramCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));

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
            Instance = new Generate3DDiagramCommand(package, commandService);
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

            var window = await _package.ShowToolWindowAsync(
                typeof(Diagram3DToolWindow),
                0,
                create: true,
                _package.DisposalToken);

            if (window?.Frame == null)
            {
                throw new NotSupportedException("Cannot create 3D Diagram window");
            }

            var windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}
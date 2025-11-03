using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace REBUSS.VSexy.ClassDiagram
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(ClassDiagramPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class ClassDiagramPackage : AsyncPackage
    {
        public const string PackageGuidString = "b75ae447-3485-4437-a8d3-83c4daed539c";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            System.Diagnostics.Debug.WriteLine("ClassDiagramPackage InitializeAsync started!");

            await GenerateMermaidCommand.InitializeAsync(this);

            System.Diagnostics.Debug.WriteLine("ClassDiagramPackage InitializeAsync completed!");
        }
    }
}
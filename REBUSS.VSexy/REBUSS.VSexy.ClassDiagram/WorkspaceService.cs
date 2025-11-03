using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;

namespace REBUSS.VSexy.ClassDiagram
{
    public class WorkspaceService
    {
        private readonly IServiceProvider _serviceProvider;
        private VisualStudioWorkspace _workspace;

        public WorkspaceService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task<VisualStudioWorkspace> GetWorkspaceAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (_workspace == null)
            {
                var componentModel = (IComponentModel)_serviceProvider.GetService(typeof(SComponentModel));
                if (componentModel != null)
                {
                    _workspace = componentModel.GetService<VisualStudioWorkspace>();
                }
            }

            return _workspace;
        }

        public async Task<Project> GetProjectByPathAsync(string projectPath)
        {
            var workspace = await GetWorkspaceAsync();
            if (workspace == null)
                return null;

            return workspace.CurrentSolution.Projects
                .FirstOrDefault(p => p.FilePath?.Equals(projectPath, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        public async Task<Project> GetProjectByNameAsync(string projectName)
        {
            var workspace = await GetWorkspaceAsync();
            if (workspace == null)
                return null;

            return workspace.CurrentSolution.Projects
                .FirstOrDefault(p => p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<Document> GetDocumentByPathAsync(string documentPath)
        {
            var workspace = await GetWorkspaceAsync();
            if (workspace == null)
                return null;

            return workspace.CurrentSolution.Projects
                .SelectMany(p => p.Documents)
                .FirstOrDefault(d => d.FilePath?.Equals(documentPath, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        public async Task<Project[]> GetAllCSharpProjectsAsync()
        {
            var workspace = await GetWorkspaceAsync();
            if (workspace == null)
                return Array.Empty<Project>();

            return workspace.CurrentSolution.Projects
                .Where(p => p.Language == LanguageNames.CSharp)
                .ToArray();
        }
    }
}

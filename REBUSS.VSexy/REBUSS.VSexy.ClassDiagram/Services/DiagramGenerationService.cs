using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace REBUSS.VSexy.ClassDiagram
{
    /// <summary>
    /// Service for generating Mermaid class diagrams from code.
    /// </summary>
    internal class DiagramGenerationService : IDiagramGenerationService
    {
        private readonly WorkspaceService _workspaceService;
        private readonly ProjectAnalyzer _projectAnalyzer;
        private readonly MermaidGenerator _mermaidGenerator;

        public DiagramGenerationService(WorkspaceService workspaceService)
        {
            _workspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService));
            _projectAnalyzer = new ProjectAnalyzer();
            _mermaidGenerator = new MermaidGenerator(CreateDefaultOptions());
        }

        /// <summary>
        /// Generates a diagram for the selected item.
        /// </summary>
        public async Task<DiagramGenerationResult> GenerateAsync(SelectedItem selectedItem)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                if (selectedItem.Project != null)
                {
                    return await GenerateFromProjectAsync(selectedItem.Project);
                }

                if (selectedItem.ProjectItem != null)
                {
                    return await GenerateFromFileAsync(selectedItem.ProjectItem);
                }

                return DiagramGenerationResult.Failure("No project or file selected");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating diagram: {ex}");
                return DiagramGenerationResult.Failure($"Error generating diagram: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates diagram from a project.
        /// </summary>
        private async Task<DiagramGenerationResult> GenerateFromProjectAsync(EnvDTE.Project dteProject)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var projectName = dteProject.Name;
            var projectPath = dteProject.FileName;

            System.Diagnostics.Debug.WriteLine($"Generating diagram for project: {projectName}");

            var project = await _workspaceService.GetProjectByNameAsync(projectName);
            if (project == null)
            {
                return DiagramGenerationResult.Failure($"Could not find Roslyn project: {projectName}");
            }

            var types = await _projectAnalyzer.AnalyzeProjectAsync(project);
            var typeCount = types.Count(t => t.IsClass);
            var mermaidDiagram = _mermaidGenerator.Generate(types);

            System.Diagnostics.Debug.WriteLine($"Found {types.Count} types");

            return DiagramGenerationResult.SuccessResult(mermaidDiagram, typeCount, projectPath, null);
        }

        /// <summary>
        /// Generates diagram from a single file.
        /// </summary>
        private async Task<DiagramGenerationResult> GenerateFromFileAsync(ProjectItem projectItem)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var fileName = projectItem.Name;
            var filePath = projectItem.FileNames[1];

            System.Diagnostics.Debug.WriteLine($"Generating diagram for file: {fileName}");

            if (string.IsNullOrEmpty(filePath) || !filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                return DiagramGenerationResult.Failure("Please select a C# file (.cs)");
            }

            var projectPath = projectItem.ContainingProject?.FileName;
            var document = await _workspaceService.GetDocumentByPathAsync(filePath);

            if (document == null)
            {
                return DiagramGenerationResult.Failure($"Could not find Roslyn document: {filePath}");
            }

            var types = await _projectAnalyzer.AnalyzeDocumentAsync(document);
            var mermaidDiagram = _mermaidGenerator.Generate(types);

            System.Diagnostics.Debug.WriteLine($"Found {types.Count} types");

            return DiagramGenerationResult.SuccessResult(mermaidDiagram, types.Count, projectPath, filePath);
        }

        private ClassDiagramOptions CreateDefaultOptions()
        {
            return new ClassDiagramOptions
            {
                IncludeFields = true,
                IncludeProperties = true,
                IncludeMethods = true,
                IncludePrivateMembers = false,
                ShowMethodParameters = true,
                ShowPropertyAccessors = false,
                ShowCompositionRelationships = true
            };
        }
    }
}
using System;

namespace REBUSS.VSexy.ClassDiagram
{
    /// <summary>
    /// Result of diagram generation operation.
    /// </summary>
    internal class DiagramGenerationResult
    {
        public bool Success { get; set; }
        public string MermaidDiagram { get; set; }
        public int TypeCount { get; set; }
        public string ProjectPath { get; set; }
        public string SelectedFilePath { get; set; }
        public string ErrorMessage { get; set; }

        public static DiagramGenerationResult SuccessResult(
                                            string mermaidDiagram,
                                            int typeCount,
                                            string projectPath,
                                            string selectedFilePath)
        {
            return new DiagramGenerationResult
            {
                Success = true,
                MermaidDiagram = mermaidDiagram,
                TypeCount = typeCount,
                ProjectPath = projectPath,
                SelectedFilePath = selectedFilePath
            };
        }

        public static DiagramGenerationResult Failure(string errorMessage)
        {
            return new DiagramGenerationResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
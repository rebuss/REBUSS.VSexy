using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace REBUSS.VSexy.ClassDiagram
{
    /// <summary>
    /// Tool window for displaying 3D diagrams using Babylon.js.
    /// </summary>
    [Guid("a3d5e8f1-4b2c-4d5e-9f1a-2b3c4d5e6f7a")]
    public class Diagram3DToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the Diagram3DToolWindow class.
        /// </summary>
        public Diagram3DToolWindow() : base(null)
        {
            Caption = "3D Diagram";
            Content = new Diagram3DToolWindowControl();
        }
    }
}
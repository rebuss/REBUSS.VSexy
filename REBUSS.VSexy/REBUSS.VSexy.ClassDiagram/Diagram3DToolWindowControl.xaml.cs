using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace REBUSS.VSexy.ClassDiagram
{
    /// <summary>
    /// Interaction logic for Diagram3DToolWindowControl.xaml
    /// </summary>
    public partial class Diagram3DToolWindowControl : Grid
    {
        /// <summary>
        /// Initializes a new instance of the Diagram3DToolWindowControl class.
        /// </summary>
        public Diagram3DToolWindowControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            var userDataFolder = Path.Combine(
                Path.GetTempPath(), 
                "REBUSS.VSexy.WebView2");
            
            Directory.CreateDirectory(userDataFolder);
            var environment = await CoreWebView2Environment.CreateAsync(
                userDataFolder: userDataFolder);
            
            await Web.EnsureCoreWebView2Async(environment);
            Web.CoreWebView2.Settings.AreDevToolsEnabled = true;

            var (rootPath, url) = ResolveWebRoot();
            Web.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "app.local", rootPath, CoreWebView2HostResourceAccessKind.Allow);

            Web.Source = new System.Uri(url); // https://app.local/index.html
        }

        private (string rootPath, string url) ResolveWebRoot()
        {
            // DEV: ładuj z /dist (po `npm run build` albo `npm run dev` + vite preview)
            var devRoot = Path.GetFullPath(Path.Combine(
                GetSolutionRootGuess(), "REBUSS.VSexy.ClassDiagram.WebClient", "dist"));
            if (Directory.Exists(devRoot))
                return (devRoot, "https://app.local/index.html");

            // PROD: ładuj z folderu /web spakowanego w VSIX
            var assemblyDir = Path.GetDirectoryName(typeof(Diagram3DToolWindowControl).Assembly.Location);
            var prodRoot = Path.Combine(assemblyDir, "web");
            return (prodRoot, "https://app.local/index.html");
        }

        private string GetSolutionRootGuess()
        {
            var dir = Directory.GetCurrentDirectory();
            while (dir != null && !File.Exists(Path.Combine(dir, "REBUSS.VSexy.sln")))
                dir = Directory.GetParent(dir)?.FullName;
            return dir ?? Directory.GetCurrentDirectory();
        }
    }
}
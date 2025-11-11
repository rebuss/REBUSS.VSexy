using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using REBUSS.VSexy.Model;
using System;
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
        private RTypeInfo _typeInfo;
        private bool _isWebViewInitialized;
        private bool _isWebViewInitializing;

        /// <summary>
        /// Initializes a new instance of the Diagram3DToolWindowControl class.
        /// </summary>
        public Diagram3DToolWindowControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        /// <summary>
        /// Sets the type information to be displayed in the 3D diagram.
        /// </summary>
        public void SetTypeInfo(RTypeInfo typeInfo)
        {
            _typeInfo = typeInfo;

            if (_isWebViewInitialized && Web?.CoreWebView2 != null)
            {
                SendTypeInfoToWeb();
            }
        }

        /// <summary>
        /// Handles the Loaded event and initializes WebView2.
        /// </summary>
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Prevent re-initialization
            if (_isWebViewInitializing || Web.CoreWebView2 != null)
                return;

            _isWebViewInitializing = true;
            
            try
            {
                var userDataFolder = Path.Combine(
                    Path.GetTempPath(), 
                    "REBUSS.VSexy.WebView2");
                
                Directory.CreateDirectory(userDataFolder);
                var environment = await CoreWebView2Environment.CreateAsync(
                    userDataFolder: userDataFolder);
                
                await Web.EnsureCoreWebView2Async(environment);
                Web.CoreWebView2.Settings.AreDevToolsEnabled = true;
                Web.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                var (rootPath, url) = ResolveWebRoot();
                Web.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "app.local", rootPath, CoreWebView2HostResourceAccessKind.Allow);

                Web.CoreWebView2.NavigationCompleted += (s, args) =>
                {
                    _isWebViewInitialized = true;
                    if (_typeInfo != null)
                    {
                        SendTypeInfoToWeb();
                    }
                };

                Web.Source = new System.Uri(url);
            }
            finally
            {
                _isWebViewInitializing = false;
            }
        }

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            // Use WebMessageAsJson instead of TryGetWebMessageAsString() for JSON messages
            Console.WriteLine("Message received from web: " + e.WebMessageAsJson);
        }   

        /// <summary>
        /// Sends type information to the WebView2 as JSON.
        /// </summary>
        private void SendTypeInfoToWeb()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_typeInfo, Formatting.Indented);
                Web.CoreWebView2.PostWebMessageAsString(json);
                System.Diagnostics.Debug.WriteLine($"Sent type info: {_typeInfo.Name}");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending type info: {ex.Message}");
            }
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
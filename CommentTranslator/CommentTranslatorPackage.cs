using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using CommentTranslator.Client;
using CommentTranslator.Command;
using CommentTranslator.Presentation;
using CommentTranslator.Util;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace CommentTranslator
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0.2", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(CommentTranslatorPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(OptionPageGrid), "CommentTranslator64", "General", 0, 0, true)]
    //[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class CommentTranslatorPackage : AsyncPackage
    {
        /// <summary>
        /// CommentTranslatorPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "15fe96fc-8fd3-4e06-b322-deba11e09dfc";

        public static Settings Settings { get; set; } = new Settings();

        public static TranslateClient TranslateClient { get; set; } = new TranslateClient(Settings);

        //public DTE2 DTE { get; set; }
        //public Events Events { get; set; }
        //public DocumentEvents DocumentEvents { get; set; }
        //public WindowEvents WindowEvents { get; set; }


        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            // await GetTKKCommand.InitializeAsync(this);
            await ToggleAutoTranslateCommand.InitializeAsync(this);
            await TranslateCommand.InitializeAsync(this);

            // 加载配置项
            Settings.ReloadSetting((OptionPageGrid)GetDialogPage(typeof(OptionPageGrid)));

            // 创建连接返回翻译内容
            TranslateClient = new TranslateClient(Settings);

            //DTE = (DTE2)GetService(typeof(DTE));
            //Events = DTE.Events;
            //DocumentEvents = Events.DocumentEvents;
            //WindowEvents = Events.WindowEvents;

            //DocumentEvents.DocumentOpened += DocumentEvents_DocumentOpened;
            //DocumentEvents.DocumentSaved += DocumentEvents_DocumentSaved;
            //WindowEvents.WindowActivated += WindowEvents_WindowActivated;
        }

        //private void WindowEvents_WindowActivated(Window GotFocus, Window LostFocus)
        //{
        //    //Debug.WriteLine("Focus: " + GotFocus.Caption);
        //}

        //private void DocumentEvents_DocumentSaved(Document Document)
        //{
        //    //Debug.WriteLine("Save: " + Document.Name);

        //}

        //private void DocumentEvents_DocumentOpened(Document Document)
        //{
        //    //Debug.WriteLine("Open: " + Document.Name);
        //}

        #endregion
    }
}

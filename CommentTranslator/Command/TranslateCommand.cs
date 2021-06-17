﻿using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommentTranslator.Ardonment;
using CommentTranslator.Util;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Task = System.Threading.Tasks.Task;

namespace CommentTranslator.Command
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class TranslateCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0101;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a957190e-e8f5-4158-94c9-06e36f63c123");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslateCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private TranslateCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static TranslateCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in TranslateCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new TranslateCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            //string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            //string title = "TranslateCommand";

            //// Show a message box to prove we were here
            //VsShellUtilities.ShowMessageBox(
            //    this.package,
            //    message,
            //    title,
            //    OLEMSGICON.OLEMSGICON_INFO,
            //    OLEMSGBUTTON.OLEMSGBUTTON_OK,
            //    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            var dte = ServiceProvider.GetServiceAsync(typeof(DTE)).Result as DTE2;

            if (dte.ActiveDocument != null)
            {
                var selection = (TextSelection)dte.ActiveDocument.Selection;

                //Select hold line if not select text
                if (string.IsNullOrEmpty(selection.Text))
                {
                    selection.SelectLine();
                }

                //Trim selected text
                var parser = CommentParserHelper.GetCommentParser(dte.ActiveDocument.Language);
                var text = selection.Text.Trim();

                if (parser != null)
                {
                    var regions = parser.GetCommentRegions(text, 0);
                    if (regions.Count() > 0 && regions.First().Start == 0 && regions.Last().End == text.Length)
                    {
                        text = string.Join(Environment.NewLine, regions.Select(r => parser.GetComment(text.Substring(r.Start, r.Length)).Content.Trim()));
                    }
                }

                //Check if selection text is still empty
                if (!string.IsNullOrEmpty(text))
                {
                    TranslatePopupConnector.Translate(GetWpfView(), text);
                }
            }
        }
        private IWpfTextView GetWpfView()
        {
            var textManager = (IVsTextManager)ServiceProvider.GetServiceAsync(typeof(SVsTextManager)).Result;
            var componentModel = (IComponentModel)this.ServiceProvider.GetServiceAsync(typeof(SComponentModel)).Result;
            var editor = componentModel.GetService<IVsEditorAdaptersFactoryService>();

            textManager.GetActiveView(1, null, out IVsTextView textViewCurrent);
            return editor.GetWpfTextView(textViewCurrent);
        }
    }
}

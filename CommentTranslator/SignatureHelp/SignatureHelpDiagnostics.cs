using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using IServiceProvider = System.IServiceProvider;

namespace CommentTranslator.SignatureHelp
{
    /// <summary>
    /// 签名帮助翻译临时诊断。
    /// 同时写入 Debug 输出与 VS 输出窗格「Comment Translator Signature Help」。
    /// </summary>
    internal static class SignatureHelpDiagnostics
    {
        internal const string PaneName = "Comment Translator Signature Help";

        private static readonly Guid s_paneGuid = new Guid("8f3c2a1e-6b5d-4e9f-a0c1-2d3e4f5a6b7c");

        private static readonly object s_lock = new object();
        private static IVsOutputWindowPane s_vsPane;
        private static bool s_paneInitialized;
        private static bool s_paneFailed;

        static SignatureHelpDiagnostics()
        {
            WriteDebug("SignatureHelpDiagnostics type loaded");
        }

        internal static void Initialize(IServiceProvider serviceProvider)
        {
            WriteDebug("SignatureHelpDiagnostics.Initialize called");
            if (serviceProvider == null)
            {
                WriteDebug("Initialize: serviceProvider is null");
                return;
            }

            lock (s_lock)
            {
                if (s_paneInitialized || s_paneFailed)
                {
                    return;
                }

                try
                {
                    var outputWindow = serviceProvider.GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
                    if (outputWindow == null)
                    {
                        WriteDebug("Initialize: SVsOutputWindow is null");
                        s_paneFailed = true;
                        return;
                    }

                    Guid paneGuid = s_paneGuid;
                    if (outputWindow.GetPane(ref paneGuid, out s_vsPane) != VSConstants.S_OK)
                    {
                        outputWindow.CreatePane(ref paneGuid, PaneName, 1, 1);
                        outputWindow.GetPane(ref paneGuid, out s_vsPane);
                    }

                    s_paneInitialized = true;
                    Log("=== Signature Help diagnostics initialized (IVsOutputWindow) ===");
                }
                catch (Exception ex)
                {
                    s_paneFailed = true;
                    WriteDebug("Initialize failed: " + ex);
                }
            }
        }

        internal static void Log(string message)
        {
            string line = "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + message;
            WriteDebug(line);

            try
            {
                lock (s_lock)
                {
                    if (s_vsPane != null)
                    {
                        s_vsPane.OutputString(line + Environment.NewLine);
                        s_vsPane.Activate();
                    }
                }
            }
            catch (Exception ex)
            {
                WriteDebug("IVs pane write failed: " + ex.Message);
            }
        }

        internal static void LogFormat(string format, params object[] args)
        {
            Log(string.Format(format, args));
        }

        internal static void LogEnabledState()
        {
            var settings = CommentTranslatorPackage.Settings;
            if (settings == null)
            {
                Log("IsEnabled=false: Settings is null");
                return;
            }

            if (!settings.AutoTranslateSignatureHelp)
            {
                Log("IsEnabled=false: AutoTranslateSignatureHelp is off");
                return;
            }

            if (CommentTranslatorPackage.TranslateClient == null)
            {
                Log("IsEnabled=false: TranslateClient is null");
                return;
            }

            Log("IsEnabled=true");
        }

        internal static void LogTextView(ITextView textView, string tag)
        {
            if (textView == null)
            {
                Log(tag + ": textView=null");
                return;
            }

            string contentType = textView.TextBuffer?.ContentType?.TypeName ?? "(null)";
            string roles = DescribeRoles(textView);
            int length = textView.TextBuffer?.CurrentSnapshot?.Length ?? -1;
            LogFormat(
                "{0}: contentType={1}, roles=[{2}], bufferLen={3}, isWpf={4}, hash={5}",
                tag,
                contentType,
                roles,
                length,
                textView is IWpfTextView,
                textView.GetHashCode());
        }

        internal static string Truncate(string text, int maxLength = 120)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "(empty)";
            }

            text = text.Replace("\r", "\\r").Replace("\n", "\\n");
            if (text.Length <= maxLength)
            {
                return text;
            }

            return text.Substring(0, maxLength) + "...";
        }

        private static void WriteDebug(string message)
        {
            System.Diagnostics.Debug.WriteLine("[CommentTranslator.SigHelp] " + message);
            System.Diagnostics.Trace.WriteLine("[CommentTranslator.SigHelp] " + message);
        }

        private static string DescribeRoles(ITextView textView)
        {
            if (textView?.Roles == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (string role in textView.Roles)
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(role);
            }

            return sb.ToString();
        }
    }
}

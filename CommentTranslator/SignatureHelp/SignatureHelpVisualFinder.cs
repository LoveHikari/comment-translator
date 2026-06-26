using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;

namespace CommentTranslator.SignatureHelp
{
    internal static class SignatureHelpVisualFinder
    {
        internal static FrameworkElement FindPresenterSurface(IWpfTextView editorView)
        {
            FrameworkElement fromEditor = FindFromRoot(editorView?.VisualElement as DependencyObject);
            if (fromEditor != null)
            {
                return fromEditor;
            }

            if (PresentationSource.CurrentSources != null)
            {
                foreach (PresentationSource source in PresentationSource.CurrentSources)
                {
                    if (source?.RootVisual is DependencyObject rootVisual)
                    {
                        FrameworkElement fromSource = FindFromRoot(rootVisual);
                        if (fromSource != null)
                        {
                            return fromSource;
                        }
                    }
                }
            }

            if (Application.Current?.Windows != null)
            {
                foreach (Window window in Application.Current.Windows)
                {
                    FrameworkElement fromWindow = FindFromRoot(window);
                    if (fromWindow != null)
                    {
                        return fromWindow;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 签名帮助气泡的最外层容器，用于在其正下方放置独立 Popup。
        /// </summary>
        internal static FrameworkElement FindSignatureHelpTooltipRoot(FrameworkElement presenter)
        {
            if (presenter == null)
            {
                return null;
            }

            FrameworkElement best = presenter;
            double bestArea = GetArea(presenter);

            for (DependencyObject node = VisualTreeHelper.GetParent(presenter);
                 node != null;
                 node = VisualTreeHelper.GetParent(node))
            {
                if (!(node is FrameworkElement element))
                {
                    continue;
                }

                if (ShouldStopAtParent(element))
                {
                    break;
                }

                double area = GetArea(element);
                if (area >= bestArea)
                {
                    bestArea = area;
                    best = element;
                }
            }

            return best;
        }

        internal static double GetContentWidth(FrameworkElement element)
        {
            if (element != null && element.ActualWidth > 0)
            {
                return element.ActualWidth;
            }

            return 480;
        }

        internal static CustomPopupPlacement[] PlaceBelowTarget(
            Size popupSize,
            Size targetSize,
            Point offset)
        {
            return new[]
            {
                new CustomPopupPlacement(new Point(0, targetSize.Height + 2), PopupPrimaryAxis.Horizontal)
            };
        }

        private static bool ShouldStopAtParent(FrameworkElement element)
        {
            if (element is Window)
            {
                return true;
            }

            string name = element.GetType().Name;
            return name.IndexOf("TextViewHost", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("TextSurfaceElement", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static double GetArea(FrameworkElement element)
        {
            if (element == null)
            {
                return 0;
            }

            element.UpdateLayout();
            return Math.Max(0, element.ActualWidth) * Math.Max(0, element.ActualHeight);
        }

        private static FrameworkElement FindFromRoot(DependencyObject root)
        {
            if (root == null)
            {
                return null;
            }

            return FindByTypeNameContains(root, "SignatureHelpPresenterSurfaceElement")
                ?? FindByTypeNameContains(root, "SignatureHelpPresenter");
        }

        private static FrameworkElement FindByTypeNameContains(DependencyObject node, string typeNamePart)
        {
            if (node == null)
            {
                return null;
            }

            if (node.GetType().Name.IndexOf(typeNamePart, StringComparison.OrdinalIgnoreCase) >= 0
                && node is FrameworkElement element)
            {
                return element;
            }

            int count = VisualTreeHelper.GetChildrenCount(node);
            for (int i = 0; i < count; i++)
            {
                FrameworkElement found = FindByTypeNameContains(VisualTreeHelper.GetChild(node, i), typeNamePart);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}

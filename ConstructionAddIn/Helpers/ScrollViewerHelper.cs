using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ConstructionAddIn.Helpers
{
    public static class ScrollViewerHelper
    {
        public static readonly DependencyProperty EnableCustomMouseWheelScrollProperty =
            DependencyProperty.RegisterAttached(
                "EnableCustomMouseWheelScroll",
                typeof(bool),
                typeof(ScrollViewerHelper),
                new PropertyMetadata(false, OnEnableCustomMouseWheelScrollChanged));

        public static bool GetEnableCustomMouseWheelScroll(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableCustomMouseWheelScrollProperty);
        }

        public static void SetEnableCustomMouseWheelScroll(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableCustomMouseWheelScrollProperty, value);
        }

        private static void OnEnableCustomMouseWheelScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer scrollViewer)
            {
                if ((bool)e.NewValue)
                {
                    scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;
                }
                else
                {
                    scrollViewer.PreviewMouseWheel -= OnPreviewMouseWheel;
                }
            }
        }

        private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }
    }
}
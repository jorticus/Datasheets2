using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Datasheets2
{
    public static class UIHelper
    {
        // https://stackoverflow.com/questions/974598/find-all-controls-in-wpf-window-by-type
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject ob)
             where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(ob); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(ob, i);
                if (child != null && child is T)
                {
                    yield return (T)child;
                }

                foreach (T childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }

        public static IEnumerable<T> FindLogicalChildren<T>(DependencyObject ob)
            where T : DependencyObject
        {
            foreach (DependencyObject child in LogicalTreeHelper.GetChildren(ob))
            {
                if (child != null && child is T)
                {
                    yield return (T)child;
                }

                foreach (T childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }
    }
}

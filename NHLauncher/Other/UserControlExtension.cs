using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHLauncher.Other
{
    public static class UserControlExtension
    {
        public static Window? GetWindow(this Control userControl)
        {
            var parent = userControl.Parent;
            while (parent != null)
            {
                if (parent is Window window)
                    return window;
                parent = parent.Parent;
            }
            return null;
        }
    }
}

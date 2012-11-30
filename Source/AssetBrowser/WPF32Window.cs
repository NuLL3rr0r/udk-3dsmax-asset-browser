using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetBrowser
{
    class WPF32Window : System.Windows.Forms.IWin32Window
    {
        public IntPtr Handle { get; private set; }

        public WPF32Window(System.Windows.Window wpfWindow)
        {
            Handle = new System.Windows.Interop.WindowInteropHelper(wpfWindow).Handle;
        }
    }
}


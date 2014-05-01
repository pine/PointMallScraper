using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Credit.PointMall.Scraper
{
    /// <summary>
    /// EventHandler - adaptor to call C# back from JavaScript or DOM event handlers
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    class DomEventHandler
    {
        [ComVisible(false)]
        public delegate bool Callback(object[] args);

        [ComVisible(false)]
        private Callback callback;
        
        [DispId(0)]
        public object Method(params object[] args)
        {
            return callback(args); // Type.Missing is "undefined" in JavaScript
        }

        public DomEventHandler(Callback callback)
        {
            this.callback = callback;
        }
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class DomEventHandler<T>
    {
        [ComVisible(false)]
        public delegate T Callback(object[] args);

        [ComVisible(false)]
        private Callback callback;

        [DispId(0)]
        public object Method(params object[] args)
        {
            return (object)callback(args);
        }

        public DomEventHandler(Callback callback)
        {
            this.callback = callback;
        }
    }
}

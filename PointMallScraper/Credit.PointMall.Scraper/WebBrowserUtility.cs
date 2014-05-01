using System.Reflection;
using System.Windows.Controls;

namespace Credit.PointMall.Scraper
{
    static class WebBrowserUtility
    {
        public static void SetSilent(
            this WebBrowser webBrowser,
            bool isSilent
            )
        {
            // IWebBrowser2 の取得 プロパティから
            var axIWebBrowser2 = typeof(WebBrowser).GetProperty("AxIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            var comObj = axIWebBrowser2.GetValue(webBrowser, null);
            
            // 値の設定
            comObj.GetType().InvokeMember(
                "Silent",
                BindingFlags.SetProperty,
                null,
                comObj,
                new object[] { isSilent }
                );
        }
    }
}

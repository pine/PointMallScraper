using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Timers;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Credit.PointMall.Scraper.Api
{
    abstract public class PointMallApi : IDisposable
    {
        protected WebBrowser Browser { get; set; }
        protected string UserName { get; set; }
        protected string Password { get; set; }

        abstract protected string RedirectCheckUrl { get; }

        private bool TimeoutEnabled { get; set; }
        private MovedListener NextDelegate { get; set; }
        private bool ReadyStateRedirect { get; set; }
        private bool ReadyStateCompleted { get; set; }
        private bool ReadyStateUrlCheck { get; set; }
        private string ReadyStateUrl { get; set; }

        private Timer TimeoutTimer { get; set; }
        private Timer ReadyStateTimer { get; set; }

        public EventHandler<string> Failure = (o, s) => { };
        public EventHandler<int> ProgressMaxChanged = (o, v) => { };
        public EventHandler<int> ProgressValueChanged = (o, v) => { };
        public EventHandler<int> LinkLengthChanged = (o, v) => { };
        public EventHandler<List<Shop>> Ended = (o, v) => { };

        public delegate void MovedListener(bool isTimeout);

        public PointMallApi()
        {
            this.Browser = null;
            this.UserName = null;
            this.Password = null;

            this.TimeoutEnabled = false;
            this.NextDelegate = null;
            this.ReadyStateRedirect = false;
            this.ReadyStateCompleted = false;
            this.ReadyStateUrlCheck = false;
            this.ReadyStateUrl = null;

            this.TimeoutTimer = new Timer();
            this.TimeoutTimer.AutoReset = false; // 繰り返し無効
            this.TimeoutTimer.Elapsed += this.TimeoutTimer_Elapsed;

            this.ReadyStateTimer = new Timer();
            this.ReadyStateTimer.AutoReset = true;
            this.ReadyStateTimer.Interval = 50;
            this.ReadyStateTimer.Elapsed += this.ReadyStateTimer_Elapsed;
        }


        public void Start(WebBrowser browser, string userName, string password, int timeout)
        {
            this.Browser = browser;

            this.Browser.LoadCompleted -= Browser_LoadCompleted;
            this.Browser.LoadCompleted += Browser_LoadCompleted;

            this.UserName = userName;
            this.Password = password;

            this.ProgressMaxChanged(this, 0);
            this.ProgressValueChanged(this, 0);

            if (timeout > 0)
            {
                this.TimeoutEnabled = true;
                this.TimeoutTimer.Interval = timeout;
            }
            else
            {
                this.TimeoutEnabled = false;
            }

            this.Start();
        }

        abstract protected void Start();

        protected void Move(
            MovedListener next,
            string url = null,
            bool isRedirect = false,
            bool isCompletedTiming = false,
            bool isUrlCheck = false
            )
        {
            // 移動後に実行する処理
            this.NextDelegate = next;
            this.ReadyStateRedirect = isRedirect;
            this.ReadyStateCompleted = isCompletedTiming;
            this.ReadyStateUrlCheck = isUrlCheck;
            this.ReadyStateUrl = url;

            // タイムアウトが有効な場合
            if (this.TimeoutEnabled)
            {
                this.TimeoutTimer.Stop();
                this.TimeoutTimer.Start();
            }

            // 移動開始
            if (!string.IsNullOrEmpty(url))
            {
                this.Browser.Navigate(url);
            }

            // 文書読み込みを待機
            if (!isCompletedTiming)
            {
                this.ReadyStateTimer.Start();
            }
        }
        
        protected mshtml.HTMLDocument Document
        {
            get
            {
                return (mshtml.HTMLDocument)this.Browser.Document;
            }
        }

        protected string SanitizeShopUrl(string url)
        {
            Uri uri = new Uri(url);

            return uri.GetLeftPart(UriPartial.Path);
        }

        protected string GetHostName(string url)
        {
            Uri uri = new Uri(url);

            return uri.Host;
        }

        private bool CheckRedirectEnded()
        {
            if (this.ReadyStateRedirect)
            {
                if (this.Browser.Source != null)
                {
                    var url = this.Browser.Source.AbsoluteUri;

                    if (!url.StartsWith(this.RedirectCheckUrl))
                    {
                        return true;
                    }
                }

                return false;
            }

            return false;
        }

        private bool CheckUrl()
        {
            if (this.ReadyStateUrlCheck)
            {
                var url = this.Browser.Source.AbsoluteUri;

                return url == this.ReadyStateUrl;
            }

            return false;
        }

        #region イベントハンドラ

        /// <summary>
        /// タイムアウトの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimeoutTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.Browser.Dispatcher.Invoke(() =>
            {
                if (this.NextDelegate != null)
                {
                    var next = this.NextDelegate;
                    this.NextDelegate = null;

                    this.Browser.Navigate("about:blank");
                    next(true);
                }
            });
        }

        /// <summary>
        /// ロード状態監視タイマー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReadyStateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // デリゲートを呼び出す
            Action callDelegate = () =>
            {
                if (this.NextDelegate != null && !this.ReadyStateCompleted)
                {
                    // 一度変数に格納しないと null が代入できない
                    // デリゲートを呼び出してから null 代入すると、
                    // デリゲート中から次のロード処理が実行し、正常に動作しない
                    var next = this.NextDelegate;
                    this.NextDelegate = null;

                    this.TimeoutTimer.Stop();
                    next(false); // 処理呼び出し
                }
            };

            try
            {
                this.Browser.Dispatcher.Invoke(() =>
                {
                    // 別スレッド実行なため、タイマーが既に停止していないか確認する
                    if (this.ReadyStateTimer.Enabled)
                    {
                        // リダイレクトが有効な場合は、リダイレクトが完了しているか調べる
                        if (this.CheckRedirectEnded())
                        {
                            callDelegate();
                            return;
                        }

                        var document = this.Browser.Document as mshtml.HTMLDocument;

                        if (document != null)
                        {
                            if ((document.readyState == "interactive" || document.readyState == "complete") &&
                                document.documentElement != null &&
                                (!this.ReadyStateRedirect || this.CheckRedirectEnded()) &&
                                (!this.ReadyStateUrlCheck || this.CheckUrl()))
                            {
                                try
                                {
                                    ((mshtml.IHTMLElement2)document.documentElement).doScroll("left");
                                }
                                catch (COMException)
                                {
                                    return;
                                }

                                this.ReadyStateTimer.Stop();

                                callDelegate();
                            }
                        }
                    }
                });
            }
            catch (TaskCanceledException) { } // 急にタスクを中断するときの発生する例外
        }

        
        private void Browser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            if (this.ReadyStateUrl == null ||
                this.Browser.Source.AbsoluteUri == this.ReadyStateUrl)
            {
                if (this.NextDelegate != null)
                {
                    var next = this.NextDelegate;
                    this.NextDelegate = null;

                    this.TimeoutTimer.Stop();
                    next(false);
                }
            }
        }

        #endregion

        #region Dispose Finalize パターン

        /// <summary>
        /// 既にDisposeメソッドが呼び出されているかどうかを表します。
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// ConsoleApplication1.DisposableClass1 によって使用されているすべてのリソースを解放します。
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.Dispose(true);
        }

        /// <summary>
        /// ConsoleApplication1.DisposableClass1 クラスのインスタンスがGCに回収される時に呼び出されます。
        /// </summary>
        ~PointMallApi()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// ConsoleApplication1.DisposableClass1 によって使用されているアンマネージ リソースを解放し、オプションでマネージ リソースも解放します。
        /// </summary>
        /// <param name="disposing">マネージ リソースとアンマネージ リソースの両方を解放する場合は true。アンマネージ リソースだけを解放する場合は false。 </param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }
            this.disposed = true;

            if (disposing)
            {
                // マネージ リソースの解放処理をこの位置に記述します。
                if (this.TimeoutTimer != null) { this.TimeoutTimer.Stop(); }
                if (this.ReadyStateTimer != null) { this.ReadyStateTimer.Stop(); }
            }
            // アンマネージ リソースの解放処理をこの位置に記述します。
        }

        /// <summary>
        /// 既にDisposeメソッドが呼び出されている場合、例外をスローします。
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">既にDisposeメソッドが呼び出されています。</exception>
        protected void ThrowExceptionIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        #endregion
    }
}

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
    /// <summary>
    /// オリコモールへアクセスし情報を取得するクラス
    /// </summary>
    public class OricoMallApi : PointMallApi
    {
        private const string OricoMallTopUrl = "http://www.oricomall.com";
        private const string OricoMallShopList = "http://www.oricomall.com/shop_list/indexed/";

        private List<Shop> ShopInfoLinks { get; set; }
        private List<Shop> ShopLinks { get; set; }
        private int ShopInfoIndex { get; set; }

        public OricoMallApi()
            : base()
        {
            this.ShopInfoLinks = null;
            this.ShopLinks = null;
            this.ShopInfoIndex = 0;
        }

        protected override void Start()
        {
            this.Move(this.MoveLoginPage, OricoMallTopUrl);
        }

        protected override string RedirectCheckUrl
        {
            get
            {
                return OricoMallTopUrl;
            }
        }

        private void MoveLoginPage(bool isTimeout)
        {
            // タイムアウト
            if (isTimeout)
            {
                this.Fail("タイムアウトになりました。");
                return;
            }

            // ログインボタンを取得
            var loginButton = this.Document.getElementsByClassName("btnLogin");

            if (loginButton.Count > 0)
            {
                // 未ログインの場合
                if (loginButton[0].innerText == "ログイン")
                {
                    // リンクを取得
                    var link = loginButton[0].getElementsByTagName("a");

                    if (link.Count > 0)
                    {
                        this.Move(this.Login, isCompletedTiming: true);
                        link[0].click();
                    }

                    else
                    {
                        // ログインリンク取得失敗
                        this.Fail("ログインリンクの取得に失敗しました");
                    }
                }

                else
                {
                    // ログイン済みの場合
                    this.Move(this.GetShopList, OricoMallShopList, isCompletedTiming: true);
                }
            }
        }

        private void GetShopList(bool isTimeout)
        {
            // タイムアウト
            if (isTimeout)
            {
                this.Fail("タイムアウトになりました。");
                return;
            }

            var shopList = this.Document.getElementById("shop_list");

            if (shopList == null)
            {
                this.Fail("ショップ一覧の取得に失敗しました");
                return;
            }

            var links = shopList.getElementsByTagName("a");
            var enabledLinks = new List<Shop>();

            foreach (var link in links)
            {
                var href = (string)link.getAttribute("href");
                var text = link.innerText;

                if (href != null && href.IndexOf("/shop/") > -1)
                {
                    enabledLinks.Add(new Shop { PointMallUrl = href, Name = text });
                }
            }

            this.ShopInfoLinks = enabledLinks;
            this.ShopInfoIndex = 0;
            this.ShopLinks = new List<Shop>();

            this.ProgressMaxChanged(this, this.ShopInfoLinks.Count);
            this.ProgressValueChanged(this, 0);

            this.GetShopInfoNext();
        }

        private void GetShopInfoNext()
        {
            // 進行状況を更新
            if (this.ShopInfoIndex > 0)
            {
                this.ProgressValueChanged(this, this.ShopInfoIndex);
            }

            // 終了判定
            if (this.ShopInfoIndex == this.ShopInfoLinks.Count)
            {
                this.End();
                return;
            }

            ++this.ShopInfoIndex;
            this.Move(this.GetShopInfo, this.CurrentShop.PointMallUrl, isUrlCheck: true);
        }

        private void GetShopInfo(bool isTimeout)
        {
            // タイムアウト
            if (isTimeout)
            {
                this.GetShopInfoNext();
                return;
            }

            var linkArea = this.Document.getElementsByClassName("go2shop");

            if (linkArea.Count > 0)
            {
                var link = linkArea[0].getElementsByTagName("a");

                if (link.Count > 0)
                {
                    var url = link[0].getAttribute("href") as string;

                    if (string.IsNullOrEmpty(url))
                    {
                        this.GetShopInfoNext();
                        return;
                    }

                    this.Move(this.GetShopUrl, url, true);
                }

                else
                {
                    this.GetShopInfoNext();
                }
            }

            else
            {
                // 失敗した場合
                this.GetShopInfoNext();
            }
        }

        private void GetShopUrl(bool isTimeout)
        {
            // タイムアウト
            if (isTimeout)
            {
                this.GetShopInfoNext();
                return;
            }

            var url = this.Browser.Source.AbsoluteUri;
            this.CurrentShop.Url = this.SanitizeShopUrl(url);
            this.CurrentShop.HostName = this.GetHostName(url);

            this.ShopLinks.Add(this.CurrentShop);
            this.LinkLengthChanged(this, this.ShopLinks.Count);

            this.GetShopInfoNext();
        }

        private void Login(bool isTimeout)
        {
            // タイムアウト
            if (isTimeout)
            {
                this.Fail("タイムアウトになりました。");
                return;
            }

            var loginId = this.Document.getElementById("loginId");
            var password = this.Document.getElementById("password");
            var captcha = this.Document.getElementById("captchaString");

            if (loginId != null && password != null && captcha != null)
            {
                loginId.setAttribute("value", this.UserName);
                password.setAttribute("value", this.Password);

                // 日本語の画像認証なため、日本語入力を ON にする
                captcha.focus();

                var loginButton = this.Document.getElementById("connectLogin");

                
                captcha.attachEvent("onkeypress", (args) => {
                    const int ENTER_KEY = 13;

                    var e = args[0] as mshtml.IHTMLEventObj;

                    if (e != null && e.keyCode == ENTER_KEY)
                    {
                        loginButton.click();
                    }

                    return true;
                });

                // ログイン後の処理
                this.Move(this.LoggedIn);
            }

            else
            {
                this.Fail("ログインに失敗しました。");
            }
        }

        private void LoggedIn(bool isTimeout)
        {
            // タイムアウト
            if (isTimeout)
            {
                this.Fail("タイムアウトになりました。");
                return;
            }

            var loginButtton = this.Document.getElementsByClassName("btn-em-01");

            // ログインが完了していない場合
            if (loginButtton.Count > 0)
            {
                this.Login(isTimeout);
                return;
            }

            this.Move(this.GetShopList, OricoMallShopList);
        }

        private void End()
        {
            this.Ended(this, this.ShopLinks);
        }

        private Shop CurrentShop
        {
            get
            {
                return this.ShopInfoLinks[this.ShopInfoIndex - 1];
            }
        }
    }
}

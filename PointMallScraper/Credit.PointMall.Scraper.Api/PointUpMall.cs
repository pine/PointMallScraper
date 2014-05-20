using System.Collections.Generic;
using System.Diagnostics;

namespace Credit.PointMall.Scraper.Api
{
    /// <summary>
    /// 三井住友カードのポイントモール、
    /// ポイントUPモールへアクセスし情報を取得するクラス
    /// </summary>
    public class PointUpMall : PointMallApi
    {
        private const string LoginTopUrl = "https://www.smbc-card.com/mem/vps/index.jsp";
        private const string ShopListUrl = "http://mall.smbc-card.com/shop_list/indexed/";
        private const string RedirectCheckUrlImpl = "http://mall.smbc-card.com";

        private List<Shop> ShopInfoLinks { get; set; }
        private List<Shop> ShopLinks { get; set; }
        private int ShopInfoIndex { get; set; }

        public PointUpMall()
            : base()
        {
            this.ShopInfoLinks = null;
            this.ShopLinks = null;
            this.ShopInfoIndex = 0;
        }

        protected override void Start()
        {
            this.Move(
                this.LoginTopLoaded,
                LoginTopUrl
                );
        }

        protected override string RedirectCheckUrl
        {
            get
            {
                return RedirectCheckUrlImpl;
            }
        }

        /// <summary>
        /// ログインページのトップページがロードされた時の処理
        /// </summary>
        /// <param name="isTimeout"></param>
        private void LoginTopLoaded(bool isTimeout)
        {
            if (isTimeout)
            {
                this.TimeoutFailure();
                return;
            }

            // ログイン済みか判定する
            var inputForm = this.Document.getElementByName("InForm");

            if (inputForm != null)
            {
                // 未ログイン
                var id = this.Document.getElementByName("userid");
                var pass = this.Document.getElementByName("password");
                var loginButton = this.Document.getElementByName("login01");

                // 入力フォームが取得できた場合
                if (id != null && pass != null && loginButton != null)
                {
                    id.setAttribute("value", this.UserName);
                    pass.setAttribute("value", this.Password);

                    loginButton.click();
                    this.Move(this.LoggedIn);
                }

                else
                {
                    this.Fail("ログインに失敗しました");
                }
            }

            else
            {
                // ログイン済み

            }
        }

        /// <summary>
        /// ログイン後の処理
        /// </summary>
        /// <param name="isTimeout"></param>
        private void LoggedIn(bool isTimeout)
        {
            // タイムアウト
            if (isTimeout)
            {
                this.TimeoutFailure();
                return;
            }
            
            // ログインフォーム
            var inputForm = this.Document.getElementByName("InForm");

            // ログインしたか判定する
            if (inputForm == null)
            {
                // ポイントモールのトップページへ移動する
                this.Move(this.MovedShopList, ShopListUrl, isCompletedTiming: true);
            }

            else
            {
                this.Fail("ログインに失敗しました。ログイン情報が正しくない可能性があります。");
            }

        }

        private void MovedShopList(bool isTimeout)
        {
            // タイムアウト
            if (isTimeout)
            {
                this.TimeoutFailure();
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

            var linkArea = this.Document.getElementByClassName("go2shop");

            if (linkArea != null)
            {
                var link = linkArea.getElementByTagName("a");

                if (link != null)
                {
                    var url = link.getAttribute("href") as string;

                    if (string.IsNullOrEmpty(url))
                    {
                        this.GetShopInfoNext();
                        return;
                    }

                    this.Move(this.GetShopUrl, url, isRedirect: true);
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

            Debug.WriteLine(url);

            this.ShopLinks.Add(this.CurrentShop);
            this.LinkLengthChanged(this, this.ShopLinks.Count);

            this.GetShopInfoNext();
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

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
        private const string TokimekiPointTownLoginComponentUrl = "https://www.aeon.co.jp/tpt/login/LoginComponent.html";
        private const string TokimekiPointTownTopUrl = "http://www.aeon.co.jp/tpt/";
        private const string TokimekiPointTownShopListUrl = "http://www.aeon.co.jp/tpt/shared/shop_list.json";
        private const string TokimekiPointTownShopUrl = "https://www.aeon.co.jp/tpt/shop/{0}/index.html";
        private const string TokimekiPointTownRedirectCheckUrl = "https://www.aeon.co.jp/";

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
                this.MoveLoginPage,
                TokimekiPointTownLoginComponentUrl,
                useCheckDelegate: this.LoginComponentLoadedCompleteChecker
                );
        }

        protected override string RedirectCheckUrl
        {
            get
            {
                return TokimekiPointTownRedirectCheckUrl;
            }
        }

        private bool LoginComponentLoadedCompleteChecker()
        {
            return this.Document.getElementById("login_unit") != null;
        }

        private void MoveLoginPage(bool isTimeout)
        {
            // タイムアウト
            if (isTimeout)
            {
                this.Failure(this, "タイムアウトになりました。");
                return;
            }

            // ログインボタンを取得
            var loginButton = this.Document.getElementsByClassName("btn_login");

            if (loginButton.Count > 0)
            {
                // リンクを取得
                var link = loginButton[0].getElementsByTagName("a");

                if (link.Count > 0)
                {
                    link[0].click();
                    this.Move(this.Login, isCompletedTiming: true);
                }

                else
                {
                    // ログインリンク取得失敗
                    this.Failure(this, "ログインリンクの取得に失敗しました");
                }
            }

            else
            {
                // ログイン済みの場合
                this.GetJson(this.ShopListLoaded, TokimekiPointTownShopListUrl);
            }
            
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

            var linkArea = this.Document.getElementByClassName("btn_shop");

            if (linkArea != null)
            {
                var link = linkArea.getElementsByTagName("a");

                if (link.Count > 0)
                {
                    var url = link[0].getAttribute("href") as string;

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

        private void Login(bool isTimeout)
        {
            // タイムアウト
            if (isTimeout)
            {
                this.Failure(this, "タイムアウトになりました。");
                return;
            }

            var loginId = this.Document.getElementByName("userId");
            var password = this.Document.getElementByName("password");

            if (loginId != null && password != null)
            {
                loginId.setAttribute("value", this.UserName);
                password.setAttribute("value", this.Password);

                var loginButton = this.Document.getElementById("btn-login");

                if (loginButton != null)
                {
                    loginButton.click();

                    // ログイン後の処理
                    this.Move(this.LoggedIn);
                }

                else
                {
                    this.Failure(this, "ログインボタンの取得に失敗しました。");
                }
            }

            else
            {
                this.Failure(this, "ログインに失敗しました。");
            }
        }

        private void LoggedIn(bool isTimeout)
        {
            // タイムアウト
            if (isTimeout)
            {
                this.Failure(this, "タイムアウトになりました。");
                return;
            }

            var loginButtton = this.Document.getElementById("btn-login");

            // ログインが完了していない場合
            if (loginButtton != null)
            {
                this.Login(isTimeout);
                return;
            }

            this.GetJson(this.ShopListLoaded, TokimekiPointTownShopListUrl);
        }

        private void ShopListLoaded(dynamic json)
        {
            if (json == null)
            {
                this.Failure(this, "ショップ一覧 JSON の取得に失敗しました。");
                return;
            }

            this.ShopInfoLinks = new List<Shop>();
            this.ShopLinks = new List<Shop>();
            this.ShopInfoIndex = 0;

            foreach (dynamic shop in json)
            {
                string acsCode = shop.pc.acs_code as string;
                string name = shop.name;

                var shopObj = new Shop {
                    Name = name,
                    PointMallUrl = this.CreateShopInfoUrl(acsCode)
                };

                this.ShopInfoLinks.Add(shopObj);
            }

            this.ProgressMaxChanged(this, this.ShopInfoLinks.Count);
            this.ProgressValueChanged(this, 0);

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

        private string CreateShopInfoUrl(string acsCode)
        {
            return string.Format(TokimekiPointTownShopUrl, acsCode);
        }
    }
}

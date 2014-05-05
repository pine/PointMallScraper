using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Credit.PointMall.Scraper
{
    class SettingUtility
    {
        private static string CreateUserNameKey(string id)
        {
            return "UserName_" + id;
        }

        private static string CreatePasswordKey(string id)
        {
            return "Password_" + id;
        }

        /// <summary>
        /// 設定に保存されたユーザー名を取得する
        /// </summary>
        /// <returns></returns>
        internal static string LoadSettingUserName(string id)
        {
            if (id != null)
            {
                var key = CreateUserNameKey(id);
                return Properties.Settings.Default[key] as string;
            }

            return null;
        }

        /// <summary>
        /// 設定にユーザー名を保存する
        /// </summary>
        /// <returns></returns>
        internal static void SaveSettingUserName(string id, string userName)
        {
            if (id != null)
            {
                var key = CreateUserNameKey(id);
                Properties.Settings.Default[key] = userName;
            }
        }

        /// <summary>
        /// 設定に保存されたパスワードを取得する
        /// </summary>
        /// <returns></returns>
        internal static string LoadSettingPassword(string id)
        {
            if (id != null)
            {
                var key = CreatePasswordKey(id);
                return Properties.Settings.Default[key] as string;
            }

            return null;
        }

        /// <summary>
        /// 設定にパスワードを保存する
        /// </summary>
        /// <returns></returns>
        internal static void SaveSettingPassword(string id, string password)
        {
            if (id != null)
            {
                var key = CreatePasswordKey(id);
                Properties.Settings.Default[key] = password;
            }
        }
    }
}

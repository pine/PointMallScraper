using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Credit.PointMall.Scraper.Api
{
    public class PointMall
    {
        public static PointMall[] GetList()
        {
            return new[]{
                new PointMall(typeof(OricoMallApi), "オリコ")
            };
        }

        /// <summary>
        /// カード会社の名前
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        private Type ApiType { get; set; }

        /// <summary>
        /// API のインスタンスを取得
        /// </summary>
        public PointMallApi CreateApiInstance()
        {
           return (PointMallApi)Activator.CreateInstance(this.ApiType);
        }

        public override string ToString()
        {
            return this.Name;
        }

        public PointMall(Type apiType, string name)
        {
            this.ApiType = apiType;
            this.Name = name;
        }
    }
}

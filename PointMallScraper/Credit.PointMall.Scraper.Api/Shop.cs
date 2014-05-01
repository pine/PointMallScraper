using System.Runtime.Serialization;

namespace Credit.PointMall.Scraper.Api
{
    [DataContract]
    public class Shop
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "hostName")]
        public string HostName { get; set; }

        [DataMember(Name = "pointMallUrl")]
        public string PointMallUrl { get; set; }
    }
}

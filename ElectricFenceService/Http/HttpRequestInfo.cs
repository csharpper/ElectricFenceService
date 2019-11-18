using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricFenceService.Http
{
    public class HttpRequestInfo
    {
        public string Sort { get; private set; }
        public SortSource[] Source { get; private set; }

        public static HttpRequestInfo Create(string url)
        {
            string req = url.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (string.IsNullOrEmpty(req))
                return null;
            string[] strs = req.Split(new char[] { '?' }, StringSplitOptions.RemoveEmptyEntries);
            if (strs != null && strs.Length > 0)
            {
                HttpRequestInfo info = new HttpRequestInfo()
                {
                    Sort = strs[0].ToLower()
                };
                if (strs.Length >= 2)
                {
                    string[] datas = strs[1].Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
                    List<SortSource> sources = new List<SortSource>();
                    foreach (var data in datas)
                    {
                        var infos = data.Split(new char[] { '=' });
                        if (infos.Length == 2)
                            sources.Add(new SortSource(infos[0], infos[1]));
                    }
                    info.Source = sources.ToArray();
                }
                return info;
            }
            return null;
        }
    }
}

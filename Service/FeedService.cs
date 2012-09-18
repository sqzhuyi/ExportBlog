using System;
using System.Collections.Generic;
using System.Text;

namespace ExportBlog
{
    /// <summary>
    /// 资源抓取服务
    /// </summary>
    public class FeedService
    {
        IFeedService service = null;
        Exception excep = null;

        public FeedService(Source src, string user)
        {
            user = user.ToLower();
            try
            {
                switch (src)
                {
                    case Source._163:
                        service = new B163Service(user);
                        break;
                    case Source._51cto:
                        service = new B51CtoService(user);
                        break;
                    case Source.chinaunix:
                        service = new ChinaUnixService(user);
                        break;
                    case Source.cnblogs:
                        service = new CnBlogsService(user);
                        break;
                    case Source.csdn:
                        service = new CsdnService(user);
                        break;
                    case Source.hexun:
                        service = new HexunService(user);
                        break;
                    case Source.iteye:
                        service = new ItEyeService(user);
                        break;
                    case Source.oschina:
                        service = new OschinaService(user);
                        break;
                    case Source.sina:
                        service = new SinaService(user);
                        break;
                    case Source.sohu:
                        service = new SohuService(user);
                        break;
                }
            }
            catch (Exception ex)
            {
                excep = ex;
            }
        }
        private IList<FeedEntity> _list = null;

        public IList<FeedEntity> GetList()
        {
            if (excep != null)
            {
                throw excep;
            }
            if (_list == null)
            {
                _list = service.GetList();
            }
            return _list;
        }
        public bool GetContent(ref FeedEntity entity)
        {
            if (entity.Content != string.Empty) return true;

            System.Threading.Thread.Sleep(2000);

            return service.GetContent(ref entity);
        }

        public FeedEntity GetEntity(string url)
        {
            if (_list == null)
            {
                _list = new List<FeedEntity>();
            }
            foreach (var m in _list)
            {
                if (m.Url == url)
                    return m;
            }
            var entity = service.GetEntity(url);
            entity.Url = url;
            _list.Add(entity);
            return entity;
        }
    }
}

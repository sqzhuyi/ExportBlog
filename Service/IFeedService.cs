using System;
using System.Collections.Generic;

namespace ExportBlog
{
    internal interface IFeedService
    {
        /// <summary>
        /// 获取所有的文章列表
        /// </summary>
        /// <returns></returns>
        IList<FeedEntity> GetList();

        /// <summary>
        /// 获取文章内容
        /// </summary>
        /// <param name="entity"></param>
        bool GetContent(ref FeedEntity entity);

        /// <summary>
        /// 通过文章url获取完整文章内容
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        FeedEntity GetEntity(string url);
    }
}

using System;
using System.Collections.Generic;

namespace NetPing_modern.ViewModels
{
    public class PostViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ShortBody { get; set; }
        public string Body { get; set; }
        public TermViewModel Category { get; set; }
        public DateTime Created { get; set; }
        public string Url { get; set; }
        public bool IsTop { get; set; }
        public List<TagViewModel> Tags { get; set; }
        public string Preview { get; set; }

        public Dictionary<string, string> MetaHtml { get; set; }

        /// <summary>
        /// поиск значения ключа метотегов
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetMetaValueToKey(string key)
        {
            return (MetaHtml != null && MetaHtml.Count > 0 && MetaHtml.ContainsKey(key)) ? MetaHtml[key] : null;
        }
    }
}
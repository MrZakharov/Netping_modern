using System;
using System.Collections.Generic;

namespace NetPing.Models
{
    /// <summary>
    /// Stores information about the post and related devices
    /// </summary>
    [Serializable]
    public class Post
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Body {  get;set; }
        public List<SPTerm> Devices { get; set; }
        public SPTerm Category { get; set; }
        public DateTime Created { get; set; }
        public string Url_name { get; set; }
        public bool IsTop { get; set; }


        /// <summary>
        /// Для заполнения meta HTML страницы
        /// </summary>
        public Dictionary<string, string> MetaHtml { get; set; }
    }
}
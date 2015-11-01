using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NetPing.Global.Config;
using Newtonsoft.Json.Linq;
using NetPing_modern.DAL.Model;

namespace NetPing_modern.Services.Confluence
{
    internal class ConfluenceClient : IConfluenceClient
    {

        public class ContentNotFoundException : Exception
        {
            public ContentNotFoundException(int contentId) : base(string.Format("Confluence content with id = {0} was not found", contentId))
            {

            }

            public ContentNotFoundException(string spaceKey, string title)
                : base(string.Format("Confluence content with space key = '{0}' and title = '{1}' was not found", spaceKey, title))
            {
            }
        }

        private IConfig _config;
        private readonly Dictionary<int, string> _cache = new Dictionary<int, string>();

        public ConfluenceClient(IConfig config)
        {
            _config = config;
        }

        private async Task<string> GetContentAsync(int id, Func<int, string, string> parser)
        {
            if (_cache.ContainsKey(id))
            {
                return parser(id, _cache[id]);
            }

            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(string.Format("{0}:{1}", _config.ConfluenceSettings.Login, _config.ConfluenceSettings.Password));
            var base64Auth = System.Convert.ToBase64String(plainTextBytes);

            NetworkCredential credential = new NetworkCredential(_config.ConfluenceSettings.Login, _config.ConfluenceSettings.Password);
            var handler = new HttpClientHandler { Credentials = credential };
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(_config.ConfluenceSettings.Url);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);

                var response =
                    client.GetAsync(string.Format("wiki/rest/api/content/{0}?expand=body.view&os_authType=basic", id));
                if (response.Result.IsSuccessStatusCode)
                {
                    StreamContent content = (StreamContent)response.Result.Content;
                    var task = content.ReadAsStringAsync();
                    string result = task.Result;
                    string parsed = parser(id, result);
                    /*using (StreamWriter outfile = new StreamWriter("C:\\tmp\\confluence\\" + id.ToString() + ".txt"))
                    {
                        outfile.Write(parsed);
                    }*/
                    return parsed;
                }
            }
            throw new ContentNotFoundException(id);
        }

        public async Task<string> GetContenAsync(int id)
        {
            return await GetContentAsync(id, ParseResult);
        }

        public async Task<string> GetContentTitleAsync(int id)
        {
            return await GetContentAsync(id, ParseTitle);
        }

        private string ParseTitle(int id, string content)
        {
            if (!_cache.ContainsKey(id))
            {
                _cache[id] = content;
            }

            if (IsJson(content))
            {
                dynamic obj = JObject.Parse(content);
                if (obj.type == "page")
                {
                    return obj.title;
                }
                throw new NotImplementedException(string.Format("The type {0} is not implemented", obj.type));
            }
            return content;
        }

        private string ParseResult(int id, string result)
        {
            if (!_cache.ContainsKey(id))
            {
                _cache[id] = result;
            }
            
            if (IsJson(result))
            {
                return ParseJson(result);
            }
            return result;
        }

        private string ParseJson(string result)
        {
            dynamic obj = JObject.Parse(result);
            if (obj.type == "page")
            {
                return ParsePage(obj);
            }
            throw new NotImplementedException(string.Format("The type {0} is not implemented", obj.type));
        }

        private string ParsePage(dynamic page)
        {
            if (page.link != null)
            {
                if (page.link.Type == JTokenType.Array && page.link.Count > 0)
                {
                    return page.body.value;
                }
            }
            else if (page.body != null && page.body.view != null)
            {
                var value = page.body.view.value;
                value = FixImageLinks(value.ToString());
                return value;
            }
            return string.Empty;
        }

        private readonly Regex _imgRegex = new Regex(@"\<img class=""confluence-embedded-image""[^\>]+src=""(?<src>[^""]+)""[^>]+data-base-url=""(?<baseurl>[^""]+)""[^>]+\>");
        private const string WikiPrefix = "/wiki";

        private object FixImageLinks(string value)
        {
            return _imgRegex.Replace(value, new MatchEvaluator(ConfluenceImage));
        }

        private string ConfluenceImage(Match match)
        {
            var str = match.ToString();
            if (match.Success)
            {
                var srcGroup = match.Groups["src"];
                var baseurlGroup = match.Groups["baseurl"];
                if (srcGroup.Success && baseurlGroup.Success)
                {
                    var url = baseurlGroup.Value;
                    if (url.LastIndexOf(WikiPrefix, StringComparison.InvariantCultureIgnoreCase) != -1)
                    {
                        url = url.Substring(0, url.Length - WikiPrefix.Length);
                    }
                    url = url + srcGroup.Value;

                    return str.Replace(srcGroup.Value, url);
                }
            }

            return str;
        }

        private bool IsJson(string result)
        {
            return result.StartsWith("{") && result.EndsWith("}");
        }

        public async Task<int> GetContentBySpaceAndTitle(string spaceKey, string title)
        {
            NetworkCredential credential = new NetworkCredential(_config.ConfluenceSettings.Login, _config.ConfluenceSettings.Password);
            var handler = new HttpClientHandler { Credentials = credential };
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(_config.ConfluenceSettings.Url);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response =
                    client.GetAsync(string.Format("wiki/rest/api/content?title={0}&spaceKey={1}&os_authType=basic", title, spaceKey));
                if (response.Result.IsSuccessStatusCode)
                {
                    StreamContent content = (StreamContent)response.Result.Content;
                    var task = content.ReadAsStringAsync();
                    string stringContent = task.Result;
                    dynamic results = JObject.Parse(stringContent);
                    if (results.results != null)
                    {
                        if (results.results.Type == JTokenType.Array && results.results.Count > 0)
                        {
                            if (results.results[0].id != null)
                            {
                                return int.Parse(results.results[0].id.Value);
                            }
                        }
                    }
                    throw new ContentNotFoundException(spaceKey, title);
                }
            }
            throw new ContentNotFoundException(spaceKey, title);
        }

        private readonly Regex _contentIdRegex = new Regex(@"pageId=(?<id>\d+)");
/*
        public int? GetContentIdFromUrl(string url)
        {
            var mc = _contentIdRegex.Matches(url);
            if (mc.Count > 0)
            {
                Match m = mc[0];
                if (m.Success)
                {
                    Group group = m.Groups["id"];
                    int id = int.Parse(group.Value);

                    return id;
                }
            }
            return null;
        }
 */
        private readonly Regex _spaceTitleRegex = new Regex(@"\/display\/(?<spaceKey>[\w \.\-\+%]+)\/(?<title>[\w \.\-\+%]+)?");
        public int? GetContentIdFromUrl(string url)
        {
            var mc = _contentIdRegex.Matches(url);

            if (mc.Count > 0)
            {
                Match m = mc[0];
                if (m.Success)
                {
                    Group group = m.Groups["id"];
                    int id= int.Parse(group.Value);
                    if (id > 0) return id;

                }
            }
            else
            {
                mc = _spaceTitleRegex.Matches(url);
                if (mc.Count > 0)
                {
                    Match m = mc[0];
                    if (m.Success)
                    {
                        Group spaceKeyGroup = m.Groups["spaceKey"];
                        string spaceKey = spaceKeyGroup.Value;

                        Group titleGroup = m.Groups["title"];
                        string title = titleGroup.Value;

                        var contentTask = GetContentBySpaceAndTitle(spaceKey, title);
                        int contentId = contentTask.Result;
                        if (contentId > 0) return contentId;
                    }
                }
            }
            return null;
        }

        public UserManualModel GetUserManual(int id, int itemId)
        {
            var userManual = new UserManualModel();
            userManual.ItemId = itemId;
            NetworkCredential credential = new NetworkCredential(_config.ConfluenceSettings.Login, _config.ConfluenceSettings.Password);
            var handler = new HttpClientHandler { Credentials = credential };
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(_config.ConfluenceSettings.Url);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response =
                    client.GetAsync(string.Format("wiki/rest/api/content/{0}?os_authType=basic", id));
                if (response.Result.IsSuccessStatusCode)
                {
                    StreamContent content = (StreamContent)response.Result.Content;
                    var task = content.ReadAsStringAsync();
                    string stringContent = task.Result;
                    dynamic results = JObject.Parse(stringContent);
                    var regex = new Regex("\\[([^)]*)\\] ");
                    if(results.id != null && !string.IsNullOrEmpty(results.title.Value))
                    {
                        userManual.Id = int.Parse(results.id.Value);
                        userManual.Title = regex.Replace((results.title.Value as string).Replace(".", "%2E").Replace("/", "%2F"), string.Empty);
                        userManual.Pages = GetUserManualPages(userManual.Id);
                    }
                }
            }

            return userManual;
        }

        private ICollection<PageModel> GetUserManualPages(int id)
        {
            var pages = new List<PageModel>();
            NetworkCredential credential = new NetworkCredential(_config.ConfluenceSettings.Login, _config.ConfluenceSettings.Password);
            var handler = new HttpClientHandler { Credentials = credential };
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(_config.ConfluenceSettings.Url);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response =
                    client.GetAsync(string.Format("wiki/rest/api/content/{0}/child/page?os_authType=basic", id));
                if (response.Result.IsSuccessStatusCode)
                {
                    StreamContent content = (StreamContent)response.Result.Content;
                    var task = content.ReadAsStringAsync();
                    string stringContent = task.Result;
                    dynamic results = JObject.Parse(stringContent);
                    var regex = new Regex("\\[([^)]*)\\] ");
                    if (results.results != null)
                    {
                        if (results.results.Type == JTokenType.Array && results.results.Count > 0)
                        {
                           foreach(var result in results.results)
                           {
                               var pageId = int.Parse(result.id.Value);
                               var pageTitle = regex.Replace((result.title.Value as string).Replace(".", "%2E"), string.Empty);
                               var subPages = new List<PageModel>();
                               var pageContent = string.Empty;
                               if (IsTreePage(pageId))
                                   subPages = GetUserManualPages(pageId);
                               else
                                   pageContent = GetUserManualPageContent(pageId);

                               pages.Add(new PageModel
                                   {
                                       Content = pageContent,
                                       Id = pageId,
                                       Title = pageTitle,
                                       Pages = subPages
                                   });
                           }
                        }
                    }
                }
            }
            return pages;
        }

        private string GetUserManualPageContent(int id)
        {
            NetworkCredential credential = new NetworkCredential(_config.ConfluenceSettings.Login, _config.ConfluenceSettings.Password);
            var handler = new HttpClientHandler { Credentials = credential };
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(_config.ConfluenceSettings.Url);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response =
                    client.GetAsync(string.Format("wiki/rest/api/content/{0}?os_authType=basic&expand=body.view", id));
                if (response.Result.IsSuccessStatusCode)
                {
                    StreamContent content = (StreamContent)response.Result.Content;
                    var task = content.ReadAsStringAsync();
                    string stringContent = task.Result;
                    dynamic results = JObject.Parse(stringContent);
                    if (results.body != null && results.body.view != null && results.body.view.value != null)
                    {
                        return results.body.view.value.Value;
                    }
                }
            }

            return string.Empty;
        }

        private bool IsTreePage(int id)
        {
            NetworkCredential credential = new NetworkCredential(_config.ConfluenceSettings.Login, _config.ConfluenceSettings.Password);
            var handler = new HttpClientHandler { Credentials = credential };
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(_config.ConfluenceSettings.Url);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response =
                    client.GetAsync(string.Format("wiki/rest/api/content/{0}/child/page?os_authType=basic", id));
                if (response.Result.IsSuccessStatusCode)
                {
                    StreamContent content = (StreamContent)response.Result.Content;
                    var task = content.ReadAsStringAsync();
                    string stringContent = task.Result;
                    dynamic results = JObject.Parse(stringContent);
                    if (results.size != null)
                    {
                        return results.size.Value > 0;
                    }
                }
            }

            return false;
        }
    }
}
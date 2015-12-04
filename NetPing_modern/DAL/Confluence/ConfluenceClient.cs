using System;
using System.Collections.Generic;
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
        private readonly IConfig _config;

        private readonly Dictionary<Int32, String> _cache = new Dictionary<Int32, String>();

        private readonly Regex _imgRegex = new Regex(@"\<img class=""confluence-embedded-image""[^\>]+src=""(?<src>[^""]+)""[^>]+data-base-url=""(?<baseurl>[^""]+)""[^>]+\>");

        private const String WikiPrefix = "/wiki";

        private readonly Regex _contentIdRegex = new Regex(@"pageId=(?<id>\d+)");

        private readonly Regex _spaceTitleRegex = new Regex(@"\/display\/(?<spaceKey>[\w \.\-\+%]+)\/(?<title>[\w \.\-\+%]+)?");

        public class ContentNotFoundException : Exception
        {
            public ContentNotFoundException(Int32 contentId) : base(
                $"Confluence content with id = {contentId} was not found")
            {

            }

            public ContentNotFoundException(String spaceKey, String title)
                : base($"Confluence content with space key = '{spaceKey}' and title = '{title}' was not found")
            {
            }
        }

        public ConfluenceClient(IConfig config)
        {
            _config = config;
        }

        private async Task<String> GetContentAsync(Int32 id, Func<Int32, String, String> parser)
        {
            if (_cache.ContainsKey(id))
            {
                return parser(id, _cache[id]);
            }

            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(
                $"{_config.ConfluenceSettings.Login}:{_config.ConfluenceSettings.Password}");

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
                    client.GetAsync($"wiki/rest/api/content/{id}?expand=body.view&os_authType=basic");

                if (response.Result.IsSuccessStatusCode)
                {
                    StreamContent content = (StreamContent)response.Result.Content;
                    var task = content.ReadAsStringAsync();
                    String result = task.Result;
                    String parsed = parser(id, result);
                    
                    return parsed;
                }
            }
            throw new ContentNotFoundException(id);
        }

        public async Task<String> GetContenAsync(Int32 id)
        {
            return await GetContentAsync(id, ParseResult);
        }

        public async Task<String> GetContentTitleAsync(Int32 id)
        {
            return await GetContentAsync(id, ParseTitle);
        }

        private String ParseTitle(Int32 id, String content)
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

                throw new NotImplementedException(String.Format("The type {0} is not implemented", obj.type));
            }
            return content;
        }

        private String ParseResult(Int32 id, String result)
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

        private String ParseJson(String result)
        {
            dynamic obj = JObject.Parse(result);

            if (obj.type == "page")
            {
                return ParsePage(obj);
            }

            throw new NotImplementedException(String.Format("The type {0} is not implemented", obj.type));
        }

        private String ParsePage(dynamic page)
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
            return String.Empty;
        }

        private Object FixImageLinks(String value)
        {
            return _imgRegex.Replace(value, new MatchEvaluator(ConfluenceImage));
        }

        private String ConfluenceImage(Match match)
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

        private Boolean IsJson(String result)
        {
            return result.StartsWith("{") && result.EndsWith("}");
        }

        public async Task<Int32> GetContentBySpaceAndTitle(String spaceKey, String title)
        {
            NetworkCredential credential = new NetworkCredential(_config.ConfluenceSettings.Login, _config.ConfluenceSettings.Password);
            var handler = new HttpClientHandler { Credentials = credential };
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(_config.ConfluenceSettings.Url);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response =
                    client.GetAsync(String.Format("wiki/rest/api/content?title={0}&spaceKey={1}&os_authType=basic", title, spaceKey));
                if (response.Result.IsSuccessStatusCode)
                {
                    StreamContent content = (StreamContent)response.Result.Content;
                    var task = content.ReadAsStringAsync();
                    String stringContent = task.Result;
                    dynamic results = JObject.Parse(stringContent);
                    if (results.results != null)
                    {
                        if (results.results.Type == JTokenType.Array && results.results.Count > 0)
                        {
                            if (results.results[0].id != null)
                            {
                                return Int32.Parse(results.results[0].id.Value);
                            }
                        }
                    }
                    throw new ContentNotFoundException(spaceKey, title);
                }
            }
            throw new ContentNotFoundException(spaceKey, title);
        }

        public Int32? GetContentIdFromUrl(String url)
        {
            var mc = _contentIdRegex.Matches(url);

            if (mc.Count > 0)
            {
                Match m = mc[0];
                if (m.Success)
                {
                    Group group = m.Groups["id"];
                    Int32 id= Int32.Parse(group.Value);
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
                        String spaceKey = spaceKeyGroup.Value;

                        Group titleGroup = m.Groups["title"];
                        String title = titleGroup.Value;

                        var contentTask = GetContentBySpaceAndTitle(spaceKey, title);
                        Int32 contentId = contentTask.Result;
                        if (contentId > 0) return contentId;
                    }
                }
            }
            return null;
        }

        public UserManualModel GetUserManual(Int32 id, Int32 itemId)
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
                    client.GetAsync(String.Format("wiki/rest/api/content/{0}?os_authType=basic", id));
                if (response.Result.IsSuccessStatusCode)
                {
                    StreamContent content = (StreamContent)response.Result.Content;
                    var task = content.ReadAsStringAsync();
                    String stringContent = task.Result;
                    dynamic results = JObject.Parse(stringContent);
                    var regex = new Regex("\\[([^)]*)\\] ");
                    if(results.id != null && !String.IsNullOrEmpty(results.title.Value))
                    {
                        userManual.Id = Int32.Parse(results.id.Value);
                        userManual.Title = regex.Replace((results.title.Value as String).Replace(".", "%2E"), String.Empty);
                        userManual.Pages = GetUserManualPages(userManual.Id);
                    }
                }
            }

            return userManual;
        }

        private ICollection<PageModel> GetUserManualPages(Int32 id)
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
                    client.GetAsync(String.Format("wiki/rest/api/content/{0}/child/page?os_authType=basic", id));
                if (response.Result.IsSuccessStatusCode)
                {
                    StreamContent content = (StreamContent)response.Result.Content;
                    var task = content.ReadAsStringAsync();
                    String stringContent = task.Result;
                    dynamic results = JObject.Parse(stringContent);
                    var regex = new Regex("\\[([^)]*)\\] ");
                    if (results.results != null)
                    {
                        if (results.results.Type == JTokenType.Array && results.results.Count > 0)
                        {
                           foreach(var result in results.results)
                           {
                               var pageId = Int32.Parse(result.id.Value);
                               var pageTitle = regex.Replace((result.title.Value as String).Replace(".", "%2E"), String.Empty);
                               var subPages = new List<PageModel>();
                               var pageContent = String.Empty;
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

        private String GetUserManualPageContent(Int32 id)
        {
            NetworkCredential credential = new NetworkCredential(_config.ConfluenceSettings.Login, _config.ConfluenceSettings.Password);
            var handler = new HttpClientHandler { Credentials = credential };
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(_config.ConfluenceSettings.Url);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response =
                    client.GetAsync(String.Format("wiki/rest/api/content/{0}?os_authType=basic&expand=body.view", id));
                if (response.Result.IsSuccessStatusCode)
                {
                    StreamContent content = (StreamContent)response.Result.Content;
                    var task = content.ReadAsStringAsync();
                    String stringContent = task.Result;
                    dynamic results = JObject.Parse(stringContent);
                    if (results.body != null && results.body.view != null && results.body.view.value != null)
                    {
                        return results.body.view.value.Value;
                    }
                }
            }

            return String.Empty;
        }

        private Boolean IsTreePage(Int32 id)
        {
            NetworkCredential credential = new NetworkCredential(_config.ConfluenceSettings.Login, _config.ConfluenceSettings.Password);
            var handler = new HttpClientHandler { Credentials = credential };
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri(_config.ConfluenceSettings.Url);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response =
                    client.GetAsync(String.Format("wiki/rest/api/content/{0}/child/page?os_authType=basic", id));
                if (response.Result.IsSuccessStatusCode)
                {
                    StreamContent content = (StreamContent)response.Result.Content;
                    var task = content.ReadAsStringAsync();
                    String stringContent = task.Result;
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
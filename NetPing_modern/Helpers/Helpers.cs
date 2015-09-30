using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using NetPing.Global.Config;
using NetPing.Models;
using NetPing.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Web.Mvc;
using System.Text;
using System.Text.RegularExpressions;
using NetPing_modern.Services.Confluence;


namespace NetpingHelpers
{
    public static class Helpers
    {
        public static IEnumerable<HTMLInjection> GetHtmlInjections(IRepository repository = null)
        {
            if(repository == null)
                repository = new SPOnlineRepository(new ConfluenceClient(new Config()));

            return repository.HtmlInjections;
        }

        public static IEnumerable<Device> GetNewDevices()
        {
            // should be removed
            // repository should be instantiated by DI container
            var repository = new SPOnlineRepository(new ConfluenceClient(new Config()));

            var devices = repository.Devices.Where(dev =>
                                    dev.Label.IsEqualStrId(NetPing_modern.Properties.Resources.Guid_Label_New)
                            );
            devices = devices.OrderByDescending(dev => dev.Created);
            return devices;
        }

        public static IEnumerable<Post> GetTopPosts()
        {
            // should be removed
            // repository should be instantiated by DI container
            var repository = new SPOnlineRepository(new ConfluenceClient(new Config()));

            var top_posts = repository.Posts.Where(p => p.IsTop).OrderByDescending(p=>p.Created);

            return top_posts;
        }

        public static bool IsCultureEng
        {
            get { 
                if (System.Globalization.CultureInfo.CurrentCulture.LCID==1033)  return true;
                return false;
            }
        }

        public static bool IsCultureRus
        {
            get
            {
                if (System.Globalization.CultureInfo.CurrentCulture.LCID == 1049) return true;
                return false;
            }
        }


        public static bool IsDebug(this HtmlHelper htmlHelper)
        {
#if DEBUG
      return true;
#else
            return false;
#endif
        }

        public static string ToFileUrlStr(this FieldUrlValue url, string file_name)
        {
            if (url == null) return "http://netping.ru/Pub/Files/" + file_name;
              else return url.Url;
        }

        public static List<SPTerm> ListNamesToListDesitnations(this List<SPTerm> names,IEnumerable<Device> devices)
        {
            List<SPTerm> result = new List<SPTerm>();

     
     
            //var repository = new SPOnlineRepository();
            foreach (var name in names)
            {
                //var device= repository.Devices.FirstOrDefault(dev => dev.Name == name);
                var device= devices.FirstOrDefault(dev => dev.Name == name);
                if (device != null)  result=result.Union(device.Destination).ToList();
            }
            return result;
        }

        public static string ToShortTextHTML(this string str,string url)
        {
            str=Regex.Replace(str, @"<(.|n)*?>", string.Empty).Replace("&nbsp", " ").Replace("&#160;"," "); // To clear text from HTML


    	    
            string[] words = str.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);		
		    StringBuilder sb = new StringBuilder();
		    for (int i = 0, count = 0; i < words.Length && count < 100; i++)
		    {
	    	    sb.AppendFormat("{0} ", words[i]);
			    count += words[i].Length + 1; // плюс пробел
		    }
            str = sb.ToString() + "<a href=\"" + url + "\"> " + NetPing_modern.Resources.Other.helpers_details+ "...</a>";
            return str;
        }


        // Get device names terms list, return destinations list for this devices
        public static List<SPTerm> DevicesToDestinations(this List<SPTerm> values, IRepository repository)
        {
            List<SPTerm> result=new List<SPTerm>();
            if (values == null || values.Count < 1)
                return result;
            
            foreach (SPTerm value in values)
            {
                List<SPTerm> dev_dests = repository.Devices.ToList().Find(i => i.Name.Name == value.Name).Destination;
                result=result.Union(dev_dests).ToList();
            }

            return result;
        }

        // Replace links inside Sharepoint HTML body
        public static string ReplaceInternalLinks(this string value)
        {
            //value=value.Replace("src=\"/web/feeds/Lists/Photos/", "src=\"https://netpingeastcoltd-public.sharepoint.com/Pub/Photos/Old_feeds/");
            //value=value.Replace("src=\"/web/Catalog/Lists/Photos/", "src=\"https://netpingeastcoltd-public.sharepoint.com/Pub/Photos/Old_catalog/");
            //value = value.Replace("src=\"/web/Lists/Photos/", "src=\"https://netpingeastcoltd-public.sharepoint.com/Pub/Photos/");

            value = value.Replace("=\"/wiki/download/", "=\"https://netping.atlassian.net/wiki/download/");
            return value;
        }

        public static List<string> ToStringList(this TaxonomyFieldValueCollection values)
        {
            var result = new List<string>();
            if (values == null || values.Count < 1)
                return result;

            foreach (var value in values)
            {
                result.Add(value.Label);
            }

            return result;
        }

        public static List<SPTerm> ToSPTermList(this TaxonomyFieldValueCollection values, IEnumerable<SPTerm> terms)
        {
            var result = new List<SPTerm>();
            if (values == null || values.Count < 1)
                return result;

            foreach (var value in values)
            {
                result.Add(value.ToSPTerm(terms));
            }

            return result; 
        }

        public static SPTerm ToSPTerm(this TaxonomyFieldValue value, IEnumerable<SPTerm> terms)
        {
            if (value == null)
                return new SPTerm();

            var guid = new Guid(value.TermGuid);

            return terms.FirstOrDefault(t => t.Id == guid);
        }

        public static string ToNormalString(this TaxonomyFieldValue value)
        {
            if (value == null)
                return string.Empty;

            return value.Label;
        }

        public static List<string> ToStringList(this FieldLookupValue[] values)
        {
            var result = new List<string>();
            if (values == null || values.Count() < 1)
                return result;

            foreach (var value in values)
            {
                result.Add(value.LookupValue);
            }

            return result;
        }

        public static SecureString ToSecureString(this string value)
        {
            var result = new SecureString();
            foreach (char c in value) result.AppendChar(c);
            
            return result;
        }
    }
}
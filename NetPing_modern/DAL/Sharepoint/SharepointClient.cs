using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using NetpingHelpers;
using NetPing.Models;
using NetPing_modern.DAL;
using Ninject.Infrastructure.Language;

namespace NetPing.DAL
{
    public class SharepointClient : IDisposable
    {
        private readonly SharepointClientParameters _parameters;
        private readonly ClientContext _context;

        public SharepointClient(SharepointClientParameters parameters)
        {
            _parameters = parameters;
            var credentials = new SharePointOnlineCredentials(parameters.User, parameters.Password.ToSecureString());

            _context = new ClientContext(parameters.Url)
            {
                RequestTimeout = parameters.RequestTimeout,
                Credentials = credentials
            };

            _context.ExecuteQuery();
        }

        private  ClientContext CreateClientContext()
        {
            var credentials = new SharePointOnlineCredentials(_parameters.User, _parameters.Password.ToSecureString());

            var context = new ClientContext(_parameters.Url)
            {
                RequestTimeout = _parameters.RequestTimeout,
                Credentials = credentials
            };

            return context;
        }

        public void DownloadFileToLocal(string fileref,string localpath, string filename)
        {
            if (!fileref.Contains("https:"))
            {
                fileref = UrlBuilder.GetSPFullUrl(fileref).ToString();
            }

            System.IO.Directory.CreateDirectory(localpath);

            var client = new WebClient();
            client.Credentials = _context.Credentials;
            client.Headers.Add("X-FORMS_BASED_AUTH_ACCEPTED", "f");

           client.DownloadFile(fileref, localpath+filename);
            client.Dispose();
        }

        public ListItem GetFolderParent(String folderUrl)
        {
            var itemFolder = _context.Web.GetFolderByServerRelativeUrl(folderUrl);
            var itemFolderParent = itemFolder.ParentFolder;
            var itemFolderParentItems = itemFolderParent.ListItemAllFields;

            _context.Load(itemFolder);
            _context.Load(itemFolderParent);
            _context.Load(itemFolderParentItems);

            _context.ExecuteQuery();

            return itemFolderParentItems;
        }

        public ListItemCollection GetList(String name, String query)
        {
            var list = _context.Web.Lists.GetByTitle(name);

            var camlquery = new CamlQuery {ViewXml = query};

            var items = list.GetItems(camlquery);

            _context.Load(list);
            _context.Load(items);
            _context.ExecuteQuery();

            return items;
        }

        public IEnumerable<SPTerm> GetTerms(String setName)
        {
            const Int32 englishLCID = 1033;

            var currentLCID = CultureInfo.CurrentCulture.LCID;
            
            var terms = new List<SPTerm>();

            var sortOrders = new SortOrdersCollection<Guid>();

            var session = TaxonomySession.GetTaxonomySession(_context);

            var termSets = session.GetTermSetsByName(setName, englishLCID);

            _context.Load(session, s=>s.TermStores);
            _context.Load(termSets);

            _context.ExecuteQuery();

            var termSet = termSets.First();
            
            var allTerms = termSet.GetAllTerms();

            _context.Load(allTerms);

            _context.ExecuteQuery();

            var guids = allTerms.ToList().Select(t => t.Id).ToList();

            Parallel.ForEach(guids, CreateClientContext, (id, state, context) =>
            {
                try
                {
                    GetTermItem(setName, context, englishLCID, id, currentLCID, terms, sortOrders);
                }
                catch (WebException)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));

                    GetTermItem(setName, context, englishLCID, id, currentLCID, terms, sortOrders);
                }

                return context;
            }
            , context => context.Dispose());
            
            var customSortOrder = sortOrders.GetSortOrders();

            terms.Sort(new SPTermComparerByCustomSortOrder(customSortOrder));

            if (terms.Count == 0) throw new Exception("No terms was readed!");

            return terms;
        }

        private static void GetTermItem(String setName, ClientContext context, Int32 englishLCID, Guid id, Int32 currentLCID,
            List<SPTerm> terms, SortOrdersCollection<Guid> sortOrders)
        {
            var lsession = TaxonomySession.GetTaxonomySession(context);

            var ltermSets = lsession.GetTermSetsByName(setName, englishLCID);

            context.Load(lsession, s => s.TermStores);
            context.Load(ltermSets);
            context.ExecuteQuery();

            var ltermSet = ltermSets.First();

            var term = ltermSet.GetTerm(id);

            context.Load(term);

            context.ExecuteQuery();

            var name = term.Name;

            var spTerm = new SPTerm
            {
                Id = term.Id,
                Name = name,
                Path = term.PathOfTerm,
                Properties = term.LocalCustomProperties
            };

            if (currentLCID != englishLCID) // If lcid label not avaliable or lcid==1033 keep default label
            {
                var langLabel = term.GetAllLabels(currentLCID);

                context.Load(langLabel);
                context.ExecuteQuery();

                if (langLabel.Count != 0)
                {
                    spTerm.Name = langLabel.First().Value;
                }
            }

            terms.Add(spTerm);

            if (!String.IsNullOrEmpty(term.CustomSortOrder))
            {
                sortOrders.AddSortOrder(term.CustomSortOrder);
            }
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
using System;
using System.Threading.Tasks;
using NetPing_modern.DAL.Model;

namespace NetPing_modern.Services.Confluence
{
    public interface IConfluenceClient
    {
        Task<String> GetContenAsync(Int32 id);

        Task<String> GetContentTitleAsync(Int32 id);

        Task<Int32> GetContentBySpaceAndTitle(String spaceKey, String title);

        Int32? GetContentIdFromUrl(String url);

        UserManualModel GetUserManual(Int32 id, Int32 itemId);
    }
}

using System.Threading.Tasks;
using NetPing_modern.DAL.Model;

namespace NetPing_modern.Services.Confluence
{
    public interface IConfluenceClient
    {
        Task<string> GetContenAsync(int id);

        Task<string> GetContentTitleAsync(int id);

        Task<int> GetContentBySpaceAndTitle(string spaceKey, string title);

        int? GetContentIdFromUrl(string url);

        UserManualModel GetUserManual(int id, int itemId);
    }
}

using Microsoft.SharePoint.Client;

namespace NetPing.DAL
{
    internal interface IListItemConverter<out T>
    {
        T Convert(ListItem listItem, SharepointClient sp=null);
    }
}
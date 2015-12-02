using System;
using Microsoft.SharePoint.Client;

namespace NetPing.DAL
{
    internal static class ListItemHelper
    {
        public static T Get<T>(this ListItem listItem, String key)
        {
            var field = listItem[key];

            if (field == null)
            {
                return default(T);
            }
            else
            {
                if (typeof(T) == typeof(Boolean))
                {
                    return (T)(object)Convert.ToBoolean(field);
                }

                return (T) field;
            }
        }
    }
}
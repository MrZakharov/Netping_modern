using System;
using Microsoft.SharePoint.Client;

namespace NetPing.DAL
{
    internal static class ListItemExtensions
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
                if (typeof (T) == typeof (Boolean) && !(field is Boolean))
                {
                    return (T) (object) Convert.ToBoolean(field);
                }
                else if (typeof (T) == typeof (Int32))
                {
                    return (T) (object) Int32.Parse(field.ToString());
                }
                else if (typeof (T) == typeof (Microsoft.SharePoint.Client.Taxonomy.TaxonomyFieldValueCollection) )
                {

           //         Microsoft.SharePoint.Client.Taxonomy.TaxonomyFieldValue aa;
           //         Microsoft.SharePoint.Client.Taxonomy.TaxonomyFieldValueCollection bb;
           //         bb = (Microsoft.SharePoint.Client.Taxonomy.TaxonomyFieldValueCollection) (object) aa;
                   

                }

                return (T) (object) field;
            }
        }
    }
}
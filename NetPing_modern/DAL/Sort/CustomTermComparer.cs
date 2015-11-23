using System;
using System.Collections.Generic;
using Microsoft.SharePoint.Client.Taxonomy;
using WebGrease.Css.Extensions;

namespace NetPing_modern.DAL
{
    internal class CustomTermComparer : IComparer<Term>
    {
        public int Compare(Term x, Term y)
        {
            var sortOrder = new List<Guid>();
            if (!string.IsNullOrEmpty(x.CustomSortOrder))
            {
                x.CustomSortOrder.Split(':').ForEach(id => sortOrder.Add(new Guid(id)));
            }

            if (!string.IsNullOrEmpty(y.CustomSortOrder))
            {
                y.CustomSortOrder.Split(':').ForEach(id =>
                                                     {
                                                         var guid = new Guid(id);
                                                         if (!sortOrder.Contains(guid))
                                                         {
                                                             sortOrder.Add(guid);
                                                         }
                                                     });
            }

            return IndexOf(sortOrder, x.Id).CompareTo(IndexOf(sortOrder, y.Id));
        }

        private int IndexOf(List<Guid> list, Guid id)
        {
            return list.IndexOf(id) + 1;
        }
    }
}
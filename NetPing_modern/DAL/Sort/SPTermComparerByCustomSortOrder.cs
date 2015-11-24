using System;
using System.Collections.Generic;
using NetPing.Models;

namespace NetPing_modern.DAL
{
    internal class SPTermComparerByCustomSortOrder : IComparer<SPTerm>
    {
        private readonly IList<Guid> _sortOrder = null;

        public SPTermComparerByCustomSortOrder(IList<Guid> customSortOrder)
        {
            _sortOrder = customSortOrder;
        }

        private int IndexOf(IList<Guid> list, Guid id)
        {
            return list.IndexOf(id) + 1;
        }

        public int Compare(SPTerm x, SPTerm y)
        {
            if (_sortOrder == null)
                return 0;

            return IndexOf(_sortOrder, x.Id).CompareTo(IndexOf(_sortOrder, y.Id));
        }
    }
}
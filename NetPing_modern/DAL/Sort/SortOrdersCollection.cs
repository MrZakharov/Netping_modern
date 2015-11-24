using System;
using System.Collections.Generic;
using WebGrease.Css.Extensions;

namespace NetPing_modern.DAL
{
    internal class SortOrdersCollection<T>
    {
        private readonly List<IList<T>> _sortOrderGroups = new List<IList<T>>();

        public void AddSortOrder(string sortOrder)
        {
            if (string.IsNullOrEmpty(sortOrder))
                return;

            var sortOrderGroup = new List<T>();
            sortOrder.Split(':').ForEach(id => sortOrderGroup.Add(GetValue(id)));

            _sortOrderGroups.Add(sortOrderGroup);
        }

        private T GetValue(string id)
        {
            object value = Guid.Parse(id);
            return (T) value;
        }

        private IList<T> _sortOrders; 

        public IList<T> GetSortOrders()
        {
            if (_sortOrders == null)
            {
                _sortOrders = MergeGroups(_sortOrderGroups);
                if (_sortOrders == null)
                {
                    return new List<T>();
                }
            }

            return new List<T>(_sortOrders);
        }

        private IList<T> MergeGroups(List<IList<T>> sortOrderGroups)
        {
            IList<T> merged = null;

            for (var i = 0; i < sortOrderGroups.Count - 1; i++)
            {
                if (merged == null)
                    merged = sortOrderGroups[i];

                merged = MergeLists(merged, sortOrderGroups[i + 1]);
            }
            return merged;
        }

        private IList<T> MergeLists(IList<T> first, IList<T> second, int startFrom = 0)
        {
            var a = new List<T>(first);
            var b = new List<T>(second);

            for (var bi = startFrom; bi < b.Count; bi++)
            {
                var ai = a.IndexOf(b[bi]);
                if (ai > -1)
                {
                    var length = a.Count - ai;
                    Move(a, 0, ai, b, bi);
                    bi = bi + length;
                    return MergeLists(a, b, bi);
                }
            }

            var i = startFrom;
            a.ForEach(id =>
                        {
                if (i > b.Count)
                {
                    b.Add(id);
                }
                else
                {
                            b.Insert(i, id);
                }
                            i++;
                        });

            return b;
        }

        private void Move(List<T> source, int startIndex, int endIndex, List<T> target, int position)
        {
            var i = startIndex;
            var pos = position;
            while (i < endIndex)
            {
                target.Insert(pos, source[startIndex]);
                source.RemoveAt(startIndex);
                pos++;
                i++;
            }
            source.RemoveAt(startIndex);
        }
    }
}
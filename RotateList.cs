using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TakeVideoScreenshot
{
    class RotateList<T> : IList<T>
    {
        private int offset;
        private readonly List<T> list;

        public int Offset
        {
            get => offset;
            set { if (Count != 0) offset = value % Count; }
        }

        public T this[int index] { get => list[ConvertIndex(index)]; set => list[ConvertIndex(index)] = value; }

        public int Count => list.Count;

        public bool IsReadOnly => false;

        public RotateList()
        {
            list = new List<T>();
        }

        public RotateList(IEnumerable<T> collection)
        {
            list = collection.ToList();
        }

        public void Next()
        {
            Offset++;
        }

        public void Previous()
        {
            Offset--;
        }

        public void Add(T item)
        {
            list.Insert((Count + Offset) % (Count + 1), item);
        }

        public void Clear()
        {
            list.Clear();
            Offset = 0;
        }

        public bool Contains(T item)
        {
            return list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new RotateListEnumerator<T>(this);
        }

        public int IndexOf(T item)
        {
            return IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            list.Insert((index + Offset) % (Count + 1), item);
        }

        public bool Remove(T item)
        {
            bool remove = list.Remove(item);

            if (Count == 0) Offset = 0;
            else if (Offset >= Count) Offset %= Count;

            return remove;
        }

        public void RemoveAt(int index)
        {
            list.RemoveAt(ConvertIndex(index));

            if (Count == 0) Offset = 0;
            else if (Offset >= Count) Offset %= Count;
        }

        private int ConvertIndex(int index)
        {
            return (index + Offset) % Count;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

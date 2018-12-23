using System;
using System.Collections;
using System.Collections.Generic;

namespace TakeVideoScreenshot
{
    class RotateListEnumerator<T> : IEnumerator<T>
    {
        private int index;
        public readonly RotateList<T> list;

        public T Current { get; private set; }

        object IEnumerator.Current => Current;

        public RotateListEnumerator(RotateList<T> list)
        {
            this.list = list;
            index = -1;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (++index >= list.Count) return false;

            Current = list[index];
            return true;
        }

        public void Reset()
        {
            index = -1;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Net
{
    public class SafeEnumerator<T> : IEnumerator<T>
    {
        private readonly IEnumerator<T> m_Inner;
        private readonly object m_Lock;

        public SafeEnumerator(IEnumerator<T> inner, object @lock)
        {
            m_Inner = inner;
            m_Lock = @lock;
            // entering lock in constructor
            Monitor.Enter(m_Lock);
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
            Monitor.Exit(m_Lock);
        }

        #endregion

        #region Implementation of IEnumerator

        public bool MoveNext()
        {
            return m_Inner.MoveNext();
        }

        public void Reset()
        {
            m_Inner.Reset();
        }

        public T Current
        {
            get { return m_Inner.Current; }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;

namespace Match3
{
    public class Register
    {

        private readonly HashSet<object> _objects = new HashSet<object>();
        private readonly object _lock = new object();

        private bool IsEmpty
        {
            get {
                lock (_lock)
                {
                    return _objects != null && _objects.Count == 0;
                } 
            }
        }

        public event EventHandler Empty;

        public void RegisterObject(object obj)
        {
            lock (_lock)
                _objects.Add(obj);
        }

        public void UnregisterObject(object obj)
        {
            lock (_lock) {
                _objects.Remove(obj);
                if (IsEmpty)
                    Empty?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsRegistered(object obj)
        {
            lock (_lock)
            {
                return _objects.Contains(obj);
            }
        }

    }
}

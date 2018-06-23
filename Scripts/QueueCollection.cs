using System.Collections;
using System.Collections.Generic;

namespace KannaBot.Scripts
{
    public class QueueCollection<T> where T : class 
    {
        private readonly Queue<T> _queue = new Queue<T>();

        public delegate void VoidDelegate();
        public event VoidDelegate OnChanged;

        public int Count => _queue.Count;

        public void Enqueue(T t)
        {
            _queue.Enqueue(t);
            OnChanged?.Invoke();
        }

        public T Dequeue()
        {
            var t = _queue.Dequeue();
            OnChanged?.Invoke();
            return t;
        }

        public void Clear()
        {
            _queue.Clear();
            OnChanged?.Invoke();
        }

        public T[] ToArray() => _queue.ToArray();
    }
}
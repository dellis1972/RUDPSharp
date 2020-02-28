using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RUDPSharp
{
    public class Pool<T> where T : new() {
        ConcurrentQueue<T> pool = new ConcurrentQueue<T> ();
        public Pool(int maxPool)
        {
            for (int i=0; i < maxPool; i++) {
                pool.Enqueue (new T());
            }
        }
        public T Rent (){
            if (pool.TryDequeue (out T item))
                return item;
            item = new T();
            return item;
        }

        public void Return (T item) {
            pool.Enqueue (item);
        }
    }
    public class BufferPool<T> {
        ConcurrentDictionary<int, ConcurrentQueue<T[]>> pool = new ConcurrentDictionary<int, ConcurrentQueue<T[]>> ();
        public BufferPool(int maxArraySize, int maxPool)
        {
            var queue = new ConcurrentQueue<T[]> ();
            pool.TryAdd (maxArraySize, queue);
            for (int i=0; i < maxPool; i++) {
                queue.Enqueue (new T[maxArraySize]);
            }
        }
        public T[] Rent (int length){
            if (pool.TryGetValue (length, out ConcurrentQueue<T[]> queue)) {
                if (queue.TryDequeue (out T[] buffer))
                    return buffer;
                buffer = new T[length];
                return buffer;
            }
            queue = new ConcurrentQueue<T[]> ();
            pool.TryAdd (length, queue);
            return new T[length];
        }

        public void Return (T[] buffer) {
            if (pool.TryGetValue (buffer.Length, out ConcurrentQueue<T[]> queue)) {
                queue.Enqueue (buffer);
                return;
            }
            queue = new ConcurrentQueue<T[]> ();
            queue.Enqueue (buffer);
            pool.TryAdd (buffer.Length, queue);
        }
    }
}
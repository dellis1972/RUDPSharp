using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Client
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
        ConcurrentDictionary<int, Queue<T[]>> pool = new ConcurrentDictionary<int, Queue<T[]>> ();
        public BufferPool(int maxArraySize, int maxPool)
        {
            var queue = new Queue<T[]> ();
            pool.TryAdd (maxArraySize, queue);
            for (int i=0; i < maxPool; i++) {
                queue.Enqueue (new T[maxArraySize]);
            }
        }
        public T[] Rent (int length){
            if (pool.TryGetValue (length, out Queue<T[]> queue)) {
                if (queue.TryDequeue (out T[] buffer))
                    return buffer;
                Console.WriteLine ("new buffer!!!");
                buffer = new T[length];
                return buffer;
            }
            queue = new Queue<T[]> ();
            pool.TryAdd (length, queue);
            return new T[length];
        }

        public void Return (T[] buffer) {
            if (pool.TryGetValue (buffer.Length, out Queue<T[]> queue)) {
                queue.Enqueue (buffer);
                return;
            }
            queue = new Queue<T[]> ();
            queue.Enqueue (buffer);
            pool.TryAdd (buffer.Length, queue);
        }
    }
}
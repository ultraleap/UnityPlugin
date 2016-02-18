using System;

namespace LeapCWrapper
{
    public class CircularIndexedQueue<T>
    {
        private T[] array;
        private int start = 0;
        private int end = 0;
        private int count = 0;
        private int capacity =0;
        private bool empty = true;

        public CircularIndexedQueue(int capacity)
        {
            if(capacity <= 0){
                capacity = 1;
            }
            this.array = new T[capacity];
            this.start = 0;
            this.end = 0;
            this.count = 0;
            this.capacity = capacity;
            this.empty = true;
        }
        
        public void Enqueue(T t)
        {
            array[end] = t;
            end++;
            if( end >= capacity){
                end = 0;
            }
            if(end == start){
                start++;
                if(start >= capacity){
                    start = 0;
                }
            }
            empty = false;
            if(count < capacity)
                count++;
        }
        
        public T Dequeue()
        {
            if(IsEmpty){
                return default(T);
            }
            T result = array[start];
            start++;
            if(start >= capacity){
                start = 0;
            } 
            if(Count == 1){
                empty = true;
                start = 0;
                end = 0;
            }
            count--;
            return result;
        }
        
        public int Count { get { 
                return count; 
            } 
        }
        
        public T this[int index]
        {
            get 
            { 
                return array[(start + index) % array.Length]; 
            }
        }
        public void Resize(int newCapacity){
            if(newCapacity <= capacity){
                return;
            }

            T[] newArray = new T[newCapacity];
            for(int i = 0; i < this.Count; i++){
                newArray[i] = this.Dequeue();
            }
            this.array = newArray;
            this.capacity = newCapacity;
        }

        public int Capacity{
            get{
                return capacity;
            }
        }
        public bool IsEmpty{
            get{
                return empty;
            }
        }
    }
}


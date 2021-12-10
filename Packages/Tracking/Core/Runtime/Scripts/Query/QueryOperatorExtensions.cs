/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Query
{

    /// <summary>
    /// A collection of extension methods that operate on a Query structure.  All of
    /// these methods destroy the original query and return a new Query based on 
    /// the original.
    /// </summary>
    public static class QueryOperatorExtensions
    {

        /// <summary> Don't put big r values into this! </summary>
        private static int getCombinationLength(int n, int r)
        {
            if (r > 8) throw new System.NotSupportedException();
            var result = 1;
            var rFac = Factorial(r);
            for (var i = 0; i < r; i++) result *= n - i;
            result /= rFac;
            return result;
        }
        /// <summary> Int32s, careful with overflow. </summary>
        private static int Factorial(int N)
        {
            int f = 1; for (var n = N; n > 1; n--) f *= n; return f;
        }

        /// <summary> 
        /// Prints a Debug.LogError if the query results contains more than a single element, but otherwise preserves the Query as-is. 
        /// </summary>
        public static Query<T> ComplainIfMany<T>(this Query<T> query, string message = null)
        {
            T[] array;
            int count;
            query.Deconstruct(out array, out count);

            if (count > 1)
            {
                Debug.LogError($"Query contained more than one element.");
            }

            return new Query<T>(array, count);
        }

        /// <summary>
        /// Returns a new Query operation representing the concatenation of the current Query to
        /// the argument ICollection.
        /// 
        /// For example
        ///   (A, B, C, D).Query().Concat((E, F, G, H))
        ///  would result in
        ///   (A, B, C, D, E, F, G, H)
        /// </summary>
        public static Query<T> Concat<T>(this Query<T> query, ICollection<T> collection)
        {
            using (var slice = query.Deconstruct())
            {
                var dstArray = ArrayPool<T>.Spawn(slice.Count + collection.Count);

                Array.Copy(slice.BackingArray, dstArray, slice.Count);
                collection.CopyTo(dstArray, slice.Count);

                return new Query<T>(dstArray, slice.Count + collection.Count);
            }
        }

        /// <summary>
        /// Returns a new Query representing the concatenation of the current Query to
        /// the argument Query.
        /// 
        /// For example
        ///   (A, B, C, D).Query().Concat((E, F, G, H))
        ///  would result in
        ///   (A, B, C, D, E, F, G, H)
        /// </summary>
        public static Query<T> Concat<T>(this Query<T> query, Query<T> other)
        {
            using (var slice = query.Deconstruct())
            using (var otherSlice = other.Deconstruct())
            {
                var dstArray = ArrayPool<T>.Spawn(slice.Count + otherSlice.Count);

                Array.Copy(slice.BackingArray, dstArray, slice.Count);
                Array.Copy(otherSlice.BackingArray, 0, dstArray, slice.Count, otherSlice.Count);

                return new Query<T>(dstArray, slice.Count + otherSlice.Count);
            }
        }

        /// <summary>
        /// Returns a new Query representing the original Query but without any duplicates.
        /// The order of the elements in the returned query is undefined.
        /// 
        /// For example
        ///  (A, B, C, A, B, C, D).Query().Distinct()
        /// Could result in
        ///  (B, A, C, D)
        /// </summary>
        public static Query<T> Distinct<T>(this Query<T> query)
        {
            T[] array;
            int count;
            query.Deconstruct(out array, out count);

            HashSet<T> set = Pool<HashSet<T>>.Spawn();

            for (int i = 0; i < count; i++)
            {
                set.Add(array[i]);
            }

            Array.Clear(array, 0, array.Length);
            count = 0;

            foreach (var item in set)
            {
                array[count++] = item;
            }

            set.Clear();
            Pool<HashSet<T>>.Recycle(set);

            return new Query<T>(array, count);
        }

        /// <summary>
        /// Returns a new Query representing only the items of the current Query that
        /// are of a specific type.
        /// 
        /// For example
        ///   ("A", 1, null, 5.0f, 900, "hello").Query().OfType(typeof(string))
        /// would result in
        ///   ("A", "hello")
        /// </summary>
        public static Query<T> OfType<T>(this Query<T> query, Type type)
        {
            using (var slice = query.Deconstruct())
            {
                var dstArray = ArrayPool<T>.Spawn(slice.Count);

                int dstCount = 0;
                for (int i = 0; i < slice.Count; i++)
                {
                    if (slice[i] != null &&
                        type.IsAssignableFrom(slice[i].GetType()))
                    {
                        dstArray[dstCount++] = slice[i];
                    }
                }

                return new Query<T>(dstArray, dstCount);
            }
        }

        /// <summary>
        /// Returns a new Query where the elements have been ordered using a selector function
        /// to select the values to order by.
        /// </summary>
        public static Query<T> OrderBy<T, K>(this Query<T> query, Func<T, K> selector) where K : IComparable<K>
        {
            T[] array;
            int count;
            query.Deconstruct(out array, out count);

            var comparer = FunctorComparer<T, K>.Ascending(selector);
            Array.Sort(array, 0, count, comparer);
            comparer.Clear();

            return new Query<T>(array, count);
        }

        /// <summary>
        /// Returns a new Query where the elements have been ordered using a selector function
        /// to select the values to order by.
        /// </summary>
        public static Query<T> OrderByDescending<T, K>(this Query<T> query, Func<T, K> selector) where K : IComparable<K>
        {
            T[] array;
            int count;
            query.Deconstruct(out array, out count);

            var comparer = FunctorComparer<T, K>.Descending(selector);
            Array.Sort(array, 0, count, comparer);
            comparer.Clear();

            return new Query<T>(array, count);
        }

        /// <summary>
        /// Returns a new Query representing the current Query repeated a number of
        /// times.
        /// 
        /// For example:
        ///   (1, 2, 3).Query().Repeat(3)
        /// Would result in:
        ///   (1, 2, 3, 1, 2, 3, 1, 2, 3)
        /// </summary>
        public static Query<T> Repeat<T>(this Query<T> query, int times)
        {
            if (times < 0)
            {
                throw new ArgumentException("The repetition count must be non-negative.");
            }

            using (var slice = query.Deconstruct())
            {
                var dstArray = ArrayPool<T>.Spawn(slice.Count * times);

                for (int i = 0; i < times; i++)
                {
                    Array.Copy(slice.BackingArray, 0, dstArray, i * slice.Count, slice.Count);
                }

                return new Query<T>(dstArray, slice.Count * times);
            }
        }

        /// <summary>
        /// Returns a new Query representing the original elements but in reverse order.
        /// 
        /// For example:
        ///  (1, 2, 3).Query().Reverse()
        /// Would result in:
        ///  (3, 2, 1)
        /// </summary>
        public static Query<T> Reverse<T>(this Query<T> query)
        {
            T[] array;
            int count;
            query.Deconstruct(out array, out count);

            array.Reverse(0, count);

            return new Query<T>(array, count);
        }

        /// <summary>
        /// Returns a new Query representing the current Query mapped element-by-element
        /// into a new Query by a mapping operation.
        /// 
        /// For example:
        ///   (1, 2, 3, 4).Query().Select(num => (num * 2).ToString())
        /// Would result in:
        ///   ("2", "4", "6", "8")
        /// </summary>
        public static Query<K> Select<T, K>(this Query<T> query, Func<T, K> selector)
        {
            using (var slice = query.Deconstruct())
            {
                var dstArray = ArrayPool<K>.Spawn(slice.Count);
                for (int i = 0; i < slice.Count; i++)
                {
                    dstArray[i] = selector(slice[i]);
                }

                return new Query<K>(dstArray, slice.Count);
            }
        }

        /// <summary>
        /// Returns a new Query representing the current Query mapped element-by-element
        /// into a new Query by a mapping operation. This variant accepts an auxiliary argument slot for the selector function, to prevent allocation.
        /// 
        /// For example:
        ///   (1, 2, 3, 4).Query().Select(10, (num, offset) => (num * 2 + offset).ToString())
        /// Would result in:
        ///   ("12", "14", "16", "18")
        /// </summary>
        public static Query<K> Select<T, Arg, K>(this Query<T> query, Arg arg, Func<T, Arg, K> selectorWithArg)
        {
            using (var slice = query.Deconstruct())
            {
                var dstArray = ArrayPool<K>.Spawn(slice.Count);
                for (int i = 0; i < slice.Count; i++)
                {
                    dstArray[i] = selectorWithArg(slice[i], arg);
                }

                return new Query<K>(dstArray, slice.Count);
            }
        }

        /// <summary>
        /// Returns a new Query representing the current Query mapped element-by-element
        /// into a new Query by a mapping operation. This variant accepts two auxiliary argument slots for the selector function, to prevent allocation.
        /// 
        /// For example:
        ///   (1, 2, 3, 4).Query().Select(10, (num, offset) => (num * 2 + offset).ToString())
        /// Would result in:
        ///   ("12", "14", "16", "18")
        /// </summary>
        public static Query<K> Select<T, Aux1, Aux2, K>(this Query<T> query, Aux1 aux1, Aux2 aux2, Func<T, Aux1, Aux2, K> selectorWithArg)
        {
            using (var slice = query.Deconstruct())
            {
                var dstArray = ArrayPool<K>.Spawn(slice.Count);
                for (int i = 0; i < slice.Count; i++)
                {
                    dstArray[i] = selectorWithArg(slice[i], aux1, aux2);
                }

                return new Query<K>(dstArray, slice.Count);
            }
        }

        /// <summary>
        /// Returns a new Query representing the current Query mapped element-by-element
        /// into a new Query by a mapping operation. This variant accepts three auxiliary argument slots for the selector function, to prevent allocation.
        /// 
        /// For example:
        ///   (1, 2, 3, 4).Query().Select(10, (num, offset) => (num * 2 + offset).ToString())
        /// Would result in:
        ///   ("12", "14", "16", "18")
        /// </summary>
        public static Query<K> Select<T, Aux1, Aux2, Aux3, K>(this Query<T> query, Aux1 aux1, Aux2 aux2, Aux3 aux3, Func<T, Aux1, Aux2, Aux3, K> selectorWithArg)
        {
            using (var slice = query.Deconstruct())
            {
                var dstArray = ArrayPool<K>.Spawn(slice.Count);
                for (int i = 0; i < slice.Count; i++)
                {
                    dstArray[i] = selectorWithArg(slice[i], aux1, aux2, aux3);
                }

                return new Query<K>(dstArray, slice.Count);
            }
        }

        /// <summary>
        /// Returns a new Query representing the current Query mapped element-by-element
        /// into a new Query by a mapping operation. This variant accepts four auxiliary argument slots for the selector function, to prevent allocation.
        /// 
        /// For example:
        ///   (1, 2, 3, 4).Query().Select(10, (num, offset) => (num * 2 + offset).ToString())
        /// Would result in:
        ///   ("12", "14", "16", "18")
        /// </summary>
        public static Query<K> Select<T, Aux1, Aux2, Aux3, Aux4, K>(this Query<T> query, Aux1 aux1, Aux2 aux2, Aux3 aux3, Aux4 aux4, Func<T, Aux1, Aux2, Aux3, Aux4, K> selectorWithArg)
        {
            using (var slice = query.Deconstruct())
            {
                var dstArray = ArrayPool<K>.Spawn(slice.Count);
                for (int i = 0; i < slice.Count; i++)
                {
                    dstArray[i] = selectorWithArg(slice[i], aux1, aux2, aux3, aux4);
                }

                return new Query<K>(dstArray, slice.Count);
            }
        }

        /// <summary>
        /// Returns a new Query representing the current Query where each element has been
        /// mapped onto a new Collection, and then all Collections are concatenated into a 
        /// single long sequence.
        /// 
        /// For example:
        ///   (1, 2, 3, 4).Query().SelectMany(count => new List().Fill(count, count.ToString()))
        /// Would result in:
        ///   ("1", "2", "2", "3", "3", "3", "4", "4", "4", "4")
        /// </summary>
        public static Query<K> SelectMany<T, K>(this Query<T> query, Func<T, ICollection<K>> selector)
        {
            using (var slice = query.Deconstruct())
            {
                int totalCount = 0;
                for (int i = 0; i < slice.Count; i++)
                {
                    totalCount += selector(slice[i]).Count;
                }

                var dstArray = ArrayPool<K>.Spawn(totalCount);

                int targetIndex = 0;
                for (int i = 0; i < slice.Count; i++)
                {
                    var collection = selector(slice[i]);
                    collection.CopyTo(dstArray, targetIndex);
                    targetIndex += collection.Count;
                }

                return new Query<K>(dstArray, totalCount);
            }
        }

        /// <summary>
        /// Returns a new Query representing the current Query where each element has been
        /// mapped onto a new Query, and then all Queries are concatenated into a 
        /// single long sequence.
        /// 
        /// For example:
        ///   (1, 2, 3, 4).Query().SelectMany(count => new List().Fill(count, count.ToString()).Query())
        /// Would result in:
        ///   ("1", "2", "2", "3", "3", "3", "4", "4", "4", "4")
        /// </summary>
        public static Query<K> SelectMany<T, K>(this Query<T> query, Func<T, Query<K>> selector)
        {
            using (var slice = query.Deconstruct())
            {
                var slices = ArrayPool<Query<K>.QuerySlice>.Spawn(slice.Count);
                int totalCount = 0;
                for (int i = 0; i < slice.Count; i++)
                {
                    slices[i] = selector(slice[i]).Deconstruct();
                    totalCount += slices[i].Count;
                }

                var dstArray = ArrayPool<K>.Spawn(totalCount);

                int targetIndex = 0;
                for (int i = 0; i < slice.Count; i++)
                {
                    Array.Copy(slices[i].BackingArray, 0, dstArray, targetIndex, slices[i].Count);
                    targetIndex += slices[i].Count;
                    slices[i].Dispose();
                }

                ArrayPool<Query<K>.QuerySlice>.Recycle(slices);

                return new Query<K>(dstArray, totalCount);
            }
        }

        /// <summary>
        /// Returns a new Query representing the current Query but without a
        /// certain number of the elements at the start.  This method is safe
        /// to call with a skip amount that is larger than the number of elements 
        /// in the sequence.
        /// 
        /// For example:
        ///   (A, B, C, D, E, F, G).Query().Skip(2)
        /// Would result in:
        ///   (C, D, E, F, G)
        /// </summary>
        public static Query<T> Skip<T>(this Query<T> query, int toSkip)
        {
            T[] array;
            int count;
            query.Deconstruct(out array, out count);

            int resultCount = Mathf.Max(count - toSkip, 0);
            toSkip = count - resultCount;
            Array.Copy(array, toSkip, array, 0, resultCount);
            Array.Clear(array, resultCount, array.Length - resultCount);

            return new Query<T>(array, resultCount);
        }

        /// <summary>
        /// Returns a new Query that skips values while the predicate returns true.  As soon
        /// as the predicate returns false, the operation returns the remainder of the sequence.  Even
        /// if the predicate becomes true again, the elements are still returned.
        /// 
        /// For example
        ///   (-1, -2, -5, 5, 9, -1, 5, -3).Query().SkipWhile(isNegative)
        /// Would result in 
        ///   (5, 9, -1, 5, -3)
        /// </summary>
        public static Query<T> SkipWhile<T>(this Query<T> query, Func<T, bool> predicate)
        {
            T[] array;
            int count;
            query.Deconstruct(out array, out count);

            int toSkip = 0;
            while (toSkip < count)
            {
                if (predicate(array[toSkip]))
                {
                    toSkip++;
                }
                else
                {
                    break;
                }
            }

            int resultCount = count - toSkip;
            Array.Copy(array, toSkip, array, 0, resultCount);
            Array.Clear(array, resultCount, array.Length - resultCount);

            return new Query<T>(array, resultCount);
        }

        /// <summary>
        /// Returns a new Query with the elements of the original query in sorted order.
        /// </summary>
        public static Query<T> Sort<T>(this Query<T> query) where T : IComparable<T>
        {
            T[] array;
            int count;
            query.Deconstruct(out array, out count);

            Array.Sort(array, 0, count);

            return new Query<T>(array, count);
        }

        /// <summary>
        /// Returns a new Query with the elements of the original query in reverse sorted order.
        /// </summary>
        public static Query<T> SortDescending<T>(this Query<T> query) where T : IComparable<T>
        {
            T[] array;
            int count;
            query.Deconstruct(out array, out count);

            Array.Sort(array, 0, count);
            array.Reverse(0, count);

            return new Query<T>(array, count);
        }

        // Cached storage support for supporting SortValues<T> (for IndexedValue queries).
        public class CachedIndexableComparer<T> : IComparer<T>
        {
            public System.Func<T, T, int> comparison;
            public void SetComparison(System.Func<T, T, int> comparison)
            {
                this.comparison = comparison;
            }
            public int Compare(T x, T y) { return comparison(x, y); }
        }

        /// <summary> WARNING: This function allocates.
        ///
        /// Returns a nested Query where each inner sequence is split from the original query by evaluating the "when" function with sequence prev, next (a, b) pair along the original query.
        ///
        /// For example:
        ///   seqA = (A, B, C, D, E, F, G, H)
        ///   seqA.Query().Split((a, b) => a == C || b == G)
        /// Would result in:
        ///   ((A, B, C), (D, E, F), (G, H))
        ///
        /// If you pass a "keepB" function, for each (a, b) pair along the sequence, "b" values for which keepB(b) returns false will be discarded from the returned subsequence list. This is useful, for example, if you want to split a sequence of valid and invalid values into a list of subsequences of only continuous valid values.
        ///
        /// For example:
        ///   seqA = (A, B, null, null, E, null, G, H)
        ///   seqA.Query().Split((a, b) => (a == null) != (b == null), keepB: b => b != null)
        /// Would result in:
        ///   ((A, B), (E), (G, H))
        ///
        /// TODO: Should be possible to make this method non-allocating by using something like struct Slices all over the original backing Collection instead of allocating new Lists. -Nick 2019-12-03
        /// </summary>
        public static Query<List<T>> Split<T>(this Query<T> query, Func<T, T, bool> when, Func<T, bool> keepB = null)
        {
            using (var slice = query.Deconstruct())
            {
                var outerArr = new List<List<T>>();
                var pendingArr = new List<T>();

                if (slice.Count == 0)
                {
                    outerArr.Add(pendingArr);
                }
                else if (slice.Count == 1)
                {
                    pendingArr.Add(slice[0]);
                    outerArr.Add(pendingArr);
                }
                else
                {
                    // General case.
                    var A = slice[0];
                    pendingArr.Add(A);
                    for (var b = 1; b < slice.Count; b++)
                    {
                        var B = slice[b];

                        if (when(A, B))
                        {
                            // Split.
                            outerArr.Add(pendingArr);
                            pendingArr = new List<T>();
                        }

                        if (keepB == null || keepB(B))
                        {
                            // Add B to pending, make it new A.
                            pendingArr.Add(B);
                            A = B;
                        }
                    }

                    // Don't forget the last sequence. 
                    if (pendingArr.Count > 0)
                    {
                        outerArr.Add(pendingArr);
                    }
                }

                return outerArr.Query();
            }
        }

        /// <summary> As Split, but simpler: Each inner sequence's length is determined by the innerCount argument. Requires an exact split, innerCount must evenly divide the total number of elements in the original Query. </summary>
        public static Query<List<T>> SplitTake<T>(this Query<T> query, int innerCount)
        {
            using (var slice = query.Deconstruct())
            {
                var outerArr = new List<List<T>>();
                var pendingArr = new List<T>();

                if (slice.Count % innerCount != 0) throw new System.ArgumentException($"Query.SplitTake() over {slice.Count} elements must be divisible by innerCount {innerCount}.");

                if (slice.Count == 0)
                {
                    outerArr.Add(pendingArr);
                }
                else if (slice.Count == 1)
                {
                    pendingArr.Add(slice[0]);
                    outerArr.Add(pendingArr);
                }
                else
                {
                    // General case.
                    var A = slice[0];
                    pendingArr.Add(A);
                    for (var b = 1; b < slice.Count; b++)
                    {
                        var B = slice[b];

                        if (pendingArr.Count == innerCount)
                        {
                            // Split.
                            outerArr.Add(pendingArr);
                            pendingArr = new List<T>();
                        }

                        // Add B to pending, make it new A.
                        pendingArr.Add(B);
                        A = B;
                    }

                    // Don't forget the last sequence. 
                    if (pendingArr.Count > 0)
                    {
                        outerArr.Add(pendingArr);
                    }
                }

                return outerArr.Query();
            }
        }

        /// <summary>
        /// Returns a new Query representing only the first few elements of the current sequence.
        /// This method is safe to call even with a count that is larger than the number of elements in
        /// the sequence.
        /// 
        /// For example:
        ///   (A, B, C, D, E, F, G).Query().Take(4)
        /// Would result in:
        ///   (A, B, C, D)
        /// </summary>
        public static Query<T> Take<T>(this Query<T> query, int toTake)
        {
            T[] array;
            int count;
            query.Deconstruct(out array, out count);

            count = Mathf.Min(count, toTake);
            Array.Clear(array, count, array.Length - count);

            return new Query<T>(array, count);
        }

        /// <summary>
        /// Returns a new Query that takes values while the predicate returns true.  As soon
        /// as the predicate returns false, the sequence will return no more elements.  Even if the
        /// predicate becomes true again, the sequence will still halt.
        /// 
        /// For example:
        ///   (1, 3, 9, -1, 5, -4, 9).Query().TakeWhile(isPositive)
        /// Would result in:
        ///   (1, 3, 9)
        /// </summary>
        public static Query<T> TakeWhile<T>(this Query<T> query, Func<T, bool> predicate)
        {
            T[] array;
            int count;
            query.Deconstruct(out array, out count);

            int takeCount;
            for (takeCount = 0; takeCount < count; takeCount++)
            {
                if (!predicate(array[takeCount]))
                {
                    break;
                }
            }

            Array.Clear(array, takeCount, array.Length - takeCount);

            return new Query<T>(array, takeCount);
        }

        /// <summary>
        /// Returns a new Query representing only the elements of the Query for which
        /// the predicate returns true.
        /// 
        /// For example:
        ///   (1, 2, 3, 4, 5, 6, 7).Query().Where(isEven)
        /// Would result in:
        ///   (2, 4, 6)
        /// </summary>
        public static Query<T> Where<T>(this Query<T> query, Func<T, bool> predicate)
        {
            T[] array;
            int count;
            query.Deconstruct(out array, out count);

            int writeIndex = 0;
            for (int i = 0; i < count; i++)
            {
                if (predicate(array[i]))
                {
                    array[writeIndex++] = array[i];
                }
            }

            Array.Clear(array, writeIndex, array.Length - writeIndex);

            return new Query<T>(array, writeIndex);
        }

        /// <summary>
        /// Returns a new Query representing only the elements that are valid Unity Objects.
        /// </summary>
        public static Query<T> ValidUnityObjs<T>(this Query<T> query) where T : UnityEngine.Object
        {
            return query.Where(t =>
            {
                UnityEngine.Object obj = t;
                return obj != null;
            });
        }

        /// <summary>
        /// Returns a new Query representing all of the elements paired with their index that
        /// they are located within the query.  This can be useful if you want to retrieve 
        /// the original index later.
        /// 
        /// For example:
        ///   (A, B, C).Query().WithIndices()
        /// Would result in:
        ///   ((0, A), (1, B), (2, C))
        /// </summary>
        public static Query<IndexedValue<T>> WithIndices<T>(this Query<T> query)
        {
            using (var slice = query.Deconstruct())
            {
                var dstArray = ArrayPool<IndexedValue<T>>.Spawn(slice.Count);

                for (int i = 0; i < slice.Count; i++)
                {
                    dstArray[i] = new IndexedValue<T>()
                    {
                        index = i,
                        value = slice[i]
                    };
                }

                return new Query<IndexedValue<T>>(dstArray, slice.Count);
            }
        }

        /// <summary>
        /// Returns a new Query where each new element in the sequence is an instance of the PrevPair struct.
        /// The value field of the pair will point to an element in the current sequence, and the prev field will
        /// point to an element that comes 'offset' elements before the current element. If 'includeStart' is true, 
        /// the sequence will also include elements that have no previous element.
        /// 
        /// For example, with an offset of 2 and with includeStart as true, the sequence:
        ///   A, B, C, D, E, F
        /// is transformed into:
        ///   (A,_) (B,_) (C,A) (D,B) (E,C) (F,D)
        /// </summary>
        public static Query<PrevPair<T>> WithPrevious<T>(this Query<T> query,
          int offset = 1, bool includeStart = false)
        {
            using (var slice = query.Deconstruct())
            {
                int resultCount = includeStart ? slice.Count : Mathf.Max(0, slice.Count - offset);
                var dstArray = ArrayPool<PrevPair<T>>.Spawn(resultCount);

                int dstIndex = 0;

                if (includeStart)
                {
                    for (int i = 0; i < Mathf.Min(slice.Count, offset); i++)
                    {
                        dstArray[dstIndex++] = new PrevPair<T>()
                        {
                            value = slice[i],
                            prev = default(T),
                            hasPrev = false
                        };
                    }
                }

                for (int i = offset; i < slice.Count; i++)
                {
                    dstArray[dstIndex++] = new PrevPair<T>()
                    {
                        value = slice[i],
                        prev = slice[i - offset],
                        hasPrev = true
                    };
                }

                return new Query<PrevPair<T>>(dstArray, resultCount);
            }
        }

        /// <summary>
        /// Returns a new Query where each new element in the sequence is an instance of the
        /// PrevPair struct. The value field of the pair will point to an element in the
        /// current sequence, and the prev field will point to an element that comes
        /// 'offset' elements before the current element. If 'includeStart' is true, the
        /// sequence will also include elements that have no previous element.
        /// 
        /// For example, with an offset of 2 and with includeEnd as true, the sequence:
        ///   A, B, C, D, E, F
        /// is transformed into:
        ///   (A,C) (B,D) (C,E) (D,F) (E,_) (F,_)
        /// </summary>
        public static Query<NextPair<T>> WithNext<T>(this Query<T> query,
          int offset = 1, bool includeEnd = false)
        {
            offset = Math.Abs(offset);
            using (var slice = query.Deconstruct())
            {
                int resultCount = includeEnd ? slice.Count : Mathf.Max(0, slice.Count - offset);
                var dstArray = ArrayPool<NextPair<T>>.Spawn(resultCount);

                int dstIndex = 0;

                for (int i = 0; i < slice.Count - offset; i++)
                {
                    dstArray[dstIndex++] = new NextPair<T>()
                    {
                        value = slice[i],
                        next = slice[i + offset],
                        hasNext = true
                    };
                }

                if (includeEnd)
                {
                    for (int i = slice.Count - offset; i < slice.Count; i++)
                    {
                        dstArray[dstIndex++] = new NextPair<T>()
                        {
                            value = slice[i],
                            next = default(T),
                            hasNext = false
                        };
                    }
                }

                return new Query<NextPair<T>>(dstArray, resultCount);
            }
        }

        /// <summary>
        /// Returns a new Query that represents the combination of this query sequence with a Collection.
        /// The two sequences are combined element-by-element using a selector function.
        /// The resulting sequence has a length equal to the smaller of the two sequences.
        /// 
        /// For example:
        ///   sequenceA = (A, B, C, D)
        ///   sequenceB = (E, F, G, H)
        ///   sequenceA.Query().Zip(sequenceB, (a, b) => a + b)
        /// Would result in:
        ///   (AE, BF, CG, DH)
        /// </summary>
        public static Query<V> Zip<T, K, V>(this Query<T> query, ICollection<K> collection, Func<T, K, V> selector)
        {
            using (var slice = query.Deconstruct())
            {
                int resultCount = Mathf.Min(slice.Count, collection.Count);
                var resultArray = ArrayPool<V>.Spawn(resultCount);

                var tmpArray = ArrayPool<K>.Spawn(collection.Count);
                collection.CopyTo(tmpArray, 0);

                for (int i = 0; i < resultCount; i++)
                {
                    resultArray[i] = selector(slice[i], tmpArray[i]);
                }

                ArrayPool<K>.Recycle(tmpArray);

                return new Query<V>(resultArray, resultCount);
            }
        }

        /// <summary>
        /// Returns a new Query that represents the combination of this query sequence with another Query.
        /// The two sequences are combined element-by-element using a selector function.
        /// The resulting sequence has a length equal to the smaller of the two sequences.
        /// 
        /// For example:
        ///   sequenceA = (A, B, C, D)
        ///   sequenceB = (E, F, G, H)
        ///   sequenceA.Query().Zip(sequenceB.Query(), (a, b) => a + b)
        /// Would result in:
        ///   (AE, BF, CG, DH)
        /// </summary>
        public static Query<V> Zip<T, K, V>(this Query<T> query, Query<K> otherQuery, Func<T, K, V> selector)
        {
            using (var slice = query.Deconstruct())
            using (var otherSlice = otherQuery.Deconstruct())
            {
                int resultCount = Mathf.Min(slice.Count, otherSlice.Count);
                var resultArray = ArrayPool<V>.Spawn(resultCount);

                for (int i = 0; i < resultCount; i++)
                {
                    resultArray[i] = selector(slice[i], otherSlice[i]);
                }

                return new Query<V>(resultArray, resultCount);
            }
        }

        public struct PrevPair<T>
        {
            /// <summary>
            /// The current element of the sequence
            /// </summary>
            public T value;

            /// <summary>
            /// If hasPrev is true, the element that came before value
            /// </summary>
            public T prev;

            /// <summary>
            /// Does the prev field represent a previous value?  If false,
            /// prev will take the default value of T.
            /// </summary>
            public bool hasPrev;
        }

        public struct NextPair<T>
        {
            /// <summary>
            /// The current element of the sequence
            /// </summary>
            public T value;

            /// <summary>
            /// If hasNext is true, the element that comes after the value.
            /// </summary>
            public T next;

            /// <summary>
            /// Does the next field represent the next value?  If false,
            /// prev will take the default value of T.
            /// </summary>
            public bool hasNext;
        }

        public struct IndexedValue<T>
        {
            public int index;
            public T value;

            public IndexedValue(int index, T value) { this.index = index; this.value = value; }

            public void Deconstruct(out int index, out T value)
            {
                index = this.index;
                value = this.value;
            }
        }

        private class FunctorComparer<T, K> : IComparer<T> where K : IComparable<K>
        {
            [ThreadStatic]
            private static FunctorComparer<T, K> _single;

            private Func<T, K> _functor;
            private int _sign;

            private FunctorComparer() { }

            public static FunctorComparer<T, K> Ascending(Func<T, K> functor)
            {
                return single(functor, 1);
            }

            public static FunctorComparer<T, K> Descending(Func<T, K> functor)
            {
                return single(functor, -1);
            }

            private static FunctorComparer<T, K> single(Func<T, K> functor, int sign)
            {
                if (_single == null)
                {
                    _single = new FunctorComparer<T, K>();
                }
                _single._functor = functor;
                _single._sign = sign;
                return _single;
            }

            public void Clear()
            {
                _functor = null;
            }

            public int Compare(T x, T y)
            {
                return _sign * _functor(x).CompareTo(_functor(y));
            }
        }
    }
}
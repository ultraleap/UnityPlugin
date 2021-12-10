/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity
{

    /// <summary>
    /// A simple wrapper around List to provide readonly access.
    /// Useful when you want to return a list to someone but you want
    /// to make sure they don't muck it up!
    /// </summary>
    public struct ReadonlyList<T>
    {
        private readonly List<T> _list;

        public ReadonlyList(List<T> list)
        {
            _list = list;
        }

        public bool isValid
        {
            get
            {
                return _list != null;
            }
        }

        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        public T this[int index]
        {
            get
            {
                return _list[index];
            }
        }

        public List<T>.Enumerator GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public static implicit operator ReadonlyList<T>(List<T> list)
        {
            return new ReadonlyList<T>(list);
        }

        public int IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        /// <summary> Converts the ReadonlyList{T} reference back to a List{T}. Useful to use extension methods that accept Lists but not ReadonlyLists, but be careful, this makes the underlying data writeable again and could go against the grain of another author's API. Intended to allow calling Query() ops on APIs that expose ReadonlyLists instead of Lists. </summary>
        public List<T> asList { get { return _list; } }
    }
}
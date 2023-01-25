/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Unity
{
    public abstract class SerializableHashSetBase { }

    public interface ICanReportDuplicateInformation
    {
#if UNITY_EDITOR
        List<int> GetDuplicationInformation();
        void ClearDuplicates();
#endif
    }

    public class SerializableHashSet<T> : SerializableHashSetBase,
                                          ICanReportDuplicateInformation,
                                          ISerializationCallbackReceiver,
                                          IEnumerable<T>
    {

        [SerializeField]
        private List<T> _values = new List<T>();

        [NonSerialized]
        private HashSet<T> _set = new HashSet<T>();

        #region HASH SET API

        public int Count
        {
            get { return _set.Count; }
        }

        public bool Add(T item)
        {
            return _set.Add(item);
        }

        public void Clear()
        {
            _set.Clear();
        }

        public bool Contains(T item)
        {
            return _set.Contains(item);
        }

        public bool Remove(T item)
        {
            return _set.Remove(item);
        }

        public static implicit operator HashSet<T>(SerializableHashSet<T> serializableHashSet)
        {
            return serializableHashSet._set;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        #endregion

        public void ClearDuplicates()
        {
            HashSet<T> takenValues = new HashSet<T>();
            for (int i = _values.Count; i-- != 0;)
            {
                var value = _values[i];
                if (takenValues.Contains(value))
                {
                    _values.RemoveAt(i);
                }
                else
                {
                    takenValues.Add(value);
                }
            }
        }

        public List<int> GetDuplicationInformation()
        {
            Dictionary<T, int> info = new Dictionary<T, int>();

            foreach (var value in _values)
            {
                if (value == null)
                {
                    continue;
                }

                if (info.ContainsKey(value))
                {
                    info[value]++;
                }
                else
                {
                    info[value] = 1;
                }
            }

            List<int> dups = new List<int>();
            foreach (var value in _values)
            {
                if (value == null)
                {
                    continue;
                }

                dups.Add(info[value]);
            }

            return dups;
        }

        public void OnAfterDeserialize()
        {
            _set.Clear();

            if (_values != null)
            {
                foreach (var value in _values)
                {
                    if (value != null)
                    {
                        _set.Add(value);
                    }
                }
            }

#if !UNITY_EDITOR
            _values.Clear();
#endif
        }

        public void OnBeforeSerialize()
        {
            if (_values == null)
            {
                _values = new List<T>();
            }

#if UNITY_EDITOR
            //Delete any values not present
            for (int i = _values.Count; i-- != 0;)
            {
                T value = _values[i];
                if (value == null)
                {
                    continue;
                }

                if (!_set.Contains(value))
                {
                    _values.RemoveAt(i);
                }
            }

            //Add any values not accounted for
            foreach (var value in _set)
            {
                if (isNull(value))
                {
                    if (!_values.Any(obj => isNull(obj)))
                    {
                        _values.Add(value);
                    }
                }
                else
                {
                    if (!_values.Contains(value))
                    {
                        _values.Add(value);
                    }
                }
            }
#else
            //At runtime we just recreate the list
            _values.Clear();
            _values.AddRange(this);
#endif
        }

        private bool isNull(object obj)
        {
            if (obj == null)
            {
                return true;
            }

            if (obj is UnityEngine.Object)
            {
                return (obj as UnityEngine.Object) == null;
            }

            return false;
        }
    }
}
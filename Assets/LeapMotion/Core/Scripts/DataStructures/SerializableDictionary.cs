/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace Leap.Unity {

  [Obsolete("It is no longer required to annotate SerializableDictionary with an SDictionary attribute")]
  public class SDictionaryAttribute : PropertyAttribute { }

  public abstract class SerializableDictionaryBase { }

  public interface ICanReportDuplicateInformation {
#if UNITY_EDITOR
    List<int> GetDuplicationInformation();
    void ClearDuplicates();
#endif
  }

  public interface ISerializableDictionary {
    float KeyDisplayRatio();
  }

  /// <summary>
  /// In order to have this class be serialized, you will always need to create your own
  /// non-generic version specific to your needs.  This is the same workflow that exists
  /// for using the UnityEvent class as well. 
  /// </summary>
  public class SerializableDictionary<TKey, TValue> : SerializableDictionaryBase,
                                                      IEnumerable<KeyValuePair<TKey, TValue>>,
                                                      ICanReportDuplicateInformation,
                                                      ISerializationCallbackReceiver,
                                                      ISerializableDictionary {

    [SerializeField]
    private List<TKey> _keys = new List<TKey>();

    [SerializeField]
    private List<TValue> _values = new List<TValue>();

    [NonSerialized]
    private Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

    #region DICTIONARY API

    public TValue this[TKey key] {
      get { return _dictionary[key]; }
      set { _dictionary[key] = value; }
    }

    public Dictionary<TKey, TValue>.KeyCollection Keys {
      get { return _dictionary.Keys; }
    }

    public Dictionary<TKey, TValue>.ValueCollection Values {
      get { return _dictionary.Values; }
    }

    public int Count {
      get { return _dictionary.Count; }
    }

    public void Add(TKey key, TValue value) {
      _dictionary.Add(key, value);
    }

    public void Clear() {
      _dictionary.Clear();
    }

    public bool ContainsKey(TKey key) {
      return _dictionary.ContainsKey(key);
    }

    public bool ContainsValue(TValue value) {
      return _dictionary.ContainsValue(value);
    }

    public bool Remove(TKey key) {
      return _dictionary.Remove(key);
    }

    public bool TryGetValue(TKey key, out TValue value) {
      return _dictionary.TryGetValue(key, out value);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
      return _dictionary.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return _dictionary.GetEnumerator();
    }

    public static implicit operator Dictionary<TKey, TValue>(SerializableDictionary<TKey, TValue> serializableDictionary) {
      return serializableDictionary._dictionary;
    }

    #endregion

    /// <summary>
    /// Returns how much of the display space should be allocated to the key.
    /// Should be a value in the range 0-1.
    /// </summary>
    public virtual float KeyDisplayRatio() {
      return 0.5f;
    }

    public override string ToString() {
      StringBuilder toReturn = new StringBuilder();
      List<TKey> keys = _dictionary.Keys.ToList<TKey>();
      List<TValue> values = _dictionary.Values.ToList<TValue>();
      toReturn.Append("[");
      for (int i = 0; i < keys.Count; i++) {
        toReturn.Append("{");
        toReturn.Append(keys[i].ToString());
        toReturn.Append(" : ");
        toReturn.Append(values[i].ToString());
        toReturn.Append("}, \n");
      }
      toReturn.Remove(toReturn.Length - 3, 3);
      toReturn.Append("]");
      return toReturn.ToString();
    }

    public void OnAfterDeserialize() {
      _dictionary.Clear();

      if (_keys != null && _values != null) {
        int count = Mathf.Min(_keys.Count, _values.Count);
        for (int i = 0; i < count; i++) {
          TKey key = _keys[i];
          TValue value = _values[i];

          if (key == null) {
            continue;
          }

          _dictionary[key] = value;
        }
      }

#if !UNITY_EDITOR
      _keys.Clear();
      _values.Clear();
#endif
    }

#if UNITY_EDITOR
    public List<int> GetDuplicationInformation() {
      Dictionary<TKey, int> info = new Dictionary<TKey, int>();

      for (int i = 0; i < _keys.Count; i++) {
        TKey key = _keys[i];
        if (key == null) {
          continue;
        }

        if (info.ContainsKey(key)) {
          info[key]++;
        } else {
          info[key] = 1;
        }
      }

      List<int> dups = new List<int>();
      for (int i = 0; i < _keys.Count; i++) {
        TKey key = _keys[i];
        if (key == null) {
          continue;
        }

        dups.Add(info[key]);
      }

      return dups;
    }

    public void ClearDuplicates() {
      HashSet<TKey> takenKeys = new HashSet<TKey>();
      for (int i = 0; i < _keys.Count; i++) {
        TKey key = _keys[i];
        if (takenKeys.Contains(key)) {
          _keys.RemoveAt(i);
          _values.RemoveAt(i);
          i--;
        } else {
          takenKeys.Add(key);
        }
      }
    }
#endif

    public void OnBeforeSerialize() {
      if (_keys == null) {
        _keys = new List<TKey>();
      }

      if (_values == null) {
        _values = new List<TValue>();
      }

#if UNITY_EDITOR
      for (int i = _keys.Count; i-- != 0;) {
        TKey key = _keys[i];
        if (key == null) continue;

        if (!_dictionary.ContainsKey(key)) {
          _keys.RemoveAt(i);
          _values.RemoveAt(i);
        }
      }
#endif

      Dictionary<TKey, TValue>.Enumerator enumerator = _dictionary.GetEnumerator();
      while (enumerator.MoveNext()) {
        var pair = enumerator.Current;

#if UNITY_EDITOR
        if (!_keys.Contains(pair.Key)) {
          _keys.Add(pair.Key);
          _values.Add(pair.Value);
        }
#else
        _keys.Add(pair.Key);
        _values.Add(pair.Value);
#endif
      }
    }
  }
}

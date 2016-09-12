using UnityEngine;
using System.Collections.Generic;

namespace Leap.Unity {

  /// <summary>
  /// You must mark a serializable dictionary with this attribute in order to 
  /// use the custom inspector editor.
  /// </summary>
  public class SDictionaryAttribute : PropertyAttribute { }

  public interface ICanReportDuplicateInformation {
    List<int> GetDuplicationInformation();
    void ClearDuplicates();
  }

  /// <summary>
  /// In order to have this class be serialized, you will always need to create your own
  /// non-generic version specific to your needs.  This is the same workflow that exists
  /// for using the UnityEvent class as well. 
  /// </summary>
  public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ICanReportDuplicateInformation, ISerializationCallbackReceiver {
    
    [SerializeField]
    private List<TKey> _keys;
    
    [SerializeField]
    private List<TValue> _values;

    public void OnAfterDeserialize() {
      Clear();

      if (_keys != null && _values != null) {
        int count = Mathf.Min(_keys.Count, _values.Count);
        for (int i = 0; i < count; i++) {
          this[_keys[i]] = _values[i];
        }
      }
    }

    public List<int> GetDuplicationInformation() {
      Dictionary<TKey, int> info = new Dictionary<TKey, int>();

      for (int i = 0; i < _keys.Count; i++) {
        TKey key = _keys[i];
        if (info.ContainsKey(key)) {
          info[key]++;
        } else {
          info[key] = 1;
        }
      }

      List<int> dups = new List<int>();
      for (int i = 0; i < _keys.Count; i++) {
        dups.Add(info[_keys[i]]);
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

    public void OnBeforeSerialize() {
      if (_keys == null) {
        _keys = new List<TKey>();
      }

      if (_values == null) {
        _values = new List<TValue>();
      }

      for (int i = _keys.Count; i-- != 0;) {
        if (!ContainsKey(_keys[i])) {
          _keys.RemoveAt(i);
          _values.RemoveAt(i);
        }
      }

      Enumerator enumerator = GetEnumerator();
      while (enumerator.MoveNext()) {
        var pair = enumerator.Current;

        if (!_keys.Contains(pair.Key)) {
          _keys.Add(pair.Key);
          _values.Add(pair.Value);
        }
      }
    }
  }
}

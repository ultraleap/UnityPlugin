using UnityEngine;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace Leap.Unity {

  /// <summary>
  /// You must mark a serializable dictionary with this attribute in order to 
  /// use the custom inspector editor.
  /// </summary>
  public class SDictionaryAttribute : PropertyAttribute { }

  public interface ICanReportDuplicateInformation {
#if UNITY_EDITOR
    List<int> GetDuplicationInformation();
    void ClearDuplicates();
#endif
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
    
    public override string ToString() {
      StringBuilder toReturn = new StringBuilder();
      List<TKey> keys = Keys.ToList<TKey>();
      List<TValue> values = Values.ToList<TValue>();
      toReturn.Append("[");
      for (int i = 0; i < keys.Count; i++) {
        toReturn.Append("{");
        toReturn.Append(keys[i].ToString());
        toReturn.Append(" : ");
        toReturn.Append(values[i].ToString());
        toReturn.Append("}, \n");
      }
      toReturn.Remove(toReturn.Length-3, 3);
      toReturn.Append("]");
      return toReturn.ToString();
    }

    public void OnAfterDeserialize() {
      Clear();

      if (_keys != null && _values != null) {
        int count = Mathf.Min(_keys.Count, _values.Count);
        for (int i = 0; i < count; i++) {
          TKey key = _keys[i];
          TValue value = _values[i];

          if (key == null) {
            continue;
          }

          this[key] = value;
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

        if (!ContainsKey(key)) {
          _keys.RemoveAt(i);
          _values.RemoveAt(i);
        }
      }
#endif

      Enumerator enumerator = GetEnumerator();
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

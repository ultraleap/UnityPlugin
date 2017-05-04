using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity {

  /// <summary>
  /// You must mark a serializable hash set field with this
  /// attribute in order to use the custom inspector editor
  /// </summary>
  public class SHashSetAttribute : PropertyAttribute { }

  public class SerializableHashSet<T> : HashSet<T>,
    ICanReportDuplicateInformation,
    ISerializationCallbackReceiver {

    [SerializeField]
    private List<T> _values;

    public void ClearDuplicates() {
      HashSet<T> takenValues = new HashSet<T>();
      for (int i = _values.Count; i-- != 0;) {
        var value = _values[i];
        if (takenValues.Contains(value)) {
          _values.RemoveAt(i);
        } else {
          takenValues.Add(value);
        }
      }
    }

    public List<int> GetDuplicationInformation() {
      Dictionary<T, int> info = new Dictionary<T, int>();

      foreach (var value in _values) {
        if (value == null) {
          continue;
        }

        if (info.ContainsKey(value)) {
          info[value]++;
        } else {
          info[value] = 1;
        }
      }

      List<int> dups = new List<int>();
      foreach (var value in _values) {
        if (value == null) {
          continue;
        }

        dups.Add(info[value]);
      }

      return dups;
    }

    public void OnAfterDeserialize() {
      Clear();

      if (_values != null) {
        foreach (var value in _values) {
          if (value != null) {
            Add(value);
          }
        }
      }

#if !UNITY_EDITOR
      _values.Clear();
#endif
    }

    public void OnBeforeSerialize() {
      if (_values == null) {
        _values = new List<T>();
      }

#if UNITY_EDITOR
      //Delete any values not present
      for (int i = _values.Count; i-- != 0;) {
        T value = _values[i];
        if (value == null) {
          continue;
        }

        if (!Contains(value)) {
          _values.RemoveAt(i);
        }
      }

      //Add any values not accounted for
      foreach (var value in this) {
        if (!_values.Contains(value)) {
          _values.Add(value);
        }
      }
#else
      //At runtime we just recreate the list
      _values.Clear();
      _values.AddRange(this);
#endif
    }
  }
}

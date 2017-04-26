using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity {

  [Serializable]
  public class EnumEventTable : ISerializationCallbackReceiver {

    [Serializable]
    private class Entry {

      [SerializeField]
      public int enumValue;

      [SerializeField]
      public UnityEvent callback;
    }

    //The actual serialized list of entries
    [SerializeField]
    private List<Entry> _entries = new List<Entry>();

    //Not serialized, is just populated after deserialization and used
    //when calling Invoke for speed.
    private Dictionary<int, UnityEvent> _entryMap = new Dictionary<int, UnityEvent>();

    public void Invoke(int enumValue) {
      UnityEvent callback;
      if (_entryMap.TryGetValue(enumValue, out callback)) {
        callback.Invoke();
      }
    }

    public void OnBeforeSerialize() { }

    public void OnAfterDeserialize() {
      if (_entryMap == null) {
        _entryMap = new Dictionary<int, UnityEvent>();
      } else {
        _entryMap.Clear();
      }

      foreach (var entry in _entries) {
        _entryMap[entry.enumValue] = entry.callback;
      }
    }
  }
}

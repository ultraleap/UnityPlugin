/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity {

  [Serializable]
  public class EnumEventTable : ISerializationCallbackReceiver {

    [Serializable]
    private class Entry {

      #pragma warning disable 0649
      [SerializeField]
      public int enumValue;

      [SerializeField]
      public UnityEvent callback;
      #pragma warning restore 0649
    }

    //The actual serialized list of entries
    [SerializeField]
    private List<Entry> _entries = new List<Entry>();

    //Not serialized, is just populated after deserialization and used
    //when calling Invoke for speed.
    private Dictionary<int, UnityEvent> _entryMap = new Dictionary<int, UnityEvent>();

    public bool HasUnityEvent(int enumValue) {
      UnityEvent callback;
      if (_entryMap.TryGetValue(enumValue, out callback)) {
        return callback.GetPersistentEventCount() > 0;
      }
      else {
        return false;
      }
    }

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

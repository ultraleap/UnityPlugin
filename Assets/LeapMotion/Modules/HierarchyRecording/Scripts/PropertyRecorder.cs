using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity;
using Leap.Unity.Query;

[DisallowMultipleComponent]
public class PropertyRecorder : MonoBehaviour {

  [Serializable]
  public class BindingSet : SerializableHashSet<string> { }

  [SerializeField]
  private List<string> _bindings = new List<string>();

  [SerializeField]
  private List<string> _expandedTypes = new List<string>();

  [NonSerialized]
  private List<EditorCurveBinding> _cachedBindings;
  public List<EditorCurveBinding> GetBindings(GameObject root) {
    if (_cachedBindings == null) {
      _cachedBindings =
      AnimationUtility.GetAnimatableBindings(gameObject, root).Query().
                                                               Where(IsBindingEnabled).
                                                               ToList();
    }

    return _cachedBindings;
  }

  public bool IsBindingEnabled(EditorCurveBinding binding) {
    return _bindings.Contains(getKey(binding));
  }

  public void SetBindingEnabled(EditorCurveBinding binding, bool enabled) {
    var key = getKey(binding);
    if (enabled == IsBindingEnabled(binding)) {
      return;
    }

    if (enabled) {
      _bindings.Add(key);
    } else {
      _bindings.Remove(key);
    }
  }

  public bool IsBindingExpanded(EditorCurveBinding binding) {
    return _expandedTypes.Contains(binding.type.Name);
  }

  public void SetBindingExpanded(EditorCurveBinding binding, bool expanded) {
    if (expanded == IsBindingExpanded(binding)) {
      return;
    }

    if (expanded) {
      _expandedTypes.Add(binding.type.Name);
    } else {
      _expandedTypes.Remove(binding.type.Name);
    }
  }

  private string getKey(EditorCurveBinding binding) {
    return binding.type.Name + " : " + binding.propertyName;
  }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity;

[DisallowMultipleComponent]
public class PropertyRecorder : MonoBehaviour {

  [Serializable]
  public class BindingSet : SerializableHashSet<string> { }

  [SerializeField]
  public BindingSet bindings = new BindingSet();

  [SerializeField]
  public BindingSet expandedTypes = new BindingSet();

  public bool IsBindingEnabled(EditorCurveBinding binding) {
    return bindings.Contains(getKey(binding));
  }

  public void SetBindingEnabled(EditorCurveBinding binding, bool enabled) {
    var key = getKey(binding);
    if (enabled) {
      bindings.Add(key);
    } else {
      bindings.Remove(key);
    }
  }

  public bool IsTypeExpanded(string typeName) {
    return expandedTypes.Contains(typeName);
  }

  public void SetTypeExpanded(string typeName, bool expanded) {
    if (expanded) {
      expandedTypes.Add(typeName);
    } else {
      expandedTypes.Remove(typeName);
    }
  }

  private string getKey(EditorCurveBinding binding) {
    return binding.type.Name + " : " + binding.propertyName;
  }


}

/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Query;

namespace Leap.Unity.Recording {

  [DisallowMultipleComponent]
  public class PropertyRecorder : MonoBehaviour {

    [Serializable]
    public class BindingSet : SerializableHashSet<string> { }

    [SerializeField]
    protected List<string> _bindings = new List<string>();

    [SerializeField]
    protected List<string> _expandedTypes = new List<string>();

#if UNITY_EDITOR
    [NonSerialized]
    protected List<EditorCurveBinding> _cachedBindings;
    public List<EditorCurveBinding> GetBindings(GameObject root) {
      if (_cachedBindings == null) {
        _cachedBindings =
        AnimationUtility.GetAnimatableBindings(gameObject, root).Query().
                                                                 Where(IsBindingEnabled).
                                                                 Where(b => b.type != typeof(Transform) &&
                                                                            b.type != typeof(GameObject)).
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
#endif
  }
}

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class LeapGuiFeatureBase : ScriptableObject {
  private bool _isDirty = true; //everything defaults dirty at the start!

  public bool isDirty {
    get {
#if UNITY_EDITOR
      if (Application.isPlaying) {
        return _isDirty;
      } else {
        return true;
      }
#else
      return _isDirty;
#endif
    }
    set {
      _isDirty = value;
    }
  }

  //TODO: add logic in LeapGUI to use this method
  public virtual FeatureSupportInfo GetSupportInfo(LeapGui gui) {
    return FeatureSupportInfo.FullSupport();
  }

  public abstract void ClearDataObjectReferences();
  public abstract void AddDataObjectReference(LeapGuiElementData data);

  public abstract LeapGuiElementData CreateDataObject(LeapGuiElement element);

#if UNITY_EDITOR
  public abstract void DrawFeatureEditor(Rect rect, bool isActive, bool isFocused);
  public abstract float GetEditorHeight();
#endif
}

public abstract class LeapGuiElementData : ScriptableObject {
  [HideInInspector]
  public LeapGuiElement element;
  [HideInInspector]
  public LeapGuiFeatureBase feature;
}

public abstract class LeapGuiFeature<DataType> : LeapGuiFeatureBase
  where DataType : LeapGuiElementData {

  /// <summary>
  /// A list of all element data object.
  /// </summary>
  [HideInInspector]
  public List<DataType> data = new List<DataType>();

  public override void ClearDataObjectReferences() {
    data.Clear();
  }

  public override void AddDataObjectReference(LeapGuiElementData data) {
    this.data.Add(data as DataType);
  }

  public override LeapGuiElementData CreateDataObject(LeapGuiElement element) {
    var dataObj = ScriptableObject.CreateInstance<DataType>();
    dataObj.element = element;
    dataObj.feature = this;
    return dataObj;
  }
}


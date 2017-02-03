using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class LeapGuiFeatureBase : HidableMonobehaviour {
  private bool _isDirty = true; //everything defaults dirty at the start!

  public bool isDirty
  {
    get
    {
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
    set
    {
      _isDirty = value;
    }
  }

  //TODO: add logic in LeapGUI to use this method
  public virtual FeatureSupportInfo GetSupportInfo(LeapGui gui) {
    return FeatureSupportInfo.FullSupport();
  }

  public abstract void ClearDataObjectReferences();
  public abstract void AddDataObjectReference(LeapGuiElementData data);

  public abstract Type GetDataObjectType();
  public abstract LeapGuiElementData CreateDataObject(LeapGuiElement element);

#if UNITY_EDITOR
  public abstract void DrawFeatureEditor(Rect rect, bool isActive, bool isFocused);
  public abstract float GetEditorHeight();
#endif
}

public abstract class LeapGuiElementData : HidableMonobehaviour {
  [HideInInspector]
  public LeapGuiElement element;
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

  public override Type GetDataObjectType() {
    return typeof(DataType);
  }

  public override LeapGuiElementData CreateDataObject(LeapGuiElement element) {
    var dataObj = element.gameObject.AddComponent<DataType>();
    dataObj.element = element;
    return dataObj;
  }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class LeapGuiFeatureBase : ScriptableObject {
  public bool isDirty;

  public abstract void ClearDataObjectReferences();
  public abstract void AddDataObjectReference(LeapGuiElementData data);

  public abstract LeapGuiElementData CreateDataObject(LeapGuiElement element);
}

public abstract class LeapGuiElementData : ScriptableObject {
  public LeapGuiElement element;
  public LeapGuiFeatureBase feature;
}

public abstract class LeapGuiFeature<DataType> : LeapGuiFeatureBase
  where DataType : LeapGuiElementData {

  /// <summary>
  /// A list of all element data object.
  /// </summary>
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


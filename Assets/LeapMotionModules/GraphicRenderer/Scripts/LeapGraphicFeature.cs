using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  public abstract class LeapGraphicFeatureBase : LeapGraphicComponentBase<LeapGraphicRenderer> {
    private bool _isDirty = true; //everything defaults dirty at the start!

    public bool isDirty {
      get {
        return _isDirty;
      }
      set {
        _isDirty = value;
      }
    }

    public virtual SupportInfo GetSupportInfo(LeapGraphicGroup group) {
      return SupportInfo.FullSupport();
    }

    public abstract void AssignFeatureReferences();
    public abstract void ClearDataObjectReferences();
    public abstract void AddFeatureData(LeapFeatureData data);

    public abstract Type GetDataObjectType();
    public abstract LeapFeatureData CreateFeatureDataForGraphic(LeapGraphic graphic);

#if UNITY_EDITOR
    public abstract void DrawFeatureEditor(Rect rect, bool isActive, bool isFocused);
    public abstract float GetEditorHeight();
#endif
  }

  public abstract class LeapGraphicFeature<DataType> : LeapGraphicFeatureBase
    where DataType : LeapFeatureData {

    /// <summary>
    /// A list of all feature data.
    /// </summary>
    [HideInInspector]
    public List<DataType> featureData = new List<DataType>();

    public override void AssignFeatureReferences() {
      foreach (var dataObj in featureData) {
        dataObj.feature = this;
      }
    }

    public override void ClearDataObjectReferences() {
      featureData.Clear();
    }

    public override void AddFeatureData(LeapFeatureData data) {
      this.featureData.Add(data as DataType);
    }

    public override Type GetDataObjectType() {
      return typeof(DataType);
    }

    public override LeapFeatureData CreateFeatureDataForGraphic(LeapGraphic graphic) {
      var dataObj = graphic.gameObject.AddComponent<DataType>();
      dataObj.graphic = graphic;
      return dataObj;
    }
  }

  [ExecuteInEditMode]
  public abstract class LeapFeatureData : LeapGraphicComponentBase<LeapGraphic> {
    [HideInInspector]
    public LeapGraphic graphic;

    [HideInInspector]
    public LeapGraphicFeatureBase feature;

    protected override void OnValidate() {
      base.OnValidate();

      //Feature is not serialized, so could totally be null in the editor right as
      //the game starts.  Not an issue at runtime because OnValidate is not called
      //at runtime.
      if (feature != null) {
        feature.isDirty = true;
      }
    }

    public void MarkFeatureDirty() {
      if (feature != null) {
        feature.isDirty = true;
      }
    }

#if UNITY_EDITOR
    public static Type GetFeatureType(Type dataObjType) {
      var allTypes = Assembly.GetAssembly(dataObjType).GetTypes();
      return allTypes.Query().
                      Where(t => t.IsSubclassOf(typeof(LeapGraphicFeatureBase)) &&
                                 t != typeof(LeapGraphicFeatureBase) &&
                                !t.IsAbstract &&
                                 t.BaseType.GetGenericArguments()[0] == dataObjType).
                      FirstOrDefault();
    }
#endif
  }
}

/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  public abstract class LeapGraphicFeatureBase {

    [NonSerialized]
    private bool _isDirty = true; //everything defaults dirty at the start!

    //Unity cannot serialize lists of objects that have no serializable fields
    //when it is set to text-serialization.  A feature might have no specific
    //data, so we add this dummy bool to ensure it gets serialized anyway
    [SerializeField]
    private bool _dummyBool;

    public bool isDirty {
      get {
        return _isDirty;
      }
      set {
        _isDirty = value;
      }
    }

    public bool isDirtyOrEditTime {
      get {
#if UNITY_EDITOR
        if (!Application.isPlaying) {
          return true;
        } else
#endif
        {
          return _isDirty;
        }
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
  }

  public abstract class LeapGraphicFeature<DataType> : LeapGraphicFeatureBase
    where DataType : LeapFeatureData, new() {

    /// <summary>
    /// A list of all feature data.
    /// </summary>
    [NonSerialized]
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
      DataType data = new DataType();
      data.graphic = graphic;
      return data;
    }
  }

  [Serializable]
  public abstract class LeapFeatureData {
    [NonSerialized]
    public LeapGraphic graphic;

    [NonSerialized]
    public LeapGraphicFeatureBase feature;

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

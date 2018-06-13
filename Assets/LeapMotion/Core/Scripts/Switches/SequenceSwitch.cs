using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Leap.Unity.Animation {

  public class SequenceSwitch : TweenSwitch {

    #region Inspector

    [Header("Indexable Sequence (elements require IPropertySwitch)")]
    
    [ImplementsInterface("getSequenceProviderInterfaceType")]
    public MonoBehaviour sequenceProvider;
    private Type getSequenceProviderInterfaceType() {
      return typeof(IIndexable<GameObject>);
    }
    private IIndexable<GameObject> _gameObjects {
      get { return sequenceProvider as IIndexable<GameObject>; }
    }

    [Header("Sequence Control")]

    [Tooltip("If this is checked, elements in the sequence will activate "
           + "starting from the last index rather than the first index.")]
    public bool reverseOrder = false;

    #endregion

    #region Switch Implementation

    protected override void updateSwitch(float time, bool immediately = false) {
      int totalNumObjects = _gameObjects.Count;
      int numOnObjects = (int)(time * totalNumObjects);

      int objIdx = reverseOrder ? totalNumObjects - 1 : 0;
      for (int i = 0; i < totalNumObjects; i++) {
        var propertySwitch = _gameObjects[objIdx].GetComponent<IPropertySwitch>();

        // Each element needs a component that implements IPropertySwitch for the
        // sequence switch to function properly.
        if (propertySwitch == null) {
          Debug.LogError("Unable to switch " + _gameObjects[objIdx].name + ";"
            + "it must have a component that implements IPropertySwitch.",
            _gameObjects[objIdx]);
          continue;
        }

        if (i < numOnObjects) {
          if (immediately) {
            propertySwitch.OnNow();
          }
          else {
            propertySwitch.On();
          }
        }
        else {
          if (immediately) {
            propertySwitch.OffNow();
          }
          else {
            propertySwitch.Off();
          }
        }

        objIdx += reverseOrder ? -1 : 1;
      }
    }

    #endregion

  }

}

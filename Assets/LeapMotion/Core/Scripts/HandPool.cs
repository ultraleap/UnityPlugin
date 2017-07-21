/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Assertions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap;

namespace Leap.Unity {
  /**
   * HandPool holds a pool of IHandModels and makes HandRepresentations
   * when given a Leap Hand and a model type of graphics or physics.
   * When a HandRepresentation is created, an IHandModel is removed from the pool.
   * When a HandRepresentation is finished, its IHandModel is returned to the pool.
   */
  public class HandPool : MonoBehaviour {
    [SerializeField]
    [Tooltip("Reference for the transform that is a child of the camera rig's root and is a parent to all hand models.")]
    [FormerlySerializedAs("ModelsParent")]
    private Transform _modelsParent;
    /// <summary>
    /// Gets the parent transform of models available to the HandPool.
    /// </summary>
    public Transform modelsParent {
      get { return _modelsParent; }
    }

    [SerializeField]
    private List<ModelGroup> ModelPool;
    private List<HandRepresentation> activeHandReps = new List<HandRepresentation>();

    private Dictionary<IHandModel, ModelGroup> modelGroupMapping = new Dictionary<IHandModel, ModelGroup>();
    private Dictionary<IHandModel, HandRepresentation> modelToHandRepMapping = new Dictionary<IHandModel, HandRepresentation>();
    /**
     * ModelGroup contains a left/right pair of IHandModel's
     * @param modelList The IHandModels available for use by HandRepresentations
     * @param modelsCheckedOut The IHandModels currently in use by active HandRepresentations
     * @param IsEnabled determines whether the ModelGroup is active at app Start(), though ModelGroup's are controlled with the EnableGroup() & DisableGroup methods.
     * @param CanDuplicate Allows a IHandModels in the ModelGroup to be cloned at runtime if a suitable IHandModel isn't available.
     */
    [System.Serializable]
    public class ModelGroup {
      public string GroupName;
      [HideInInspector]
      public HandPool _handPool;

      public IHandModel LeftModel;
      [HideInInspector]
      public bool IsLeftToBeSpawned;
      public IHandModel RightModel;
      [HideInInspector]
      public bool IsRightToBeSpawned;
      [HideInInspector]
      public List<IHandModel> modelList = new List<IHandModel>();
      [HideInInspector]
      public List<IHandModel> modelsCheckedOut = new List<IHandModel>();
      public bool IsEnabled = true;
      public bool CanDuplicate;

      public Hands.HandEvent HandPostProcesses;

      /*Looks for suitable IHandModel is the ModelGroup's modelList, if found, it is added to modelsCheckedOut.
       * If not, one can be cloned*/
      public IHandModel TryGetModel(Chirality chirality, ModelType modelType) {
        for (int i = 0; i < modelList.Count; i++) {
          if (modelList[i].HandModelType == modelType && modelList[i].Handedness == chirality) {
            IHandModel model = modelList[i];
            modelList.RemoveAt(i);
            modelsCheckedOut.Add(model);
            return model;
          }
        }
        if (CanDuplicate) {
          for (int i = 0; i < modelsCheckedOut.Count; i++) {
            if (modelsCheckedOut[i].HandModelType == modelType && modelsCheckedOut[i].Handedness == chirality) {
              IHandModel modelToSpawn = modelsCheckedOut[i];
              IHandModel spawnedModel = GameObject.Instantiate(modelToSpawn);
              spawnedModel.transform.parent = _handPool.modelsParent;
              _handPool.modelGroupMapping.Add(spawnedModel, this);
              modelsCheckedOut.Add(spawnedModel);
              return spawnedModel;
            }
          }
        }
        return null;
      }
      public void ReturnToGroup(IHandModel model) {
        modelsCheckedOut.Remove(model);
        modelList.Add(model);
        this._handPool.modelToHandRepMapping.Remove(model);
      }
    }
    public void ReturnToPool(IHandModel model) {
      ModelGroup modelGroup;
      bool groupFound = modelGroupMapping.TryGetValue(model, out modelGroup);
      Assert.IsTrue(groupFound);
      //First see if there is another active Representation that can use this model
      for (int i = 0; i < activeHandReps.Count; i++) {
        HandRepresentation rep = activeHandReps[i];
        if (rep.RepChirality == model.Handedness && rep.RepType == model.HandModelType) {
          bool modelFromGroupFound = false;
          if (rep.handModels != null) {
            //And that Represention does not contain a model from this model's modelGroup
            for (int j = 0; j < modelGroup.modelsCheckedOut.Count; j++) {
              IHandModel modelToCompare = modelGroup.modelsCheckedOut[j];
              for (int k = 0; k < rep.handModels.Count; k++) {
                if (rep.handModels[k] == modelToCompare) {
                  modelFromGroupFound = true;
                }
              }
            }
          }
          if (!modelFromGroupFound) {
            rep.AddModel(model);
            modelToHandRepMapping[model] = rep;
            return;
          }
        }
      }
      //Otherwise return to pool
      modelGroup.ReturnToGroup(model);
    }
    public void RemoveHandRepresentation(HandRepresentation handRepresentation) {
      activeHandReps.Remove(handRepresentation);
    }
    /** Popuates the ModelPool with the contents of the ModelCollection */
    void Start() {
      if (modelsParent == null) {
        Debug.LogWarning("HandPool.ModelsParent needs to reference the parent transform of the hand models.  This transform should be a child of the LMHeadMountedRig transform.");
      }

      for(int i=0; i<ModelPool.Count; i++) {
        InitializeModelGroup(ModelPool[i]);
      }
    }

    private void InitializeModelGroup(ModelGroup collectionGroup) {
        // Prevent the ModelGroup be initialized by multiple times
        if (modelGroupMapping.ContainsValue(collectionGroup)) {
          return;
        }

        collectionGroup._handPool = this;
        IHandModel leftModel;
        IHandModel rightModel;
        if (collectionGroup.IsLeftToBeSpawned) {
          IHandModel modelToSpawn = collectionGroup.LeftModel;
          GameObject spawnedGO = Instantiate(modelToSpawn.gameObject);
          leftModel = spawnedGO.GetComponent<IHandModel>();
          leftModel.transform.parent = modelsParent;
        } else {
          leftModel = collectionGroup.LeftModel;
        }
        if (leftModel != null) {
          collectionGroup.modelList.Add(leftModel);
          modelGroupMapping.Add(leftModel, collectionGroup);
        }

        if (collectionGroup.IsRightToBeSpawned) {
          IHandModel modelToSpawn = collectionGroup.RightModel;
          GameObject spawnedGO = Instantiate(modelToSpawn.gameObject);
          rightModel = spawnedGO.GetComponent<IHandModel>();
          rightModel.transform.parent = modelsParent;
        } else {
          rightModel = collectionGroup.RightModel;
        }
        if (rightModel != null) {
          collectionGroup.modelList.Add(rightModel);
          modelGroupMapping.Add(rightModel, collectionGroup);
        }
    }

    /**
     * MakeHandRepresentation receives a Hand and combines that with an IHandModel to create a HandRepresentation
     * @param hand The Leap Hand data to be drive an IHandModel
     * @param modelType Filters for a type of hand model, for example, physics or graphics hands.
     */

    public HandRepresentation MakeHandRepresentation(Hand hand, ModelType modelType) {
      Chirality handChirality = hand.IsRight ? Chirality.Right : Chirality.Left;
      HandRepresentation handRep = new HandRepresentation(this, hand, handChirality, modelType);
      for (int i = 0; i < ModelPool.Count; i++) {
        ModelGroup group = ModelPool[i];
        if (group.IsEnabled) {
          IHandModel model = group.TryGetModel(handChirality, modelType);
          if (model != null ) {
            handRep.AddModel(model);
            if (!modelToHandRepMapping.ContainsKey(model)) {
              model.group = group;
              modelToHandRepMapping.Add(model, handRep);
            }
          }
        }
      }
      activeHandReps.Add(handRep);
      return handRep;
    }
    /**
    * EnableGroup finds suitable HandRepresentations and adds IHandModels from the ModelGroup, returns them to their ModelGroup and sets the groups IsEnabled to true.
     * @param groupName Takes a string that matches the ModelGroup's groupName serialized in the Inspector
    */
    public void EnableGroup(string groupName) {
      StartCoroutine(enableGroup(groupName));
    }
    private IEnumerator enableGroup(string groupName) {
      yield return new WaitForEndOfFrame();
      ModelGroup group = null;
      for (int i = 0; i < ModelPool.Count; i++) {
        if (ModelPool[i].GroupName == groupName) {
          group = ModelPool[i];
          for (int hp = 0; hp < activeHandReps.Count; hp++) {
            HandRepresentation handRep = activeHandReps[hp];
            IHandModel model = group.TryGetModel(handRep.RepChirality, handRep.RepType);
            if (model != null) {
              handRep.AddModel(model);
              modelToHandRepMapping.Add(model, handRep);
            }
          }
          group.IsEnabled = true;
        }
      }
      if (group == null) {
        Debug.LogWarning("A group matching that name does not exisit in the modelPool");
      }
    }
    /**
     * DisableGroup finds and removes the ModelGroup's IHandModels from their HandRepresentations, returns them to their ModelGroup and sets the groups IsEnabled to false.
     * @param groupName Takes a string that matches the ModelGroup's groupName serialized in the Inspector
     */
    public void DisableGroup(string groupName) {
      StartCoroutine(disableGroup(groupName));
    }
    private IEnumerator disableGroup(string groupName) {
      yield return new WaitForEndOfFrame();
      ModelGroup group = null;
      for (int i = 0; i < ModelPool.Count; i++) {
        if (ModelPool[i].GroupName == groupName) {
          group = ModelPool[i];
          for (int m = 0; m < group.modelsCheckedOut.Count; m++) {
            IHandModel model = group.modelsCheckedOut[m];
            HandRepresentation handRep;
            if (modelToHandRepMapping.TryGetValue(model, out handRep)) {
              handRep.RemoveModel(model);
              group.ReturnToGroup(model);
              m--;
            }
          }
          Assert.AreEqual(0, group.modelsCheckedOut.Count, group.GroupName + "'s modelsCheckedOut List has not been cleared");
          group.IsEnabled = false;
        }
      }
      if (group == null) {
        Debug.LogWarning("A group matching that name does not exisit in the modelPool");
      }
    }
    public void ToggleGroup(string groupName) {
      StartCoroutine(toggleGroup(groupName));
    }
    private IEnumerator toggleGroup(string groupName) {
      yield return new WaitForEndOfFrame();
      ModelGroup modelGroup = ModelPool.Find(i => i.GroupName == groupName);
      if (modelGroup != null) {
        if (modelGroup.IsEnabled == true) {
          DisableGroup(groupName);
          modelGroup.IsEnabled = false;
        } else {
          EnableGroup(groupName);
          modelGroup.IsEnabled = true;
        }
      } else Debug.LogWarning("A group matching that name does not exisit in the modelPool");
    }
    public void AddNewGroup(string groupName, IHandModel leftModel, IHandModel rightModel) {
      ModelGroup newGroup = new ModelGroup();
      newGroup.LeftModel = leftModel;
      newGroup.RightModel = rightModel;
      newGroup.GroupName = groupName;
      newGroup.CanDuplicate = false;
      newGroup.IsEnabled = true;
      ModelPool.Add(newGroup);
      InitializeModelGroup(newGroup);
    }
    public void RemoveGroup(string groupName) {
      while (ModelPool.Find(i => i.GroupName == groupName) != null) {
        ModelGroup modelGroup = ModelPool.Find(i => i.GroupName == groupName);
        if (modelGroup != null) {
          ModelPool.Remove(modelGroup);
        }
      }
    }
    public T GetHandModel<T>(int handId) where T : IHandModel {
      foreach (ModelGroup group in ModelPool) {
        foreach (IHandModel handModel in group.modelsCheckedOut) {
          if (handModel.GetLeapHand().Id == handId && handModel is T) {
            return handModel as T;
          }
        }
      }
      return null;
    }

#if UNITY_EDITOR
    /**In the Unity Editor, Validate that the IHandModel is an instance of a prefab from the scene vs. a prefab from the project. */
    void OnValidate() {
      for (int i = 0; i < ModelPool.Count; i++) {
        if (ModelPool[i] != null) {
          if (ModelPool[i].LeftModel) {
            ModelPool[i].IsLeftToBeSpawned = shouldBeSpawned(ModelPool[i].LeftModel);
          }
          if (ModelPool[i].RightModel) {
            ModelPool[i].IsRightToBeSpawned = shouldBeSpawned(ModelPool[i].RightModel);
          }
        }
      }
    }

    private bool shouldBeSpawned(Object model) {
      var prefabType = PrefabUtility.GetPrefabType(model);
      if (PrefabUtility.GetPrefabType(this) != PrefabType.Prefab) {
        return prefabType == PrefabType.Prefab;
      } else {
        return PrefabUtility.GetPrefabObject(model) != PrefabUtility.GetPrefabObject(this);
      }
    }

#endif
  }
}


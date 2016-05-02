using UnityEngine;
using UnityEngine.Assertions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
  public class HandPool :
    HandFactory {
    [SerializeField]
    private List<ModelGroup> ModelPool;
    private List<HandRepresentation> activeHandReps = new List<HandRepresentation>();

    private Dictionary<IHandModel, ModelGroup> modelGroupMapping = new Dictionary<IHandModel, ModelGroup>();
    private Dictionary<IHandModel, HandRepresentation> modelToHandRepMapping = new Dictionary<IHandModel, HandRepresentation>();

    [System.Serializable]
    public class ModelGroup {
      [HideInInspector]
      public HandPool _handPool;
      public string GroupName;

      public IHandModel LeftModel;
      [HideInInspector]
      public bool IsLeftToBeSpawned;
      public IHandModel RightModel;
      [HideInInspector]
      public bool IsRightToBeSpawned;
      [HideInInspector]
      public List<IHandModel> modelList;
      [HideInInspector]
      public List<IHandModel> modelsCheckedOut;
      public bool IsEnabled;
      public bool CanDuplicate;

      public IHandModel TryGetModel(Chirality chirality, ModelType modelType) {
        for (int i = 0; i < modelList.Count; i++) {
          if (modelList[i].HandModelType == modelType
            && modelList[i].Handedness == chirality
            || modelList[i].Handedness == Chirality.Either) {
            IHandModel model = modelList[i];
            modelList.RemoveAt(i);
            modelsCheckedOut.Add(model);
            return model;
          }
        }
        if (CanDuplicate) {
          for (int i = 0; i < modelsCheckedOut.Count; i++) {
            if (modelsCheckedOut[i].HandModelType == modelType
              && modelsCheckedOut[i].Handedness == chirality
              || modelsCheckedOut[i].Handedness == Chirality.Either) {
              IHandModel modelToSpawn = modelsCheckedOut[i];
              IHandModel spawnedModel = GameObject.Instantiate(modelToSpawn);
              spawnedModel.transform.parent = _handPool.transform;
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
      modelGroup.ReturnToGroup(model);
      Assert.IsTrue(groupFound);
    }
    public void RemoveHandRepresentation(HandRepresentation handRep) {
      activeHandReps.Remove(handRep);
    }
    /** Popuates the ModelPool with the contents of the ModelCollection */
    void Start() {
      foreach (ModelGroup collectionGroup in ModelPool) {
        collectionGroup._handPool = this;
        IHandModel leftModel;
        IHandModel rightModel;
        if (collectionGroup.IsLeftToBeSpawned) {
          IHandModel modelToSpawn = collectionGroup.LeftModel;
          GameObject spawnedGO = GameObject.Instantiate(modelToSpawn.gameObject);
          leftModel = spawnedGO.GetComponent<IHandModel>();
          leftModel.transform.parent = transform;
        }
        else {
          leftModel = collectionGroup.LeftModel;
        }
        collectionGroup.modelList.Add(leftModel);
        modelGroupMapping.Add(leftModel, collectionGroup);

        if (collectionGroup.IsRightToBeSpawned) {
          IHandModel modelToSpawn = collectionGroup.RightModel;
          GameObject spawnedGO = GameObject.Instantiate(modelToSpawn.gameObject);
          rightModel = spawnedGO.GetComponent<IHandModel>();
          rightModel.transform.parent = transform;
        }
        else {
          rightModel = collectionGroup.RightModel;
        }
        collectionGroup.modelList.Add(rightModel);
        modelGroupMapping.Add(rightModel, collectionGroup);
      }
    }

    /**
     * MakeHandRepresentation receives a Hand and combines that with an IHandModel to create a HandRepresentation
     * @param hand The Leap Hand data to be drive an IHandModel
     * @param modelType Filters for a type of hand model, for example, physics or graphics hands.
     */

    public override HandRepresentation MakeHandRepresentation(Hand hand, ModelType modelType) {
      Chirality handChirality = hand.IsRight ? Chirality.Right : Chirality.Left;
      HandRepresentation handRep = new HandProxy(this, hand, handChirality, modelType);
      for (int i = 0; i < ModelPool.Count; i++) {
        ModelGroup group = ModelPool[i];
        if (group.IsEnabled) {
          IHandModel model = group.TryGetModel(handChirality, modelType);
          if (model != null) {
            handRep.AddModel(model);
            modelToHandRepMapping.Add(model, handRep);
          }
        }
      }
      activeHandReps.Add(handRep);
      return handRep;
    }
    public void EnableGroup(string groupName) {
      for (int i = 0; i < ModelPool.Count; i++) {
        if (ModelPool[i].GroupName == groupName) {
          ModelGroup group = ModelPool[i];
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
    }
    public void DisableGroup(string groupName) {
      for (int i = 0; i < ModelPool.Count; i++) {
        ModelGroup group = null;
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
    }


#if UNITY_EDITOR
    /**In the Unity Editor, Validate that the IHandModel is an instance of a prefab from the scene vs. a prefab from the project. */
    void OnValidate() {
      for (int i = 0; i < ModelPool.Count; i++) {
        if (ModelPool[i] != null) {
          if (ModelPool[i].LeftModel) {
            ModelPool[i].IsLeftToBeSpawned = PrefabUtility.GetPrefabType(ModelPool[i].LeftModel) == PrefabType.Prefab;
          }
          if (ModelPool[i].RightModel) {
            ModelPool[i].IsRightToBeSpawned = PrefabUtility.GetPrefabType(ModelPool[i].RightModel) == PrefabType.Prefab;
          }
        }
      }
    }

#endif
  }
}


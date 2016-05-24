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
      public List<IHandModel> modelList;
      [HideInInspector]
      public List<IHandModel> modelsCheckedOut;
      public bool IsEnabled = true;
      public bool CanDuplicate;
      /*Looks for suitable IHandModel is the ModelGroup's modelList, if found, it is added to modelsCheckedOut.
       * If not, one can be cloned*/
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
        }
        else {
          EnableGroup(groupName);
          modelGroup.IsEnabled = true;
        }
      }
      else Debug.LogWarning("A group matching that name does not exisit in the modelPool");
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


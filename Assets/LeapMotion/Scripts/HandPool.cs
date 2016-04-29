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
    private bool EnforceHandedness;
    [SerializeField]
    private List<ModelPair> ModelCollection;
    [SerializeField]
    private List<ModelGroup> ModelPool;
    public List<HandRepresentation> ActiveHandReps = new List<HandRepresentation>();

    private Dictionary<IHandModel, ModelGroup> modelGroupMapping = new Dictionary<IHandModel, ModelGroup>();
    private Dictionary<IHandModel, HandRepresentation> modelToHandRepMapping = new Dictionary<IHandModel, HandRepresentation>();

    [System.Serializable]
    public class ModelPair {
      public string PairName = "PairName";
      public IHandModel LeftModel;
      [HideInInspector]
      public bool IsLeftToBeSpawned;
      public IHandModel RightModel;
      [HideInInspector]
      public bool IsRightToBeSpawned;
      public bool IsEnabled = true;
      public bool CanDuplicate;

      public ModelPair() {}

      public ModelPair(string pairName, bool IsEnabled, IHandModel leftModel, IHandModel rightModel) {
        this.PairName = pairName;
        this.LeftModel = leftModel;
        this.RightModel = rightModel;
      }
    }
    [System.Serializable]
    public class ModelGroup {
      private HandPool _handPool;
      public ModelGroup(HandPool handPool) { _handPool = handPool; }
      public string GroupName;
      public List<IHandModel> modelList;
      public List<IHandModel> modelsCheckedOut;
      public bool IsEnabled;
      public bool CanDuplicate;

      public ModelGroup(string groupName, List<IHandModel> modelList, HandPool handPool, bool isEnabled, bool canDuplicate) {
        this.GroupName = groupName;
        this.IsEnabled = isEnabled;
        this.CanDuplicate = canDuplicate;
        this.modelList = modelList;
        this.modelsCheckedOut = new List<IHandModel>();
        this._handPool = handPool;
      }

      public IHandModel TryGetModel(Chirality chirality, ModelType modelType) {
        for (int i = 0; i < modelList.Count; i++) {
          if (modelList[i].Handedness == chirality && modelList[i].HandModelType == modelType) {
            IHandModel model = modelList[i];
            modelList.RemoveAt(i);
            modelsCheckedOut.Add(model);
            return model;
          }
        }
        if (CanDuplicate) {
          for (int i = 0; i < modelsCheckedOut.Count; i++) {
            if (modelsCheckedOut[i].Handedness == chirality && modelsCheckedOut[i].HandModelType == modelType) {
              IHandModel modelToSpawn = modelsCheckedOut[i];
              IHandModel spawnedModel = GameObject.Instantiate(modelToSpawn);
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

    /** Popuates the ModelPool with the contents of the ModelCollection */
    void Start() {
      ModelPool = new List<ModelGroup>();
      foreach (ModelPair pair in ModelCollection) {
        ModelGroup newModelGroup = new ModelGroup(pair.PairName, new List<IHandModel>(), this, pair.IsEnabled, pair.CanDuplicate);
        IHandModel leftModel;
        IHandModel rightModel;
        if (pair.IsLeftToBeSpawned) {
          IHandModel modelToSpawn = pair.LeftModel;
          GameObject spawnedGO = GameObject.Instantiate(modelToSpawn.gameObject);
          leftModel = spawnedGO.GetComponent<IHandModel>();
        }
        else {
          leftModel = pair.LeftModel;
        }
        newModelGroup.modelList.Add(leftModel);
        modelGroupMapping.Add(leftModel, newModelGroup);

        if (pair.IsRightToBeSpawned) {
          IHandModel modelToSpawn = pair.RightModel;
          GameObject spawnedGO = GameObject.Instantiate(modelToSpawn.gameObject);
          rightModel = spawnedGO.GetComponent<IHandModel>();
        }
        else {
          rightModel = pair.RightModel;
        }
        newModelGroup.modelList.Add(rightModel);
        modelGroupMapping.Add(rightModel, newModelGroup);

        ModelPool.Add(newModelGroup);
      }
    }
 
    /**
     * MakeHandRepresentation receives a Hand and combines that with an IHandModel to create a HandRepresentation
     * @param hand The Leap Hand data to be drive an IHandModel
     * @param modelType Filters for a type of hand model, for example, physics or graphics hands.
     */

    public override HandRepresentation MakeHandRepresentation(Hand hand, ModelType modelType) {
      Chirality handChirality = Chirality.Either;
      if (EnforceHandedness) {
        handChirality = hand.IsRight ? Chirality.Right : Chirality.Left;
      }
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
      ActiveHandReps.Add(handRep);
      return handRep;
    }
    public void EnableGroup(string groupName) {
      for (int i = 0; i < ModelPool.Count; i++) {
        if (ModelPool[i].GroupName == groupName) {
          ModelGroup group = ModelPool[i];
          for (int hp = 0; hp < ActiveHandReps.Count; hp++ ) {
            HandRepresentation handRep = ActiveHandReps[hp];
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
    public void DisableGroup( string groupName) {
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
    public void ReturnToPool(IHandModel model) {
      ModelGroup modelGroup;
      bool groupFound = modelGroupMapping.TryGetValue(model, out modelGroup);
      modelGroup.ReturnToGroup(model);
      Assert.IsTrue(groupFound);
    }

#if UNITY_EDITOR
    /**In the Unity Editor, Validate that the IHandModel is an instance of a prefab from the scene vs. a prefab from the project. */
    void OnValidate() {
      for (int i = 0; i < ModelCollection.Count; i++) {
        if (ModelCollection[i] != null) {
          if (ModelCollection[i].LeftModel) {
            if (PrefabUtility.GetPrefabType(ModelCollection[i].LeftModel) == PrefabType.Prefab) {
              ModelCollection[i].IsLeftToBeSpawned = true;
            }
            else {
              ModelCollection[i].IsLeftToBeSpawned = false;
            }
          }
          if (ModelCollection[i].RightModel) {
            if (PrefabUtility.GetPrefabType(ModelCollection[i].RightModel) == PrefabType.Prefab) {
              ModelCollection[i].IsRightToBeSpawned = true;
            }
            else {
              ModelCollection[i].IsRightToBeSpawned = false;
            }
          }
        }
      }
    }
    void ValidateIHandModelPrefab(IHandModel iHandModel) {
      if (PrefabUtility.GetPrefabType(iHandModel) == PrefabType.Prefab) {
        EditorUtility.DisplayDialog("Warning", "This slot needs to have an instance of a prefab from your scene. Make your hand prefab a child of the LeapHanadContrller in your scene,  then drag here", "OK");
      }
    }
#endif
    void Update() {
      if (Input.GetKeyUp(KeyCode.O)) {
        DisableGroup("Poly_Hands");
      }
      if (Input.GetKeyUp(KeyCode.P)) {
        EnableGroup("Poly_Hands");
      }
      if (Input.GetKeyUp(KeyCode.U)) {
        DisableGroup("Graphics_Hands");
      }
      if (Input.GetKeyUp(KeyCode.I)) {
        EnableGroup("Graphics_Hands");
      }
    }
  }
}


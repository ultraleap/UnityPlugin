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
      public IHandModel RightModel;
      public bool IsEnabled = true;

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
      public IHandModel TryGetModel(Chirality chirality, ModelType modelType) {
        for (int i = 0; i < modelList.Count; i++) {
          if (modelList[i].Handedness == chirality && modelList[i].HandModelType == modelType) {
            IHandModel model = modelList[i];
            modelList.RemoveAt(i);
            modelsCheckedOut.Add(model);
            return model;
          }
        }
        //Todo: if spawning enabled
        for (int i = 0; i < modelsCheckedOut.Count; i++) {
          if (modelsCheckedOut[i].Handedness == chirality && modelsCheckedOut[i].HandModelType == modelType) {
            IHandModel modelToSpawn = modelsCheckedOut[i];
            GameObject spawnedGO = GameObject.Instantiate(modelToSpawn.gameObject);
            IHandModel spawnedModel = spawnedGO.GetComponent<IHandModel>();
            _handPool.modelGroupMapping.Add(spawnedModel, this);
            modelsCheckedOut.Add(spawnedModel);
            return spawnedModel;
          }
        }
        return null;
      }
      public void ReturnToGroup(IHandModel model) {
        modelsCheckedOut.Remove(model);
        modelList.Add(model);
        this._handPool.modelToHandRepMapping.Remove(model);
      }
      public ModelGroup(string groupName, bool isEnabled, List<IHandModel> modelList, HandPool handPool) {
        this.GroupName = groupName;
        this.IsEnabled = isEnabled;
        this.modelList = modelList;
        this.modelsCheckedOut = new List<IHandModel>();
        this._handPool = handPool;
      }
    }

    /** Popuates the ModelPool with the contents of the ModelCollection */
    void Start() {
      ModelPool = new List<ModelGroup>();
      foreach (ModelPair pair in ModelCollection) {
        ModelGroup newModelGroup = new ModelGroup(pair.PairName, pair.IsEnabled, new List<IHandModel>(), this);
        newModelGroup.modelList.Add(pair.LeftModel);
        modelGroupMapping.Add(pair.LeftModel, newModelGroup);
        newModelGroup.modelList.Add(pair.RightModel);
        modelGroupMapping.Add(pair.RightModel, newModelGroup);
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
      if(modelGroupMapping.TryGetValue(model, out modelGroup)){
        modelGroup.ReturnToGroup(model);
      }
    }

#if UNITY_EDITOR
    /**In the Unity Editor, Validate that the IHandModel is an instance of a prefab from the scene vs. a prefab from the project. */
    void OnValidate() {
      for (int i = 0; i < ModelCollection.Count; i++) {
        if (ModelCollection[i] != null) {
          if (ModelCollection[i].LeftModel) {
            ValidateIHandModelPrefab(ModelCollection[i].LeftModel);
          }
          if (ModelCollection[i].RightModel) {
            ValidateIHandModelPrefab(ModelCollection[i].RightModel);
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


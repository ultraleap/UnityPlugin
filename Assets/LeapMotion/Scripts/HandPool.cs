using UnityEngine;
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
    private List<ModelPair> ModelCollection;
    [SerializeField]
    private List<ModelGroup> ModelPool;
    public bool EnforceHandedness = false;
    
    [System.Serializable]
    public class ModelPair {
      public string PairName = "PairName";
      public bool IsEnabled = true;
      public IHandModel LeftModel;
      public IHandModel RightModel;

      public ModelPair() {}

      public ModelPair(string pairName, bool IsEnabled, IHandModel leftModel, IHandModel rightModel) {
        this.PairName = pairName;
        this.LeftModel = leftModel;
        this.RightModel = rightModel;
      }
    }
    [System.Serializable]
    public class ModelGroup {
      public string GroupName;
      public bool IsEnabled;
      public List<IHandModel> modelList;
      public ModelGroup(string groupName, bool isEnabled, List<IHandModel> modelList) {
        this.GroupName = groupName;
        this.IsEnabled = isEnabled;
        this.modelList = modelList;
      }
    }

    private Dictionary<IHandModel, ModelGroup> modelGroupMapping = new  Dictionary<IHandModel, ModelGroup>();

    /** Popuates the ModelPool with the contents of the ModelCollection */
    void Start() {
      ModelPool = new List<ModelGroup>();
      foreach (ModelPair pair in ModelCollection) {
        ModelGroup newModelGroup = new ModelGroup(pair.PairName, pair.IsEnabled, new List<IHandModel>());
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
      HandRepresentation handRep = null;
      List<IHandModel> models = new List<IHandModel>();
      foreach (ModelGroup group in ModelPool) {
        for (int i = 0; i < group.modelList.Count; i++) {
          IHandModel model = group.modelList[i];
          bool isCorrectHandedness;
          Chirality handChirality = hand.IsRight ? Chirality.Right : Chirality.Left;
          isCorrectHandedness = model.Handedness == handChirality;
          if (!EnforceHandedness || model.Handedness == Chirality.Either) {
            isCorrectHandedness = true;
          }
          bool isCorrectModelType;
          isCorrectModelType = model.HandModelType == modelType;
          if (isCorrectModelType && isCorrectHandedness) {
            group.modelList.RemoveAt(i);
            //--i;
            models.Add(model);
            break;
          }
        }
      }
      handRep = new HandProxy(this, models, hand);
      return handRep;
    }

    public void ReturnToPool(IHandModel model){
      ModelGroup modelGroup = modelGroupMapping[model];
      modelGroup.modelList.Add(model);
    }

    public void EnableDisablePair(string groupName, bool isEnabled) {
      for (int i = 0; i < modelGroupMapping.Count; i++) {
        ModelGroup group = modelGroupMapping.ElementAt(i).Value;
        if (group.GroupName == groupName) {
          IHandModel model = modelGroupMapping.ElementAt(i).Key;
          model.transform.gameObject.SetActive(isEnabled);
          model.IsEnabled = false;
        }
      }
      //IHandModel model = modelGroupMapping.Where(z => z.Value.GroupName == groupName).FirstOrDefault().Key;
      //Debug.Log("model = " + model);
      //model.transform.gameObject.SetActive(isEnabled);
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
        EnableDisablePair("Poly_Hands", false);
      }
      if (Input.GetKeyUp(KeyCode.P)) {
        EnableDisablePair("Poly_Hands", true);
      }
    }
  }
}

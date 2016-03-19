using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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
    private List<IHandModel> ModelCollection;
    public List<IHandModel> ModelPool;
    public LeapHandController controller_ { get; set; }
    public bool EnforceHandedness = false;


    /** Popuates the ModelPool with the contents of the ModelCollection */
    void Start() {
      ModelPool = new List<IHandModel>();
      for (int i = 0; i < ModelCollection.Count; i++) {
        if (ModelCollection[i] != null) {
          ModelPool.Add(ModelCollection[i]);
        }
      }
      controller_ = GetComponent<LeapHandController>();
    }
    /**
     * MakeHandRepresentation receives a Hand and combines that with an IHandModel to create a HandRepresentation
     * @param hand The Leap Hand data to be drive an IHandModel
     * @param modelType Filters for a type of hand model, for example, physics or graphics hands.
     */
    public override HandRepresentation MakeHandRepresentation(Hand hand, ModelType modelType) {
      HandRepresentation handRep = null;
      for (int i = 0; i < ModelPool.Count; i++) {
        IHandModel model = ModelPool[i];
        bool isCorrectHandedness;
        Chirality handChirality = hand.IsRight ? Chirality.Right : Chirality.Left;
        isCorrectHandedness = model.Handedness == handChirality;
        if (!EnforceHandedness || model.Handedness == Chirality.Either) {
          isCorrectHandedness = true;
        }
        bool isCorrectModelType;
        isCorrectModelType = model.HandModelType == modelType;
        if (isCorrectModelType && isCorrectHandedness) {
            ModelPool.RemoveAt(i);
            handRep = new HandProxy(this, model, hand);
            break;
        }
      }
      return handRep;
    }
#if UNITY_EDITOR
    /**In the Unity Editor, Validate that the IHandModel is an instance of a prefab from the scene vs. a prefab from the project. */
    void OnValidate() {
      for (int i = 0; i < ModelCollection.Count; i++) {
        if (ModelCollection[i] != null) {
          ValidateIHandModelPrefab(ModelCollection[i]);
        }
      }
    }
    void ValidateIHandModelPrefab(IHandModel iHandModel) {
      if (PrefabUtility.GetPrefabType(iHandModel) == PrefabType.Prefab) {
        EditorUtility.DisplayDialog("Warning", "This slot needs to have an instance of a prefab from your scene. Make your hand prefab a child of the LeapHanadContrller in your scene,  then drag here", "OK");
      }
    }
#endif
  }
}

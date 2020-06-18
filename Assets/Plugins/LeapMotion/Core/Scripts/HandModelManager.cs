/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Assertions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;
using System;
using Leap.Unity.Attributes;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity {

  /// <summary>
  /// The HandModelManager manages a pool of HandModelBases and makes HandRepresentations
  /// when a it detects a Leap Hand from its configured LeapProvider.
  /// 
  /// When a HandRepresentation is created, a HandModelBase is removed from the pool.
  /// When a HandRepresentation is finished, its HandModelBase is returned to the pool.
  /// 
  /// This class was formerly known as HandPool.
  /// </summary>
  public class HandModelManager : MonoBehaviour {
    
    #region Formerly in LeapHandController

    protected Dictionary<int, HandRepresentation> graphicsHandReps = new Dictionary<int, HandRepresentation>();
    protected Dictionary<int, HandRepresentation> physicsHandReps = new Dictionary<int, HandRepresentation>();

    protected bool graphicsEnabled = true;
    protected bool physicsEnabled = true;

    public bool GraphicsEnabled {
      get {
        return graphicsEnabled;
      }
      set {
        graphicsEnabled = value;
      }
    }

    public bool PhysicsEnabled {
      get {
        return physicsEnabled;
      }
      set {
        physicsEnabled = value;
      }
    }

    /** Updates the graphics HandRepresentations. */
    protected virtual void OnUpdateFrame(Frame frame) {
      if (frame != null && graphicsEnabled) {
        UpdateHandRepresentations(graphicsHandReps, ModelType.Graphics, frame);
      }
    }

    /** Updates the physics HandRepresentations. */
    protected virtual void OnFixedFrame(Frame frame) {
      if (frame != null && physicsEnabled) {
        UpdateHandRepresentations(physicsHandReps, ModelType.Physics, frame);
      }
    }

    /** 
    * Updates HandRepresentations based in the specified HandRepresentation Dictionary.
    * Active HandRepresentation instances are updated if the hand they represent is still
    * present in the Provider's CurrentFrame; otherwise, the HandRepresentation is removed. If new
    * Leap Hand objects are present in the Leap HandRepresentation Dictionary, new HandRepresentations are 
    * created and added to the dictionary. 
    * @param all_hand_reps = A dictionary of Leap Hand ID's with a paired HandRepresentation
    * @param modelType Filters for a type of hand model, for example, physics or graphics hands.
    * @param frame The Leap Frame containing Leap Hand data for each currently tracked hand
    */
    protected virtual void UpdateHandRepresentations(Dictionary<int, HandRepresentation> all_hand_reps, ModelType modelType, Frame frame) {
      for (int i = 0; i < frame.Hands.Count; i++) {
        var curHand = frame.Hands[i];
        HandRepresentation rep;
        if (!all_hand_reps.TryGetValue(curHand.Id, out rep)) {
          rep = MakeHandRepresentation(curHand, modelType);
          all_hand_reps.Add(curHand.Id, rep);
        }
        if (rep != null) {
          rep.IsMarked = true;
          rep.UpdateRepresentation(curHand);
          rep.LastUpdatedTime = (int)frame.Timestamp;
        }
      }

      /** Mark-and-sweep to finish unused HandRepresentations */
      HandRepresentation toBeDeleted = null;
      for (var it = all_hand_reps.GetEnumerator(); it.MoveNext();) {
        var r = it.Current;
        if (r.Value != null) {
          if (r.Value.IsMarked) {
            r.Value.IsMarked = false;
          }
          else {
            /** Initialize toBeDeleted with a value to be deleted */
            //Debug.Log("Finishing");
            toBeDeleted = r.Value;
          }
        }
      }
      /**Inform the representation that we will no longer be giving it any hand updates 
       * because the corresponding hand has gone away */
      if (toBeDeleted != null) {
        all_hand_reps.Remove(toBeDeleted.HandID);
        toBeDeleted.Finish();
      }
    }

    #endregion

    #region HandPool Inspector

    [Tooltip("The LeapProvider to use to drive hand representations in the defined "
           + "model pool groups.")]
    [SerializeField]
    [OnEditorChange("leapProvider")]
    private LeapProvider _leapProvider;
    public LeapProvider leapProvider {
      get { return _leapProvider; }
      set {
        if (_leapProvider != null) {
          _leapProvider.OnFixedFrame  -= OnFixedFrame;
          _leapProvider.OnUpdateFrame -= OnUpdateFrame;
        }

        _leapProvider = value;

        if (_leapProvider != null) {
          _leapProvider.OnFixedFrame  += OnFixedFrame;
          _leapProvider.OnUpdateFrame += OnUpdateFrame;
        }
      }
    }

    [SerializeField]
    [Tooltip("To add a new set of Hand Models, first add the Left and Right objects as "
           + "children of this object. Then create a new Model Pool entry referencing "
           + "the new Hand Models and configure it as desired. "
           + "Once configured, the Hand Model Manager object pipes Leap tracking data "
           + "to the Hand Models as hands are tracked, and spawns duplicates as needed "
           + "if \"Can Duplicate\" is enabled.")]
    private List<ModelGroup> ModelPool = new List<ModelGroup>();
    private List<HandRepresentation> activeHandReps = new List<HandRepresentation>();

    private Dictionary<HandModelBase, ModelGroup> modelGroupMapping = new Dictionary<HandModelBase, ModelGroup>();
    private Dictionary<HandModelBase, HandRepresentation> modelToHandRepMapping = new Dictionary<HandModelBase, HandRepresentation>();

    #endregion

    #region ModelGroup Class

    /**
     * ModelGroup contains a left/right pair of HandModelBase's
     * @param modelList The HandModelBases available for use by HandRepresentations
     * @param modelsCheckedOut The HandModelBases currently in use by active HandRepresentations
     * @param IsEnabled determines whether the ModelGroup is active at app Start(), though ModelGroup's are controlled with the EnableGroup() & DisableGroup methods.
     * @param CanDuplicate Allows a HandModelBases in the ModelGroup to be cloned at runtime if a suitable HandModelBase isn't available.
     */
    [System.Serializable]
    public class ModelGroup {
      public string GroupName;
      [HideInInspector]
      public HandModelManager _handModelManager;

      public HandModelBase LeftModel;
      [HideInInspector]
      public bool IsLeftToBeSpawned;
      public HandModelBase RightModel;
      [HideInInspector]
      public bool IsRightToBeSpawned;
      [NonSerialized]
      public List<HandModelBase> modelList = new List<HandModelBase>();
      [NonSerialized]
      public List<HandModelBase> modelsCheckedOut = new List<HandModelBase>();
      public bool IsEnabled = true;
      public bool CanDuplicate;

      /*Looks for suitable HandModelBase is the ModelGroup's modelList, if found, it is added to modelsCheckedOut.
       * If not, one can be cloned*/
      public HandModelBase TryGetModel(Chirality chirality, ModelType modelType) {
        for (int i = 0; i < modelList.Count; i++) {
          if (modelList[i].HandModelType == modelType && modelList[i].Handedness == chirality) {
            HandModelBase model = modelList[i];
            modelList.RemoveAt(i);
            modelsCheckedOut.Add(model);
            return model;
          }
        }
        if (CanDuplicate) {
          for (int i = 0; i < modelsCheckedOut.Count; i++) {
            if (modelsCheckedOut[i].HandModelType == modelType && modelsCheckedOut[i].Handedness == chirality) {
              HandModelBase modelToSpawn = modelsCheckedOut[i];
              HandModelBase spawnedModel = GameObject.Instantiate(modelToSpawn);
              spawnedModel.transform.parent = _handModelManager.transform;
              _handModelManager.modelGroupMapping.Add(spawnedModel, this);
              modelsCheckedOut.Add(spawnedModel);
              return spawnedModel;
            }
          }
        }
        return null;
      }
      public void ReturnToGroup(HandModelBase model) {
        modelsCheckedOut.Remove(model);
        modelList.Add(model);
        this._handModelManager.modelToHandRepMapping.Remove(model);
      }
    }

    #endregion

    #region HandPool Methods

    public void ReturnToPool(HandModelBase model) {
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
              HandModelBase modelToCompare = modelGroup.modelsCheckedOut[j];
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

    // TODO: DELETEME -- unnecessary?
    // public bool TryGetHand(out HandModelBase handModel, string groupName,
    //   bool isRight)
    // {
    //   ModelGroup group = ModelPool.Find(g => g.GroupName.Equals(groupName));
    //   if (group == null) { return false; }
    //   else {

    //   }
    // }

    #endregion

    #region Hand Representations

    /**
     * MakeHandRepresentation receives a Hand and combines that with a HandModelBase to create a HandRepresentation
     * @param hand The Leap Hand data to be drive a HandModelBase
     * @param modelType Filters for a type of hand model, for example, physics or graphics hands.
     */
    public HandRepresentation MakeHandRepresentation(Hand hand, ModelType modelType) {
      Chirality handChirality = hand.IsRight ? Chirality.Right : Chirality.Left;
      HandRepresentation handRep = new HandRepresentation(this, hand, handChirality, modelType);
      for (int i = 0; i < ModelPool.Count; i++) {
        ModelGroup group = ModelPool[i];
        if (group.IsEnabled) {
          HandModelBase model = group.TryGetModel(handChirality, modelType);
          if (model != null) {
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

    public void RemoveHandRepresentation(HandRepresentation handRepresentation) {
      activeHandReps.Remove(handRepresentation);
    }

    #endregion

    #region Unity Events

    protected virtual void OnEnable() {
      if (_leapProvider == null) {
        _leapProvider = Hands.Provider;
      }

      _leapProvider.OnUpdateFrame -= OnUpdateFrame;
      _leapProvider.OnUpdateFrame += OnUpdateFrame;

      _leapProvider.OnFixedFrame -= OnFixedFrame;
      _leapProvider.OnFixedFrame += OnFixedFrame;
    }

    protected virtual void OnDisable() {
      _leapProvider.OnUpdateFrame -= OnUpdateFrame;
      _leapProvider.OnFixedFrame -= OnFixedFrame;
    }

    /** Popuates the ModelPool with the contents of the ModelCollection */
    void Start() {
      for(int i=0; i<ModelPool.Count; i++) {
        InitializeModelGroup(ModelPool[i]);
      }
    }

    #endregion

    #region Group Methods

    private void InitializeModelGroup(ModelGroup collectionGroup) {
        // Prevent the ModelGroup be initialized by multiple times
        if (modelGroupMapping.ContainsValue(collectionGroup)) {
          return;
        }

        collectionGroup._handModelManager = this;
        HandModelBase leftModel;
        HandModelBase rightModel;
        if (collectionGroup.IsLeftToBeSpawned) {
          HandModelBase modelToSpawn = collectionGroup.LeftModel;
          GameObject spawnedGO = Instantiate(modelToSpawn.gameObject);
          leftModel = spawnedGO.GetComponent<HandModelBase>();
          leftModel.transform.parent = this.transform;
        } else {
          leftModel = collectionGroup.LeftModel;
        }
        if (leftModel != null) {
          collectionGroup.modelList.Add(leftModel);
          modelGroupMapping.Add(leftModel, collectionGroup);
        }

        if (collectionGroup.IsRightToBeSpawned) {
          HandModelBase modelToSpawn = collectionGroup.RightModel;
          GameObject spawnedGO = Instantiate(modelToSpawn.gameObject);
          rightModel = spawnedGO.GetComponent<HandModelBase>();
          rightModel.transform.parent = this.transform;
        } else {
          rightModel = collectionGroup.RightModel;
        }
        if (rightModel != null) {
          collectionGroup.modelList.Add(rightModel);
          modelGroupMapping.Add(rightModel, collectionGroup);
        }
    }

    /**
    * EnableGroup finds suitable HandRepresentations and adds HandModelBases from the ModelGroup, returns them to their ModelGroup and sets the groups IsEnabled to true.
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
            HandModelBase model = group.TryGetModel(handRep.RepChirality, handRep.RepType);
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
     * DisableGroup finds and removes the ModelGroup's HandModelBases from their HandRepresentations, returns them to their ModelGroup and sets the groups IsEnabled to false.
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
            HandModelBase model = group.modelsCheckedOut[m];
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

    public void AddNewGroup(string groupName, HandModelBase leftModel, HandModelBase rightModel) {
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

    public T GetHandModel<T>(int handId) where T : HandModelBase {
      foreach (ModelGroup group in ModelPool) {
        foreach (HandModelBase handModel in group.modelsCheckedOut) {
          if (handModel.GetLeapHand().Id == handId && handModel is T) {
            return handModel as T;
          }
        }
      }
      return null;
    }

    #endregion

    #region Editor-only Methods

    #if UNITY_EDITOR
    /**In the Unity Editor, Validate that the HandModelBase is an instance of a prefab from the scene vs. a prefab from the project. */
    void OnValidate() {
      if (ModelPool != null) {
        for (int i = 0; i < ModelPool.Count; i++) {
          if (ModelPool[i] != null) {
            if (ModelPool[i].LeftModel) {
              ModelPool[i].IsLeftToBeSpawned = Utils.IsObjectPartOfPrefabAsset(
                ModelPool[i].LeftModel);
            }
            if (ModelPool[i].RightModel) {
              ModelPool[i].IsRightToBeSpawned = Utils.IsObjectPartOfPrefabAsset(
                ModelPool[i].RightModel);
            }
          }
        }
      }
    }

    #endif

    #endregion

  }
}


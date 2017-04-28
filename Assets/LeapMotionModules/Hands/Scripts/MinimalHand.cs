/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System.Collections;
using System;
using Leap;

namespace Leap.Unity{
  public class MinimalHand : IHandModel {
    public override bool SupportsEditorPersistence() {
      return true;
    }
    [SerializeField]
    private Mesh _palmMesh;
  
    [SerializeField]
    private float _palmScale = 0.02f;
  
    [SerializeField]
    private Material _palmMat;
  
    [SerializeField]
    private Mesh _jointMesh;
  
    [SerializeField]
    private float _jointScale = 0.01f;
  
    [SerializeField]
    private Material _jointMat;
  
    private Hand _hand;
    private Transform _palm;
    private Transform[] _joints;
  
    public override Chirality Handedness {
      get {
        return Handedness;
      }
      set { }
    }
  
    public override ModelType HandModelType {
      get {
        return ModelType.Graphics;
      }
    }
  
    public override void SetLeapHand(Hand hand) {
      _hand = hand;
    }
  
    public override Hand GetLeapHand() {
      return _hand;
    }
  
    public override void InitHand() {
      _joints = new Transform[5 * 4];
      for (int i = 0; i < 20; i++) {
        _joints[i] = createRenderer("Joint", _jointMesh, _jointScale, _jointMat);
      }
  
      _palm = createRenderer("Palm", _palmMesh, _palmScale, _palmMat);
    }
  
    public override void UpdateHand() {
      var list = _hand.Fingers;
      int index = 0;
      for (int i = 0; i < 5; i++) {
        Finger finger = list[i];
        for (int j = 0; j < 4; j++) {
          _joints[index++].position = finger.Bone((Bone.BoneType)j).NextJoint.ToVector3();
        }
      }
  
      _palm.position = _hand.PalmPosition.ToVector3();
    }
  
    private Transform createRenderer(string name, Mesh mesh, float scale, Material mat) {
      GameObject obj = new GameObject(name);
      obj.AddComponent<MeshFilter>().mesh = mesh;
      obj.AddComponent<MeshRenderer>().sharedMaterial = mat;
      obj.transform.parent = transform;
      obj.transform.localScale = Vector3.one * scale;
      return obj.transform;
    }
  }
}

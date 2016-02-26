﻿using UnityEngine;
using System.Collections;
using System;
using Leap;

public class MinimalHand : IHandModel {

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

  private IHand _hand;
  private Transform _palm;
  private Transform[] _joints;

  public override Chirality Handedness {
    get {
      return Chirality.Either;
    }
  }

  public override ModelType HandModelType {
    get {
      return ModelType.Graphics;
    }
  }

  public override void SetLeapHand(IHand hand) {
    _hand = hand;
  }

  public override IHand GetLeapHand() {
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
      IFinger finger = list[i];
      for (int j = 0; j < 4; j++) {
        _joints[index++].position = finger.JointPosition((Finger.FingerJoint)j).ToVector3();
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

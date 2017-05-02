/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap;
using UnityEngine;
using System.Collections;

namespace Leap.Unity{
  public class HandFader : MonoBehaviour {
    public float confidenceSmoothing = 10.0f;
    public AnimationCurve confidenceCurve;
  
    protected HandModel _handModel;
    protected float _smoothedConfidence = 0.0f;
    protected Renderer _renderer;
  
    protected MaterialPropertyBlock _fadePropertyBlock;
  
    private const float EPISLON = 0.005f;
  
    protected virtual float GetUnsmoothedConfidence() {
      return _handModel.GetLeapHand().Confidence;
    }
  
    protected virtual void Awake() {
      _handModel = GetComponent<HandModel>();
      _renderer = GetComponentInChildren<Renderer>();
  
      _fadePropertyBlock = new MaterialPropertyBlock();
      _renderer.GetPropertyBlock(_fadePropertyBlock);
      _fadePropertyBlock.SetFloat("_Fade", 0);
      _renderer.SetPropertyBlock(_fadePropertyBlock);
    }
  
    protected virtual void Update() {
      float unsmoothedConfidence = GetUnsmoothedConfidence();
      _smoothedConfidence += (unsmoothedConfidence - _smoothedConfidence) / confidenceSmoothing;
      float fade = confidenceCurve.Evaluate(_smoothedConfidence);
      _renderer.enabled = (fade > EPISLON);
  
      _renderer.GetPropertyBlock(_fadePropertyBlock);
      _fadePropertyBlock.SetFloat("_Fade", confidenceCurve.Evaluate(_smoothedConfidence));
      _renderer.SetPropertyBlock(_fadePropertyBlock);
    }
  }
}

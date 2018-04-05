/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;
using Leap.Unity.GraphicalRenderer;

public class ExampleArrayController : MonoBehaviour {

  [SerializeField]
  private AnimationCurve _motionCurve;

  [SerializeField]
  private Gradient _gradient;

  private List<LeapGraphic> _graphics = new List<LeapGraphic>();
  private List<Vector3> _originalPositions = new List<Vector3>();
  private List<LeapBlendShapeData> _blendShapeData = new List<LeapBlendShapeData>();
  private List<LeapRuntimeTintData> _tintData = new List<LeapRuntimeTintData>();

  private void Start() {
    _graphics.AddRange(GetComponentsInChildren<LeapGraphic>());
    _graphics.Query().Select(g => g.transform.localPosition).FillList(_originalPositions);
    _graphics.Query().Select(g => g.GetFeatureData<LeapBlendShapeData>()).FillList(_blendShapeData);
    _graphics.Query().Select(g => g.GetFeatureData<LeapRuntimeTintData>()).FillList(_tintData);
  }

  private void Update() {
    float fade = Mathf.Clamp01(Time.time * 0.5f - 0.5f);

    for (int i = 0; i < _graphics.Count; i++) {
      _graphics[i].transform.localPosition = _originalPositions[i];

      float a = fade * 10 * noise(_graphics[i].transform.position, 42.0f, 0.8f);
      float b = noise(_graphics[i].transform.position * 1.7f, 23, 0.35f);
      float c = fade * _motionCurve.Evaluate(b);
      float d = fade * (c * 0.1f + a * (c * 0.03f + 0.01f) + _graphics[i].transform.position.z * 0.14f);

      _blendShapeData[i].amount = c;
      _graphics[i].transform.localPosition += Vector3.up * d;
      _tintData[i].color = fade * _gradient.Evaluate(b);
    }
  }

  private float noise(Vector3 offset, float seed, float speed) {
    float x1 = seed * 23.1239879f;
    float y1 = seed * 82.1239812f;
    x1 -= (int)x1;
    y1 -= (int)y1;
    x1 *= 10;
    y1 *= 10;

    float x2 = seed * 23.1239879f;
    float y2 = seed * 82.1239812f;
    x2 -= (int)x2;
    y2 -= (int)y2;
    x2 *= 10;
    y2 *= 10;

    return Mathf.PerlinNoise(offset.x + x1 + Time.time * speed, offset.z + y1 + Time.time * speed) * 0.5f +
           Mathf.PerlinNoise(offset.x + x2 - Time.time * speed, offset.z + y2 - Time.time * speed) * 0.5f;
  }
}

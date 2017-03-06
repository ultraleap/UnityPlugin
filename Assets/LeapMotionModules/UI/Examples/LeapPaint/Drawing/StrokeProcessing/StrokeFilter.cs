using Leap.Paint;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.StrokeProcessing {

  /// <summary>
  /// StrokeFilters serve the same purpose as MeshFilters serve for Meshes.
  /// StrokeRenderers use them to know what Stroke to render for a given GameObject.
  /// </summary>
  public class StrokeFilter : MonoBehaviour {

    public Stroke stroke;

    private Stroke _lastAttachedStroke;

    void Update() {
      if (stroke != _lastAttachedStroke) {
        _lastAttachedStroke = stroke;
        stroke.OnStrokeModified += OnStrokeModified;                  // Allow changes to the underlying Stroke to propagate to listeners of this Filter.
        OnStrokeModified(stroke, StrokeModificationHint.Overhaul());  // Since our attached Stroke changed, fire an Overhaul modification message to listeners.
      }
    }

    public Action<Stroke, StrokeModificationHint> OnStrokeModified = (x, y) => { };

  }


}
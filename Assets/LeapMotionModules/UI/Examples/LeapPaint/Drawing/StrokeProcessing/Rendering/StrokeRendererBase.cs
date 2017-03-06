using Leap.Paint;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.StrokeProcessing.Rendering {

  [ExecuteInEditMode]
  public abstract class StrokeRendererBase : MonoBehaviour {

    private StrokeFilter strokeFilter;

    protected virtual void Update() {
      if (strokeFilter == null) {
        strokeFilter = GetComponent<StrokeFilter>();
        if (strokeFilter != null) {
          strokeFilter.OnStrokeModified += UpdateRenderer;
        }
      }
    }

    /// <summary>
    /// Render (or re-render) the given Stroke, or (better yet) use the StrokeModificationHint to
    /// re-render the given Stroke in a more optimized way where possible.
    /// </summary>
    public abstract void UpdateRenderer(Stroke stroke, StrokeModificationHint modHint);

  }


}
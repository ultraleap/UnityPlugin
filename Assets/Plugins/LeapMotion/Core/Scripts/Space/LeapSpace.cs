/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Leap.Unity.Query;

namespace Leap.Unity.Space {

  [ExecuteInEditMode]
  public abstract class LeapSpace : LeapSpaceAnchor {
    private static List<LeapSpace> _enabledSpaces = new List<LeapSpace>();
    public static List<LeapSpace> allEnabled {
      get {
#if UNITY_EDITOR
        //if we are in the editor, don't bother with enable/disable lifecycle
        //just use FindObjectsOfType!
        if (!Application.isPlaying) {
          FindObjectsOfType<LeapSpace>().Query().Where(c => c.enabled).FillList(_enabledSpaces);
        }
#endif
        return _enabledSpaces;
      }
    }

    private List<LeapSpaceAnchor> _anchors = new List<LeapSpaceAnchor>();

    protected override void OnEnable() {
      base.OnEnable();

      _enabledSpaces.Add(this);

      RebuildHierarchy();
      RecalculateTransformers();
    }

    protected override void OnDisable() {
      base.OnDisable();

      _enabledSpaces.Remove(this);

      for (int i = 0; i < _anchors.Count; i++) {
        _anchors[i].space = null;
        _anchors[i].transformer = null;
      }
    }

    /// <summary>
    /// Returns all active anchors in depth-first pre-order
    /// </summary>
    public List<LeapSpaceAnchor> anchors {
      get {
        return _anchors;
      }
    }

    /// <summary>
    /// Call to traverse the entire hierarchy and rebuild the relationship
    /// between anchors.  Call this whenever any of the following happens:
    ///  - An anchor is created
    ///  - An anchor is enabled / disabled
    ///  - An enabled anchor is destroyed
    ///  - The relative position in the hierarchy of any two anchors changes.
    /// </summary>
    public void RebuildHierarchy() {
      Assert.IsTrue(enabled, "Should never call RebuildHierarchy on a disabled space.");

      _anchors.Clear();
      rebuildHierarchyRecursively(transform);

      Assert.IsTrue(_anchors.Count > 0, "There should always be at least a single anchor (this one).");
      Assert.AreEqual(_anchors[0], this, "A space should always be the first element in the anchor list.");
    }

    /// <summary>
    /// Call to update all transformers in the space.  Call this whenever any 
    /// anchor or parent of an anchor changes it's transform.
    /// </summary>
    public void RecalculateTransformers() {
      Assert.IsTrue(enabled, "Should never call RecalculateTransformers on a disabled space.");

      transformer = CosntructBaseTransformer();

      //Depth-first pre-order ensures that a parent already has it's transformer up-to-date
      //by the time a direct child needs to be updated.
      for (int i = 1; i < _anchors.Count; i++) {
        var anchor = _anchors[i];
        var parent = anchor.parent;

        Assert.IsNotNull(anchor, "Make sure to call RebuildHierarchy before RecalculateTransformers if you delete an anchor.");
        Assert.IsTrue(anchor.enabled, "Make sure to all RebuildHierarchy before RecalculateTransformers if you disable an anchor.");

        UpdateTransformer(anchor.transformer, parent.transformer);
      }
    }

    /// <summary>
    /// Get a hash of all features in this space.  This is useful if you want to know
    /// if anything has changed about the settings of this space. 
    /// </summary>
    public abstract Hash GetSettingHash();

    protected abstract ITransformer CosntructBaseTransformer();
    protected abstract ITransformer ConstructTransformer(LeapSpaceAnchor anchor);
    protected abstract void UpdateTransformer(ITransformer transformer, ITransformer parent);

    private void rebuildHierarchyRecursively(Transform root) {
      var anchor = root.GetComponent<LeapSpaceAnchor>();
      if (anchor != null && anchor.enabled) {
        anchor.space = this;
        anchor.RecalculateParentAnchor();
        anchor.transformer = ConstructTransformer(anchor);

        _anchors.Add(anchor);
      }

      int childCount = root.childCount;
      for (int i = 0; i < childCount; i++) {
        rebuildHierarchyRecursively(root.GetChild(i));
      }
    }
  }
}

using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Animation {
  
  public class SwitchTreeController : ObjectSwitch {
    
    [Header("Switch Tree")]
    [SerializeField]
    private SwitchTree tree;

    #region Unity Events

    protected override void Reset() {
      base.Reset();

      initialize();
    }

    protected override void OnValidate() {
      base.OnValidate();

      initialize();
    }

    protected override void Start() {
      base.Start();

      initialize();
    }

    private void initialize() {
      refreshTree();
    }

    private void refreshTree() {
      tree = new SwitchTree(this.transform, tree == null ? null : tree.curActiveNodeName);
    }

    #endregion

    #region Public API
    
    public string currentState {
      get { return tree.curActiveNodeName; }
    }

    /// <summary>
    /// Traverses the tree, deactivating all switch pathways that do not lead to the
    /// switch node identified by this nodeName, then activating all switches along the
    /// path to the switch node identified by this nodeName, but no deeper. Switches that
    /// are children of the named node are also deactivated.
    /// </summary>
    public void SwitchTo(string nodeName) {
#if UNITY_EDITOR
      UnityEditor.Undo.RecordObject(this, "Set SwitchTreeController State");
#endif
      tree.SwitchTo(nodeName);
    }
    public void SwitchTo(string nodeName, bool immediately = false) {
      tree.SwitchTo(nodeName, immediately);
    }

    /// <summary>
    /// Equivalent to calling SwitchTo with the name of the object that contains this
    /// SwitchTreeController. This turns off all of the child pathways available from
    /// the switch tree.
    /// </summary>
    public void SwitchToRoot() {
      tree.SwitchTo(transform.name);
    }

    public void SwitchToRoot(bool immediately) {
      tree.SwitchTo(transform.name, immediately);
    }

    /// <summary>
    /// If the specified state node is not active, the switch tree will switch to that
    /// node. If switch tree will switch to that node's parent, deactivating the
    /// specified node. (Calling this method will also deactivate any sibling nodes that
    /// might have been activated out of the context of the SwitchTree.)
    /// </summary>
    public void ToggleState(string nodeName) {
      tree.ToggleState(nodeName, !Application.isPlaying);
    }
    public void ToggleState(string nodeName, bool immediately) {
      tree.ToggleState(nodeName, immediately);
    }

    #endregion

  }


}

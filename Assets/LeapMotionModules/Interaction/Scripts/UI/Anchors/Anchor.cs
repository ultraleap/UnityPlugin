using Leap.Unity.Attributes;
using Leap.Unity.RuntimeGizmos;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.Interaction {

  public class Anchor : MonoBehaviour {

    private static HashSet<Anchor> _allAnchors; 
    public static HashSet<Anchor> allAnchors {
      get {
        if (_allAnchors == null) {
          _allAnchors = new HashSet<Anchor>();
        }
        return _allAnchors;
      }
    }

    [Tooltip("Should this anchor allow multiple objects to be attached to it at the same time? "
           + "This property is enforced by AnchorGroups and AnchorableBehaviours.")]
    public bool allowMultipleObjects = false;

    private HashSet<AnchorGroup> _groups = new HashSet<AnchorGroup>();
    public HashSet<AnchorGroup> groups { get { return _groups; } }

    private HashSet<AnchorableBehaviour> _preferringAnchorables = new HashSet<AnchorableBehaviour>();

    private HashSet<AnchorableBehaviour> _anchoredObjects = new HashSet<AnchorableBehaviour>();
    /// <summary>
    /// Gets the set of AnchorableBehaviours currently attached to this anchor.
    /// </summary>
    public HashSet<AnchorableBehaviour> anchoredObjects { get { return _anchoredObjects; } }

    public bool isPreferred { get { return _preferringAnchorables.Count > 0; } }

    public bool hasAnchoredObjects { get { return _anchoredObjects.Count > 0; } }

    #region Events

    /// <summary>
    /// Called as soon as any anchorable objects prefer this anchor if they were to try to
    /// attach to an anchor.
    /// </summary>
    public Action OnAnchorPreferred = () => { };

    /// <summary>
    /// Called when no anchorable objects prefer this anchor any more.
    /// </summary>
    public Action OnAnchorNotPreferred = () => { };

    /// <summary>
    /// Called every Update() that an AnchorableBehaviour prefers this anchor.
    /// </summary>
    public Action WhileAnchorPreferred = () => { };

    /// <summary>
    /// Called as soon as any anchorables become attached to this anchor.
    /// </summary>
    public Action OnAnchorablesAttached = () => { };

    /// <summary>
    /// Called when there are no anchorables attached to this anchor.
    /// </summary>
    public Action OnNoAnchorablesAttached = () => { };

    /// <summary>
    /// Called every Update() that one or more AnchorableBehaviours is attached to this anchor.
    /// </summary>
    public Action WhileAnchorablesAttached = () => { };

    #endregion

    void Awake() {
      allAnchors.Add(this);
    }

    void Start() {
      initUnityEvents();
    }

    void Update() {
      updateAnchorCallbacks();
    }

    void OnDestroy() {
      foreach (var group in groups) {
        group.Remove(this);
      }

      allAnchors.Remove(this);
    }

    #region Anchor Callbacks

    public void NotifyAttached(AnchorableBehaviour anchObj) {
      _anchoredObjects.Add(anchObj);

      if (_anchoredObjects.Count == 1) {
        OnAnchorablesAttached();
      }
    }

    public void NotifyDetached(AnchorableBehaviour anchObj) {
      _anchoredObjects.Remove(anchObj);

      if (_anchoredObjects.Count == 0) {
        OnNoAnchorablesAttached();
      }
    }

    private void updateAnchorCallbacks() {
      WhileAnchorPreferred();
      WhileAnchorablesAttached();
    }

    public void NotifyAnchorPreference(AnchorableBehaviour anchObj) {
      _preferringAnchorables.Add(anchObj);

      if (_preferringAnchorables.Count == 1) {
        OnAnchorPreferred();
      }
    }

    public void NotifyEndAnchorPreference(AnchorableBehaviour anchObj) {
      _preferringAnchorables.Remove(anchObj);

      if (_preferringAnchorables.Count == 0) {
        OnAnchorNotPreferred();
      }
    }

    #endregion

    #region Gizmos

    public static Color anchorGizmoColor = new Color(0.6F, 0.2F, 0.8F);

    void OnDrawGizmos() {
      Matrix4x4 origMatrix = Gizmos.matrix;
      Gizmos.matrix = this.transform.localToWorldMatrix;
      Gizmos.color = anchorGizmoColor;
      float radius = 0.02F;

      drawWireSphereGizmo(Vector3.zero, radius);

      drawSphereCirclesGizmo(8, Vector3.zero, radius, Vector3.up);

      Gizmos.matrix = origMatrix;
    }

    private static Vector3[] worldDirs = new Vector3[] { Vector3.right, Vector3.up, Vector3.forward };

    private void drawWireSphereGizmo(Vector3 pos, float radius) {
      foreach (var dir in worldDirs) {
        // TODO: Implement anchor gizmos
        return;
      }
    }

    private void drawSphereCirclesGizmo(int numCircles, Vector3 pos, float radius, Vector3 poleDir) {
      // TODO: Implement anchor gizmos
      return;
    }

    #endregion

    #region Unity Events (Internal)

    [SerializeField]
    private EnumEventTable _eventTable;

    public enum EventType {
      OnAnchorPreferred = 100,
      OnAnchorNotPreferred = 110,
      WhileAnchorPreferred = 120,
      OnAnchorablesAttached = 130,
      OnNoAnchorablesAttached = 140,
      WhileAnchorablesAttached = 150
    }

    private void initUnityEvents() {
      setupCallback(ref OnAnchorPreferred,        EventType.OnAnchorPreferred);
      setupCallback(ref OnAnchorNotPreferred,     EventType.OnAnchorNotPreferred);
      setupCallback(ref WhileAnchorPreferred,     EventType.WhileAnchorPreferred);
      setupCallback(ref OnAnchorablesAttached,    EventType.OnAnchorablesAttached);
      setupCallback(ref OnNoAnchorablesAttached,  EventType.OnNoAnchorablesAttached);
      setupCallback(ref WhileAnchorablesAttached, EventType.WhileAnchorablesAttached);
    }

    private void setupCallback(ref Action action, EventType type) {
      action += () => _eventTable.Invoke((int)type);
    }

    #endregion

  }

}
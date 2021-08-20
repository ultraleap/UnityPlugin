/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

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

    [Tooltip("Should this anchor attempt to enable and disable the GameObjects of attached "
           + "AnchorableBehaviours when its own active state changes? If this setting is enabled, "
           + "the Anchor will deactivate the attached objects when its own GameObject is deactivated "
           + "or if its script is disabled, and similarly for becoming active or enabled.")]
    public bool matchActiveStateWithAttachedObjects = false;

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

    void OnEnable() {
      if (matchActiveStateWithAttachedObjects) {
        foreach (var anchObj in _anchoredObjects) {
          anchObj.gameObject.SetActive(true);
        }
      }
    }

    void Start() {
      initUnityEvents();
    }

    void Update() {
      updateAnchorCallbacks();
    }

    void OnAnchorDisabled() {
      if (matchActiveStateWithAttachedObjects) {
        foreach (var anchObj in _anchoredObjects) {
          anchObj.gameObject.SetActive(false);
        }
      }
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

    public static Color AnchorGizmoColor = new Color(0.6F, 0.2F, 0.8F);

    void OnDrawGizmosSelected() {
      Matrix4x4 origMatrix = Gizmos.matrix;
      Gizmos.matrix = this.transform.localToWorldMatrix;
      Gizmos.color = AnchorGizmoColor;
      float radius = 0.015F;

      drawWireSphereGizmo(Vector3.zero, radius);

      drawSphereCirclesGizmo(5, Vector3.zero, radius, Vector3.forward);

      Gizmos.matrix = origMatrix;
    }

    private static Vector3[] worldDirs = new Vector3[] { Vector3.right, Vector3.up, Vector3.forward };

    private void drawWireSphereGizmo(Vector3 pos, float radius) {
      foreach (var dir in worldDirs) {
        if (dir == Vector3.forward) continue;
        Utils.DrawCircle(pos, dir, radius, AnchorGizmoColor, quality: 24, depthTest: true);
      }
    }

    private void drawSphereCirclesGizmo(int numCircles, Vector3 pos, float radius, Vector3 poleDir) {
      float dTheta = 180F / numCircles;
      float halfTheta = dTheta / 2F;

      for (int i = 0; i < numCircles; i++) {
        float curTheta = (dTheta * i) + halfTheta;
        Utils.DrawCircle(pos + poleDir * Mathf.Cos(curTheta * Mathf.Deg2Rad) * radius, poleDir, Mathf.Sin(curTheta * Mathf.Deg2Rad) * radius, AnchorGizmoColor, quality: 16, depthTest: true);
      }
    }

    #endregion

    #region Unity Events (Internal)

    [SerializeField]
    private EnumEventTable _eventTable = null;

    public enum EventType {
      OnAnchorPreferred = 100,
      OnAnchorNotPreferred = 110,
      WhileAnchorPreferred = 120,
      OnAnchorablesAttached = 130,
      OnNoAnchorablesAttached = 140,
      WhileAnchorablesAttached = 150,
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

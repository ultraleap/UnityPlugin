/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Recording {

  public class OnUnityCallback : MonoBehaviour {

    [SerializeField]
    private EnumEventTable _table;

    private void Awake() {
      _table.Invoke((int)CallbackType.Awake);
    }

    private void Start() {
      _table.Invoke((int)CallbackType.Start);
    }

    private void OnEnable() {
      _table.Invoke((int)CallbackType.OnEnable);
    }

    private void OnDisable() {
      _table.Invoke((int)CallbackType.OnDisable);
    }

    private void OnDestroy() {
      _table.Invoke((int)CallbackType.OnDestroy);
    }

    private void FixedUpdate() {
      _table.Invoke((int)CallbackType.FixedUpdate);
    }

    private void Update() {
      _table.Invoke((int)CallbackType.Update);
    }

    private void LateUpdate() {
      _table.Invoke((int)CallbackType.LateUpdate);
    }

    public void GenericCallback() {
      _table.Invoke((int)CallbackType.GenericCallback);
    }

    public enum CallbackType {
      Awake,
      Start,
      OnEnable,
      OnDisable,
      OnDestroy,
      FixedUpdate,
      Update,
      LateUpdate,
      GenericCallback
    }
  }
}

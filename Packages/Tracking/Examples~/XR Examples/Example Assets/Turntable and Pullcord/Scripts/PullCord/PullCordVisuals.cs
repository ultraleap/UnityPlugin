/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Examples
{
    /// <summary>
    /// Updates the pull cord visuals depending on the pull cord state
    /// </summary>
    public class PullCordVisuals : MonoBehaviour
    {
        [SerializeField] private LineRenderer _pullCordLine;
        [SerializeField] private PullCordHandle _pullCordHandle;
        [SerializeField] private Transform _hint;

        [SerializeField, Tooltip("These gameobjects will be disabled, while the handle is not pinched")]
        private List<GameObject> _objectsToDisable;

        private MeshRenderer _handleRenderer;
        private Color initialHandleColor, initialLineColor;
        private Vector3 initialHandleScale;

        private Color newHandleColor, newLineColor;
        private Vector3 newHandleScale;

        private bool hintDisabled, hintShouldBeDisabled;

        private void OnEnable()
        {
            _handleRenderer = _pullCordHandle.GetComponentInChildren<MeshRenderer>(true);

            initialHandleColor = _handleRenderer.material.color;
            initialHandleScale = _pullCordHandle.transform.localScale;
            initialLineColor = _pullCordLine.material.color;

            newHandleColor = initialHandleColor;
            newHandleScale = initialHandleScale;
            newLineColor = initialLineColor;

            if (_pullCordHandle != null)
            {
                _pullCordHandle.OnStateChanged.AddListener(OnStateChanged);
            }
        }

        private void OnDestroy()
        {
            if (_pullCordHandle != null)
            {
                _pullCordHandle.OnStateChanged.RemoveListener(OnStateChanged);
            }
        }

        private void Update()
        {
            _pullCordLine.material.color = Color.Lerp(_pullCordLine.material.color, newLineColor, Time.deltaTime * 10f);
            _handleRenderer.material.color = Color.Lerp(_handleRenderer.material.color, newHandleColor, Time.deltaTime * 10f);
            _pullCordHandle.transform.localScale = Vector3.Lerp(_pullCordHandle.transform.localScale, newHandleScale, Time.deltaTime * 10f);

            if (!hintDisabled && hintShouldBeDisabled)
            {
                _hint.localScale *= 0.9f;
                if (_hint.localScale.x < 0.01f)
                {
                    _hint.gameObject.SetActive(false);
                    hintDisabled = true;
                }
            }
        }

        private void OnStateChanged(PullCordHandle.PullCordState state)
        {
            switch (state)
            {
                case PullCordHandle.PullCordState.Inactive:
                    newHandleScale = Vector3.zero;
                    _pullCordLine.enabled = false;
                    ToggleObjects(false);
                    break;

                case PullCordHandle.PullCordState.Default:
                    newHandleScale = initialHandleScale;
                    newHandleColor = initialHandleColor;
                    newLineColor = initialLineColor;
                    _pullCordLine.enabled = true;
                    ToggleObjects(false);
                    break;

                case PullCordHandle.PullCordState.Hovered:
                    newHandleScale = initialHandleScale * 1.3f;
                    Color half = Color.Lerp(initialLineColor, Color.white, 0.5f);
                    newHandleColor = half;
                    newLineColor = half;
                    _pullCordLine.enabled = true;
                    ToggleObjects(false);
                    break;

                case PullCordHandle.PullCordState.Pinched:
                    newHandleScale = initialHandleScale;
                    newHandleColor = Color.white;
                    newLineColor = Color.white;
                    _pullCordLine.enabled = true;
                    hintShouldBeDisabled = true;
                    ToggleObjects(true);
                    break;
            }
        }

        private void ToggleObjects(bool active)
        {
            // show / hide objects that should be disabled
            foreach (GameObject go in _objectsToDisable)
            {
                go.SetActive(active);
            }
        }
    }
}
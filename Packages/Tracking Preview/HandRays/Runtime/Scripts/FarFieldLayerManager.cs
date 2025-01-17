/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Preview.HandRays
{
    /// <summary>
    /// Assigns the correct layer to far field objects
    /// </summary>
    public class FarFieldLayerManager : MonoBehaviour
    {
        [Tooltip("If enabled, assigns the next available layer to FarFieldObjectLayer")]
        public bool AutomaticFarFieldObjectLayer = true;
        [Tooltip("The layer to be assigned to all far field objects")]
        public SingleLayer FarFieldObjectLayer;
        [Tooltip("The layer assigned to the floor")]
        public SingleLayer FloorLayer;

        private bool _layersGenerated = false;

        // Start is called before the first frame update
        void Awake()
        {
            GenerateLayers();
            AssignLayers();
        }

        protected void GenerateLayers()
        {
            if (_layersGenerated || !AutomaticFarFieldObjectLayer)
            {
                return;
            }

            FarFieldObjectLayer = -1;

            for (int i = 8; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(layerName))
                {
                    FarFieldObjectLayer = i;
                    break;
                }
            }

            if (FarFieldObjectLayer == -1)
            {
                if (Application.isPlaying)
                {
                    enabled = false;
                }
                Debug.LogError("Could not find enough free layers for "
                              + "auto-setup; manual setup is required.", this.gameObject);
                return;
            }
            _layersGenerated = true;
        }

        protected void AssignLayers()
        {
            FarFieldObject[] farFieldObjects = FindObjectsByType<FarFieldObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (FarFieldObject ffo in farFieldObjects)
            {
                ffo.gameObject.layer = FarFieldObjectLayer;
            }
        }
    }
}
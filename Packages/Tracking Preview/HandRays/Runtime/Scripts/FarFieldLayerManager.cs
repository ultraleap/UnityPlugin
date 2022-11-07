using Leap.Unity;
using Leap.Unity.Preview.HandRays;
using UnityEngine;

namespace Leap.Unity.Preview.HandRays
{
    public class FarFieldLayerManager : MonoBehaviour
    {
        public bool AutomaticFarFieldObjectLayer = true;
        public SingleLayer FarFieldObjectLayer;
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
            FarFieldObject[] farFieldObjects = FindObjectsOfType<FarFieldObject>(true);
            foreach (FarFieldObject ffo in farFieldObjects)
            {
                ffo.gameObject.layer = FarFieldObjectLayer;
            }
        }
    }
}
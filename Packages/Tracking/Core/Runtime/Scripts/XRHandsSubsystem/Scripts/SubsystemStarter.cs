using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace Leap.Unity
{
    public class SubsystemStarter : MonoBehaviour
    {
        XRHandSubsystem m_Subsystem = null;

        private void Awake()
        {
            var descriptors = new List<XRHandSubsystemDescriptor>();
            SubsystemManager.GetSubsystemDescriptors(descriptors);
            foreach (var descriptor in descriptors)
            {
                if (descriptor.id == "UL XR Hands")
                {
                    m_Subsystem = descriptor.Create();
                }

                if (m_Subsystem != null)
                {
                    m_Subsystem.Start();
                }
            }
        }

        private void OnDestroy()
        {
            if (m_Subsystem != null)
            {
                m_Subsystem.Stop();
            }
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.ProviderImplementation;

namespace Leap.Unity
{
    public class SubsystemStarter
    {
        static XRHandSubsystem m_Subsystem = null;
        static XRHandProviderUtility.SubsystemUpdater updater = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Awake()
        {
            Application.quitting += OnDestroy;

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
                    updater = new XRHandProviderUtility.SubsystemUpdater(m_Subsystem);
                    updater.Start();
                }
            }

        }
        
        private static void OnDestroy()
        {
            if (m_Subsystem != null)
            {
                m_Subsystem.Stop();
                updater.Stop();
                m_Subsystem.Destroy();
            }
        }
    }
}
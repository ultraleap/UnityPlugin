using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.ProviderImplementation;
using UnityEngine.SubsystemsImplementation.Extensions;

namespace Leap.Unity
{
    public class SubsystemStarter
    {
        static XRHandSubsystem m_Subsystem = null;
        static XRHandProviderUtility.SubsystemUpdater updater = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RunAfterSceneLoad()
        {
            Application.quitting += OnQuit;
            LeapProvider leapProvider = Hands.Provider;
            XRHandSubsystemProvider subsystemProvider = null;

            if(leapProvider == null)
            {
                GameObject leapProviderGO = new GameObject("LeapXRServiceProvider");
                leapProvider = (LeapProvider)leapProviderGO.AddComponent<LeapXRServiceProvider>();
                GameObject.DontDestroyOnLoad(leapProviderGO);
            }

            List<XRHandSubsystemDescriptor> descriptors = new List<XRHandSubsystemDescriptor>();
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
                    subsystemProvider = m_Subsystem.GetProvider();
                }

                if(subsystemProvider != null)
                {
                    LeapXRHandProvider leapXRHandProvider = (LeapXRHandProvider)subsystemProvider;
                    leapXRHandProvider.TrackingProvider = leapProvider;
                }
            }
        }
        
        private static void OnQuit()
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
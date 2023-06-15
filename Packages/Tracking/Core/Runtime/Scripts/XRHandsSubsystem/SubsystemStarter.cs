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
        static GameObject leapProviderGO = null;
        static XRHandSubsystemProvider subsystemProvider = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RunBeforeSceneLoad()
        {
            UltraleapSettings ultraleapSettings = UltraleapSettings.FindSettingsSO();

            if (ultraleapSettings == null)
            {
                Debug.Log("There is no Ultraleap Settings object in the package. Subsystem will not be used.");
                return;
            }

            if (ultraleapSettings.LeapSubsystemEnabled == false)
            {
                return;
            }

            UltraleapSettings.enableUltraleapSubsystem += RunBeforeSceneLoad;
            UltraleapSettings.disableUltraleapSubsystem += OnQuit;
            Application.quitting += OnQuit;            

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
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RunAfterSceneLoad()
        {
            UltraleapSettings ultraleapSettings = UltraleapSettings.FindSettingsSO();

            if (ultraleapSettings == null || ultraleapSettings.LeapSubsystemEnabled == false)
            {
                return;
            }

            LeapProvider leapProvider = Hands.Provider;

            if (leapProvider == null)
            {
                Debug.Log("There are no Leap Providers in the scene, automatically assigning one for use with Leap XRHands");
                leapProviderGO = new GameObject("LeapXRServiceProvider");
                leapProvider = (LeapProvider)leapProviderGO.AddComponent<LeapXRServiceProvider>();
                GameObject.DontDestroyOnLoad(leapProviderGO);
            }

            if (subsystemProvider != null)
            {
                LeapXRHandProvider leapXRHandProvider = (LeapXRHandProvider)subsystemProvider;
                leapXRHandProvider.TrackingProvider = leapProvider;
            }
        }

        private static void OnQuit()
        {
            if (m_Subsystem != null)
            {
                updater.Destroy();
                m_Subsystem.Destroy();
                GameObject.Destroy(leapProviderGO);
            }
            UltraleapSettings.disableUltraleapSubsystem -= OnQuit;
        }
    }
}
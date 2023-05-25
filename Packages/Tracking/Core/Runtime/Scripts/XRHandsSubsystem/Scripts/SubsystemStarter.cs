using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SubsystemsImplementation;
using UnityEngine.SubsystemsImplementation.Extensions;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.OpenXR;
using UnityEngine.XR.Hands.ProviderImplementation;
using UnityEngine.XR.Management;

namespace Leap.Unity
{
    public class SubsystemStarter : MonoBehaviour
    {
        XRHandSubsystem m_Subsystem = null;
        XRHandSubsystem m_BaseSubsystem = null;
        XRHandSubsystemProvider m_subsystemProvider = null;



        private void Awake()
        {
            var descriptors = new List<XRHandSubsystemDescriptor>();
            SubsystemManager.GetSubsystemDescriptors(descriptors);
            foreach (var descriptor in descriptors)
            {
                if (descriptor.id == "UL XR Hands")
                {
                    m_Subsystem = descriptor.Create();
                    var handsSubsystemCinfo = new XRHandSubsystemDescriptor.Cinfo
                    {
                        id = "UL XR Hands",
                        providerType = typeof(LeapXRHandProvider)
                    };
                    XRHandSubsystemDescriptor.Register(handsSubsystemCinfo);

                }


                if (m_Subsystem != null)
                {
                    m_Subsystem.Start();
                }
            }


            m_BaseSubsystem = XRGeneralSettings.Instance?
                .Manager?
                .activeLoader?
                .GetLoadedSubsystem<XRHandSubsystem>();
        }

        private void OnDestroy()
        {
            if (m_Subsystem != null)
            {
                m_Subsystem.Stop();
            }
        }

        private void Update()
        {
            if (m_Subsystem != null)
            {
                m_Subsystem.TryUpdateHands(XRHandSubsystem.UpdateType.BeforeRender);
                
            }
        }
    }
}
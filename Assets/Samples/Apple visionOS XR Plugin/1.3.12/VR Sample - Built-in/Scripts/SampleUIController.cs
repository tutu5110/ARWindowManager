using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.XR.VisionOS.Samples.Builtin
{
    public class SampleUIController : MonoBehaviour
    {
        const string k_DisableSkyboxText = "Disable Skybox";
        const string k_EnableSkyboxText = "Enable Skybox";

        const string k_EnableHDRText = "Enable HDR";
        const string k_DisableHDRText = "Disable HDR";

        const string k_EnableMSAAText = "Enable MSAA";
        const string k_DisableMSAAText = "Disable MSAA";

        const string k_HandTrackingAuthorizationFormat = "Hand Tracking Authorization: {0}";
        const string k_WorldSensingAuthorizationFormat = "World Sensing Authorization: {0}";

        [SerializeField]
        ParticleSystem m_ParticleSystem;

        [SerializeField]
        Camera m_Camera;

        [SerializeField]
        Text m_SkyboxToggleText;

        [SerializeField]
        Text m_HDRToggleText;

        [SerializeField]
        Text m_MSAAToggleText;

        [SerializeField]
        Text m_HandTrackingAuthorizationText;

        [SerializeField]
        Text m_WorldSensingAuthorizationText;

        [SerializeField]
        Button m_QuitButton;

        void Awake()
        {
            UpdateSkyboxToggleText();
            UpdateHDRText();

#if UNITY_VISIONOS || UNITY_EDITOR
            UpdateAuthorizationText();
#endif

#if UNITY_EDITOR
            // Disable quit button in Editor (play mode) because `Application.Quit` doesn't do anything in play mode
            m_QuitButton.interactable = false;
#endif
        }

#if UNITY_VISIONOS || UNITY_EDITOR
        void OnEnable()
        {
            VisionOS.AuthorizationChanged += OnAuthorizationChanged;
        }

        void OnDisable()
        {
            VisionOS.AuthorizationChanged += OnAuthorizationChanged;
        }

        void UpdateAuthorizationText()
        {
            var type = VisionOSAuthorizationType.HandTracking;
            var status = VisionOS.QueryAuthorizationStatus(type);
            OnAuthorizationChanged(new VisionOSAuthorizationEventArgs { type = type, status = status });

            type = VisionOSAuthorizationType.WorldSensing;
            status = VisionOS.QueryAuthorizationStatus(type);
            OnAuthorizationChanged(new VisionOSAuthorizationEventArgs { type = type, status = status });
        }

        void OnAuthorizationChanged(VisionOSAuthorizationEventArgs args)
        {
            switch (args.type)
            {
                case VisionOSAuthorizationType.HandTracking:
                    m_HandTrackingAuthorizationText.text = string.Format(k_HandTrackingAuthorizationFormat, args.status);
                    break;
                case VisionOSAuthorizationType.WorldSensing:
                    m_WorldSensingAuthorizationText.text = string.Format(k_WorldSensingAuthorizationFormat, args.status);
                    break;
                // We do not support CameraAccess yet so ignore it
            }
        }
#endif

        public void SetParticleStartSpeed(float speed)
        {
            var mainModule = m_ParticleSystem.main;
            mainModule.simulationSpeed = speed;
        }

        public void ToggleSkybox()
        {
            if (m_Camera.clearFlags == CameraClearFlags.Skybox)
            {
                m_Camera.clearFlags = CameraClearFlags.Color;
                m_Camera.backgroundColor = Color.black;
            }
            else
            {
                m_Camera.clearFlags = CameraClearFlags.Skybox;
            }

            UpdateSkyboxToggleText();
        }

        void UpdateSkyboxToggleText()
        {
            m_SkyboxToggleText.text = m_Camera.clearFlags == CameraClearFlags.Skybox ? k_DisableSkyboxText : k_EnableSkyboxText;
        }

        // TODO: LXR-4049 toggling HDR at runtime results in depth/transparency artifacts. Disabled for now...
        public void ToggleHDR()
        {
            m_Camera.allowHDR = !m_Camera.allowHDR;
            UpdateHDRText();
        }

        void UpdateHDRText()
        {
            m_HDRToggleText.text = m_Camera.allowHDR ? k_DisableHDRText : k_EnableHDRText;
        }

        public void ToggleMSAA()
        {
            m_Camera.allowMSAA = !m_Camera.allowMSAA;
            UpdateMSAAText();
        }

        void UpdateMSAAText()
        {
            m_MSAAToggleText.text = m_Camera.allowMSAA ? k_DisableMSAAText : k_EnableMSAAText;
        }

        public void OnQuit()
        {
            Application.Quit();
        }
    }
}

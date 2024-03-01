using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Content.UI.Layout;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;

public class GazeTooltips : MonoBehaviour
{
    [SerializeField]
    XROrigin m_XROrigin;

    [SerializeField]
    Transform m_Tooltip;

    [SerializeField]
    bool m_TapTooltip = true;

    Transform m_XRCameraTransform;
    LayerMask m_PlaneMask;
    ZoneScale m_ZoneScale;
    GameObject m_LastPlane = null;
    readonly List<ARContactSpawnTrigger> m_SpawnerContacts = new();

    const float k_SphereCastRadius = 0.1f;
    const string k_PlaneLayer = "Placeable Surface";

    void Start()
    {
        if (m_XROrigin == null)
            m_XROrigin = FindObjectOfType<XROrigin>();

        m_XRCameraTransform = m_XROrigin.Camera.transform;
        m_XROrigin.GetComponentsInChildren(true, m_SpawnerContacts);
        m_ZoneScale = GetComponentInChildren<ZoneScale>(true);
        m_PlaneMask = LayerMask.GetMask(k_PlaneLayer);
    }

    void OnEnable()
    {
        m_Tooltip.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        PlaceTooltip();
    }

    void PlaceTooltip()
    {
        RaycastHit hitInfo;
        if (Physics.SphereCast(new Ray(m_XRCameraTransform.position, m_XRCameraTransform.forward), k_SphereCastRadius, out hitInfo, float.MaxValue, m_PlaneMask))
        {
            var spawnSurfaceFound = false;
            Vector3 surfacePosition = default;

            // plane tap tooltip
            if (m_TapTooltip)
            {
                foreach (var spawnerContact in m_SpawnerContacts)
                {
                    if (spawnerContact.isActiveAndEnabled && spawnerContact.TryGetSpawnSurfaceData(hitInfo.collider, out surfacePosition, out _))
                    {
                        spawnSurfaceFound = true;
                        break;
                    }
                }
            }

            if (spawnSurfaceFound)
            {
                m_Tooltip.position = surfacePosition;
                var facePosition = m_XRCameraTransform.position;
                var forward = facePosition - m_Tooltip.position;
                var targetRotation = forward.sqrMagnitude > float.Epsilon ? Quaternion.LookRotation(forward, Vector3.up) : Quaternion.identity;
                targetRotation *= Quaternion.Euler(new Vector3(0f, 180f, 0f));
                var targetEuler = targetRotation.eulerAngles;
                var currentEuler = m_Tooltip.rotation.eulerAngles;
                targetRotation = Quaternion.Euler
                (
                    currentEuler.x,
                    targetEuler.y,
                    currentEuler.z
                );

                m_Tooltip.rotation = targetRotation;

                if (!m_Tooltip.gameObject.activeSelf)
                    m_Tooltip.gameObject.SetActive(true);

                if (m_LastPlane != hitInfo.transform.gameObject)
                    m_ZoneScale.Snap();

                m_LastPlane = hitInfo.transform.gameObject;
            }
            else
            {
                if (m_Tooltip.gameObject.activeSelf)
                {
                    m_Tooltip.gameObject.SetActive(false);
                }
            }
        }
    }
}

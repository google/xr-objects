using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Controls the visual states of a boolean toggle switch UI
/// </summary>
[RequireComponent(typeof(Toggle))]
public class BooleanToggleVisualsController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    const float k_TargetPositionX = 17f;

#pragma warning disable 649
    [SerializeField, Tooltip("The boolean toggle knob.")]
    RectTransform m_Knob;

    [SerializeField, Tooltip("How much to translate the button imagery on the z on hover.")]
    float m_ZTranslation = 5f;
#pragma warning restore 649

    Toggle m_Toggle;
    float m_InitialBackground;
    Coroutine m_ColorFade;
    Coroutine m_LocalMove;

    void Awake()
    {
        m_Toggle = gameObject.GetComponent<Toggle>();

        //Add listener for when the state of the Toggle changes, to take action
        m_Toggle.onValueChanged.AddListener(ToggleValueChanged);

        if (m_Knob != null)
        {
            m_InitialBackground = m_Knob.localPosition.z;
        }
    }

    void OnEnable()
    {
        ToggleValueChanged(m_Toggle.isOn);
    }

    /// <inheritdoc />
    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        PerformEntranceActions();
    }

    /// <inheritdoc />
    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        PerformExitActions();
    }

    void ToggleValueChanged(bool value)
    {
        if (value)
        {
            m_Knob.localPosition = new Vector3(k_TargetPositionX, m_Knob.localPosition.y, m_Knob.localPosition.z);
        }
        else
        {
            m_Knob.localPosition = new Vector3(-k_TargetPositionX, m_Knob.localPosition.y, m_Knob.localPosition.z);
        }
    }

    void PerformEntranceActions()
    {
        if (m_Knob != null)
        {
            var backgroundLocalPosition = m_Knob.localPosition;
            backgroundLocalPosition.z = m_InitialBackground - m_ZTranslation;
            m_Knob.localPosition = backgroundLocalPosition;
        }
    }

    void PerformExitActions()
    {
        if (m_Knob != null)
        {
            var backgroundLocalPosition = m_Knob.localPosition;
            backgroundLocalPosition.z = m_InitialBackground;
            m_Knob.localPosition = backgroundLocalPosition;
            m_Knob.localScale = Vector3.one;
        }
    }
}

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PinchVizController : MonoBehaviour
{
    [SerializeField]
    SkinnedMeshRenderer m_Pointer;

    XRRayInteractor m_Interactor;

    // Start is called before the first frame update
    void Start()
    {
        m_Interactor = this.GetComponent<XRRayInteractor>();
    }

    // Update is called once per frame
    void Update()
    {
        var inputValue = Mathf.Max(m_Interactor.xrController.selectInteractionState.value, m_Interactor.xrController.uiPressInteractionState.value);
        m_Pointer.SetBlendShapeWeight(0, inputValue * 100f);
    }
}

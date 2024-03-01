using System.Collections;
using UnityEngine;

public class PositionAtStart : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The starting target.")]
    Transform m_Target;

    [SerializeField]
    [Tooltip("Adjusts the follow point from the target by this amount.")]
    Vector3 m_TargetOffset = Vector3.forward;

    bool m_IgnoreX = true;
    Vector3 m_TargetPosition;

    private const float k_StartDelay = 2.0f;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        yield return new WaitForSeconds(k_StartDelay);
        var targetRotation = m_Target.rotation;
        var newTransform = m_Target;
        var targetEuler = targetRotation.eulerAngles;
        targetRotation = Quaternion.Euler
        (
            m_IgnoreX ? 0f : targetEuler.x,
            targetEuler.y,
            targetEuler.z
        );

        newTransform.rotation = targetRotation;
        m_TargetPosition = m_Target.position + newTransform.TransformVector(m_TargetOffset);
        transform.position = m_TargetPosition;
    }
}

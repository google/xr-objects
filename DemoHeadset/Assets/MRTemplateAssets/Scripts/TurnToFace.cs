using System.Collections.Generic;

namespace UnityEngine.XR.Content.UI.Layout
{
    /// <summary>
    /// Makes this object face a target smoothly and along specific axes
    /// </summary>
    public class TurnToFace : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField, Tooltip("Target to face towards. If not set, this will default to the main camera")]
        Transform m_FaceTarget;

        [SerializeField, Tooltip("Speed to turn")]
        float m_TurnToFaceSpeed = 5f;

        [SerializeField, Tooltip("Local rotation offset")]
        Vector3 m_RotationOffset = Vector3.zero;

        [SerializeField, Tooltip("If enabled, ignore the x axis when rotating")]
        bool m_IgnoreX;

        [SerializeField, Tooltip("If enabled, ignore the y axis when rotating")]
        bool m_IgnoreY;

        [SerializeField, Tooltip("If enabled, ignore the z axis when rotating")]
        bool m_IgnoreZ;
#pragma warning restore 649

        static readonly HashSet<TurnToFace> k_EnabledInstances = new HashSet<TurnToFace>();

        void OnEnable()
        {
            transform.rotation = GetTargetRotation(transform.position);
            k_EnabledInstances.Add(this);

        }

        void OnDisable()
        {
            k_EnabledInstances.Remove(this);
        }

        void Update()
        {
            LookAtTarget();
        }

        /// <summary>
        /// Causes all turn to face layout components to snap to the correct rotation immediately.
        /// </summary>
        public static void SnapAll()
        {
            foreach (var turnToFace in k_EnabledInstances)
            {
                var instanceTransform = turnToFace.transform;
                instanceTransform.rotation = turnToFace.GetTargetRotation(instanceTransform.position);
            }
        }

        /// <summary>
        /// Get the current easing lerp amount that will be used for rotation.
        /// </summary>
        /// <returns>The easing value based on current delta time.</returns>
        public float GetCurrentRotationEase()
        {
            return 1f - Mathf.Exp(-m_TurnToFaceSpeed * Time.unscaledDeltaTime);
        }

        /// <summary>
        /// Ease the rotation towards the target rotation
        /// </summary>
        public void LookAtTarget()
        {
            var thisTransform = transform;
            var targetRotation = GetTargetRotation(thisTransform.position);
            var ease = GetCurrentRotationEase();
            thisTransform.rotation = Quaternion.Slerp(thisTransform.rotation, targetRotation, ease);
        }

        void SetDefaultTargetIfNeeded()
        {
            // Default to main camera
            if (m_FaceTarget == null)
            {
                var mainCamera = Camera.main;
                m_FaceTarget = mainCamera == null ? null : mainCamera.transform;
            }
        }

        /// <summary>
        /// Get the target rotation to use for a given position
        /// </summary>
        /// <param name="position">The position to compare to the face target when calculating the look direction.</param>
        /// <returns>The rotation that faces towards the target from the given position.</returns>
        public Quaternion GetTargetRotation(Vector3 position)
        {
            SetDefaultTargetIfNeeded();

            if (m_FaceTarget == null)
                return Quaternion.identity;

            var facePosition = m_FaceTarget.position;
            var forward = facePosition - position;
            var targetRotation = forward.sqrMagnitude > float.Epsilon ? Quaternion.LookRotation(forward, Vector3.up) : Quaternion.identity;
            targetRotation *= Quaternion.Euler(m_RotationOffset);
            if (m_IgnoreX || m_IgnoreY || m_IgnoreZ)
            {
                var targetEuler = targetRotation.eulerAngles;
                var currentEuler = transform.rotation.eulerAngles;
                targetRotation = Quaternion.Euler
                (
                    m_IgnoreX ? currentEuler.x : targetEuler.x,
                    m_IgnoreY ? currentEuler.y : targetEuler.y,
                    m_IgnoreZ ? currentEuler.z : targetEuler.z
                );
            }

            return targetRotation;
        }
    }
}

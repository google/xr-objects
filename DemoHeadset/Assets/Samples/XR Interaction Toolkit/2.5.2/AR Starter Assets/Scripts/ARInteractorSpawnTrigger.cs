#if AR_FOUNDATION_PRESENT
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Utilities.Internal;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets
{
    /// <summary>
    /// Spawns an object at an <see cref="IARInteractor"/>'s raycast hit position when a trigger is activated.
    /// </summary>
    public class ARInteractorSpawnTrigger : MonoBehaviour
    {
        /// <summary>
        /// The type of trigger to use to spawn an object.
        /// </summary>
        public enum SpawnTriggerType
        {
            /// <summary>
            /// Spawn an object when the <see cref="XRBaseController"/> associated with the interactor activates its selection
            /// state but no selection actually occurs.
            /// </summary>
            SelectAttempt,

            /// <summary>
            /// Spawn an object when an <see cref="UnityEngine.InputSystem.InputAction"/> is performed.
            /// </summary>
            InputAction,
        }

        [SerializeField]
        [RequireInterface(typeof(IARInteractor))]
        [Tooltip("The AR Interactor that determines where to spawn the object.")]
        Object m_ARInteractorObject;

        /// <summary>
        /// The <see cref="IARInteractor"/> that determines where to spawn the object.
        /// </summary>
        public Object arInteractorObject
        {
            get => m_ARInteractorObject;
            set => m_ARInteractorObject = value;
        }

        [SerializeField]
        [Tooltip("The behavior to use to spawn objects.")]
        ObjectSpawner m_ObjectSpawner;

        /// <summary>
        /// The behavior to use to spawn objects.
        /// </summary>
        public ObjectSpawner objectSpawner
        {
            get => m_ObjectSpawner;
            set => m_ObjectSpawner = value;
        }

        [SerializeField]
        [Tooltip("Whether to require that the AR Interactor hits an AR Plane with a horizontal up alignment in order to spawn anything.")]
        bool m_RequireHorizontalUpSurface;

        /// <summary>
        /// Whether to require that the <see cref="IARInteractor"/> hits an <see cref="ARPlane"/> with an alignment of
        /// <see cref="PlaneAlignment.HorizontalUp"/> in order to spawn anything.
        /// </summary>
        public bool requireHorizontalUpSurface
        {
            get => m_RequireHorizontalUpSurface;
            set => m_RequireHorizontalUpSurface = value;
        }

        [SerializeField]
        [Tooltip("The type of trigger to use to spawn an object, either when the Interactor's select action occurs or " +
            "when a button input is performed.")]
        SpawnTriggerType m_SpawnTriggerType;

        /// <summary>
        /// The type of trigger to use to spawn an object.
        /// </summary>
        public SpawnTriggerType spawnTriggerType
        {
            get => m_SpawnTriggerType;
            set => m_SpawnTriggerType = value;
        }

        [SerializeField]
        [Tooltip("The action to use to trigger spawn. (Button Control)")]
        InputActionProperty m_SpawnAction = new(new InputAction(type: InputActionType.Button));

        /// <summary>
        /// The Input System action to use to trigger spawn, if <see cref="spawnTriggerType"/> is set to <see cref="SpawnTriggerType.InputAction"/>.
        /// Must be an action with a button-like interaction where phase equals performed when pressed.
        /// Typically a <see cref="UnityEngine.InputSystem.Controls.ButtonControl"/> Control or a Value type action with a Press or Sector interaction.
        /// </summary>
        public InputActionProperty spawnAction
        {
            get => m_SpawnAction;
            set
            {
                if (Application.isPlaying)
                    m_SpawnAction.DisableDirectAction();

                m_SpawnAction = value;

                if (Application.isPlaying && isActiveAndEnabled)
                    m_SpawnAction.EnableDirectAction();
            }
        }

        IARInteractor m_ARInteractor;
        XRBaseControllerInteractor m_ARInteractorAsControllerInteractor;
        bool m_EverHadSelection;

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        void Start()
        {
            if (m_ObjectSpawner == null)
#if UNITY_2023_1_OR_NEWER
                m_ObjectSpawner = FindAnyObjectByType<ObjectSpawner>();
#else
                m_ObjectSpawner = FindObjectOfType<ObjectSpawner>();
#endif    

            m_ARInteractor = m_ARInteractorObject as IARInteractor;
            m_ARInteractorAsControllerInteractor = m_ARInteractorObject as XRBaseControllerInteractor;
            if (m_SpawnTriggerType == SpawnTriggerType.SelectAttempt && m_ARInteractorAsControllerInteractor == null)
            {
                Debug.LogError("Can only use SelectAttempt spawn trigger type with XRBaseControllerInteractor.", this);
                enabled = false;
            }
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        void OnEnable()
        {
            m_SpawnAction.EnableDirectAction();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        void OnDisable()
        {
            m_SpawnAction.DisableDirectAction();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        void Update()
        {
            var attemptSpawn = false;
            switch (m_SpawnTriggerType)
            {
                case SpawnTriggerType.SelectAttempt:
                    var currentControllerState = m_ARInteractorAsControllerInteractor.xrController.currentControllerState;
                    if (currentControllerState.selectInteractionState.activatedThisFrame)
                        m_EverHadSelection = m_ARInteractorAsControllerInteractor.hasSelection;
                    else if (currentControllerState.selectInteractionState.active)
                        m_EverHadSelection |= m_ARInteractorAsControllerInteractor.hasSelection;
                    else if (currentControllerState.selectInteractionState.deactivatedThisFrame)
                        attemptSpawn = !m_ARInteractorAsControllerInteractor.hasSelection && !m_EverHadSelection;
                    break;

                case SpawnTriggerType.InputAction:
                    if (m_SpawnAction.action.WasPerformedThisFrame())
                        attemptSpawn = true;
                    break;
            }

            if (attemptSpawn && m_ARInteractor.TryGetCurrentARRaycastHit(out var arRaycastHit))
            {
                var arPlane = arRaycastHit.trackable as ARPlane;
                if (arPlane == null)
                    return;

                if (m_RequireHorizontalUpSurface && arPlane.alignment != PlaneAlignment.HorizontalUp)
                    return;

                m_ObjectSpawner.TrySpawnObject(arRaycastHit.pose.position, arPlane.normal);
            }
        }
    }
}
#endif
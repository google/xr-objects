using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets
{
    /// <summary>
    /// Handles dismissing the object menu when clicking out the UI bounds, and showing the
    /// menu again when the create menu button is clicked after dismissal. Manages object deletion in the AR demo scene, 
    /// and also handles the toggling between the object creation menu button and the delete button.
    /// </summary>
    public class ARSampleMenuManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Button that opens the create menu.")]
        Button m_CreateButton;

        /// <summary>
        /// Button that opens the create menu.
        /// </summary>
        public Button createButton
        {
            get => m_CreateButton;
            set => m_CreateButton = value;
        }

        [SerializeField]
        [Tooltip("Button that deletes a selected object.")]
        Button m_DeleteButton;

        /// <summary>
        /// Button that deletes a selected object.
        /// </summary>
        public Button deleteButton
        {
            get => m_DeleteButton;
            set => m_DeleteButton = value;
        }

        [SerializeField]
        [Tooltip("The menu with all the creatable objects.")]
        GameObject m_ObjectMenu;

        /// <summary>
        /// The menu with all the creatable objects.
        /// </summary>
        public GameObject objectMenu
        {
            get => m_ObjectMenu;
            set => m_ObjectMenu = value;
        }

        [SerializeField]
        [Tooltip("The animator for the object creation menu.")]
        Animator m_ObjectMenuAnimator;

        /// <summary>
        /// The animator for the object creation menu.
        /// </summary>
        public Animator objectMenuAnimator
        {
            get => m_ObjectMenuAnimator;
            set => m_ObjectMenuAnimator = value;
        }

        [SerializeField]
        [Tooltip("The object spawner component in charge of spawning new objects.")]
        ObjectSpawner m_ObjectSpawner;

        /// <summary>
        /// The object spawner component in charge of spawning new objects.
        /// </summary>
        public ObjectSpawner objectSpawner
        {
            get => m_ObjectSpawner;
            set => m_ObjectSpawner = value;
        }

        [SerializeField]
        [Tooltip("Button that closes the object creation menu.")]
        Button m_CancelButton;

        /// <summary>
        /// Button that closes the object creation menu.
        /// </summary>
        public Button cancelButton
        {
            get => m_CancelButton;
            set => m_CancelButton = value;
        }

        [SerializeField]
        [Tooltip("The screen space controller associated with the demo scene.")]
        XRScreenSpaceController m_ScreenSpaceController;

        /// <summary>
        /// The screen space controller associated with the demo scene.
        /// </summary>
        public XRScreenSpaceController screenSpaceController
        {
            get => m_ScreenSpaceController;
            set => m_ScreenSpaceController = value;
        }

        [SerializeField]
        [Tooltip("The interaction group for the AR demo scene.")]
        XRInteractionGroup m_InteractionGroup;

        /// <summary>
        /// The interaction group for the AR demo scene.
        /// </summary>
        public XRInteractionGroup interactionGroup
        {
            get => m_InteractionGroup;
            set => m_InteractionGroup = value;
        }

        bool m_IsPointerOverUI;
        bool m_ShowObjectMenu;

        void OnEnable()
        {
            m_ScreenSpaceController.dragCurrentPositionAction.action.started += HideTapOutsideUI;
            m_ScreenSpaceController.tapStartPositionAction.action.started += HideTapOutsideUI;
            m_CreateButton.onClick.AddListener(ShowMenu);
            m_CancelButton.onClick.AddListener(HideMenu);
            m_DeleteButton.onClick.AddListener(DeleteFocusedObject);
        }

        void OnDisable()
        {
            m_ShowObjectMenu = false;
            m_ScreenSpaceController.dragCurrentPositionAction.action.started -= HideTapOutsideUI;
            m_ScreenSpaceController.tapStartPositionAction.action.started -= HideTapOutsideUI;
            m_CreateButton.onClick.RemoveListener(ShowMenu);
            m_CancelButton.onClick.RemoveListener(HideMenu);
            m_DeleteButton.onClick.RemoveListener(DeleteFocusedObject);
        }

        void Start()
        {
            HideMenu();
        }

        void Update()
        {
            if (m_ShowObjectMenu)
            {
                m_CreateButton.gameObject.SetActive(false);
                m_DeleteButton.gameObject.SetActive(false);
                m_IsPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(-1);
            }
            else
            {
                m_IsPointerOverUI = false;
                if (m_InteractionGroup?.focusInteractable != null && (!m_DeleteButton.isActiveAndEnabled || m_CreateButton.isActiveAndEnabled))
                {
                    m_CreateButton.gameObject.SetActive(false);
                    m_DeleteButton.gameObject.SetActive(true);
                }
                else if (m_InteractionGroup?.focusInteractable == null && (!m_CreateButton.isActiveAndEnabled || m_DeleteButton.isActiveAndEnabled))
                {
                    m_CreateButton.gameObject.SetActive(true);
                    m_DeleteButton.gameObject.SetActive(false);
                }
            }
        }

        public void SetObjectToSpawn(int objectIndex)
        {
            if (m_ObjectSpawner == null)
            {
                Debug.LogWarning("Object Spawner not configured correctly: no ObjectSpawner set.");
            }
            else
            {
                if (m_ObjectSpawner.objectPrefabs.Count > objectIndex)
                {
                    m_ObjectSpawner.spawnOptionIndex = objectIndex;
                }
                else
                {
                    Debug.LogWarning("Object Spawner not configured correctly: object index larger than number of Object Prefabs.");
                }
            }

            HideMenu();
        }

        void ShowMenu()
        {
            m_ShowObjectMenu = true;
            m_ObjectMenu.SetActive(true);
            if (!m_ObjectMenuAnimator.GetBool("Show"))
            {
                m_ObjectMenuAnimator.SetBool("Show", true);
            }
        }

        /// <summary>
        /// Triggers hide animation for menu.
        /// </summary>
        public void HideMenu()
        {
            m_ObjectMenuAnimator.SetBool("Show", false);
            m_ShowObjectMenu = false;
        }

        void HideTapOutsideUI(InputAction.CallbackContext context)
        {
            if (!m_IsPointerOverUI)
            {
                HideMenu();
            }
        }

        void DeleteFocusedObject()
        {
            var currentFocusedObject = m_InteractionGroup.focusInteractable;
            if (currentFocusedObject != null)
            {
                Destroy(currentFocusedObject.transform.gameObject);
            }
        }
    }
}

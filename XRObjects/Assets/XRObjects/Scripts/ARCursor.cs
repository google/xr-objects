// Copyright 2024 Google LLC

// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;

using UnityEngine.UI;
using TMPro;
using Mediapipe.Unity;
using Mediapipe;
using UnityEngine.Device;
using PimDeWitte.UnityMainThreadDispatcher;
using Mediapipe.Unity.CoordinateSystem;

// Class for raycasting GameObjects onto the locations where a real-world object has been detected

public class ARCursor : MonoBehaviour
{
  public GameObject cursorChildObject;
  public GameObject objectToPlace;
  public ARRaycastManager raycastManager;
  public ARPlaneManager m_ARPlaneManager;
  public ARCameraManager m_ARCameraManager;

  public bool useCursor = true;
  public Button buttonCursor;

  [HideInInspector] public bool setupInProgress = true;
  public Button buttonPlanes;

  public bool mediaPipeActive = true;
  public Button buttonMediaPipe;

  [HideInInspector] public bool showMediaPipeBoundingBoxes = false;
  public Button buttonDeleteObjects;
  public Button buttonSpeechPanel;

  [HideInInspector] public bool showSpeechPanel = false;
  public GameObject speechPanel;

  [HideInInspector] public bool showUI = true;
  public Button buttonShowUI;
  public GameObject gameObjectUI, gameObjectCameraPreview;

  // list for storing the spawned gameobjects (object boundaries/containers)
  private List<GameObject> spawnedContainersList = new List<GameObject>();

  private List<Detection> detectedObjectList = new List<Detection>();
  private List<Detection> detectedObjectListToAddToScene = new List<Detection>();

  // create a list for spawned+placed detected objects
  private List<string> spawnedObjectNamesInScene = new List<string>();
  [SerializeField] private List<string> allowedObjectLabels = new List<string> { "bottle", "cup", "bowl", "cell phone", "laptop", "mouse", "vase", "potted plant", "apple", "orange", "backpack", "handbag" };

  // for showing the detected object's name as text
  public TextMeshProUGUI objectText;
  private DetectionListAnnotationController _outputDetectionsAnnotationController;

  // GUI elements for detected object label
  GUIContent objectTextContent;
  GUIStyle objectTextStyle = new GUIStyle();

  public RawImage RawImageCameraPreview;

  private bool IsPointerOverUIObject()
  {
    if (EventSystem.current.IsPointerOverGameObject())
      return true;

    for (int touchIndex = 0; touchIndex < Input.touchCount; touchIndex++)
    {
      Touch touch = Input.GetTouch(touchIndex);
      if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
        return true;
    }

    return false;
  }

  void Start()
  {
    cursorChildObject.SetActive(useCursor);

    m_ARPlaneManager = FindObjectOfType<ARPlaneManager>();

    // add callbacks to the buttons

    // buttonCursorButton -> this is actually for showing MediaPipe bounding boxes
    Button buttonCursorButton = buttonCursor.GetComponent<Button>();
    buttonCursorButton.onClick.AddListener(buttonCursorCallback);

    Button buttonPlanesButton = buttonPlanes.GetComponent<Button>();
    buttonPlanesButton.onClick.AddListener(buttonPlanesCallback);

    Button buttonMediaPipeButton = buttonMediaPipe.GetComponent<Button>();
    buttonMediaPipeButton.onClick.AddListener(buttonMediaPipeCallback);

    Button buttonShowUIButton = buttonShowUI.GetComponent<Button>();
    buttonShowUIButton.onClick.AddListener(buttonShowUICallback);

    Button buttonDeleteObjectsButton = buttonDeleteObjects.GetComponent<Button>();
    buttonDeleteObjectsButton.onClick.AddListener(buttonDeleteObjectsCallback);

    Button buttonSpeechPanelButton = buttonSpeechPanel.GetComponent<Button>();
    buttonSpeechPanelButton.onClick.AddListener(buttonSpeechPanelCallback);

    // turn off MediaPipe in the beginning
    mediaPipeActive = false;
    m_ARCameraManager.GetComponent<ARMPObjectDetection>().enabled = false;

    // hide the speech panel at start
    speechPanel.SetActive(false);

  }

  Texture2D CropTexture(Texture2D sourceTexture, UnityEngine.Rect cropRectTransform)
  {
    var cropRect = new RectInt
    (//0, 0, 500, 500
        Mathf.FloorToInt(cropRectTransform.x),
        Mathf.FloorToInt(cropRectTransform.y),
        Mathf.FloorToInt(cropRectTransform.width),
        Mathf.FloorToInt(cropRectTransform.height)
    );
    Debug.Log("ScreenCapturedTexture 1.5a " + cropRect.x + " " + cropRect.y + " " + cropRect.width + " " + cropRect.height);
    // var newPixels = sourceTexture.GetPixels(cropRect.x, cropRect.y, cropRect.width, cropRect.height);

    var newPixels = sourceTexture.GetPixels(cropRect.x, cropRect.y, cropRect.width, cropRect.height);
    Debug.Log("ScreenCapturedTexture 1.5b ");
    var newTexture = new Texture2D(cropRect.width, cropRect.height);
    Debug.Log("ScreenCapturedTexture 1.5c ");
    newTexture.SetPixels(newPixels);

    Debug.Log("ScreenCapturedTexture 1.5d ");
    newTexture.Apply();
    Debug.Log("ScreenCapturedTexture 1.5e ");
    return newTexture;
  }

  void Update()
  {

    // check if GameObject proxies need to be placed into the scene
    if (mediaPipeActive)
    {


      // Debug.Log("D: OnGUI detected - size: " + detectedObjectList?.Count);

      if (detectedObjectListToAddToScene?.Count > 0)
      {
        // Debug.Log("D: OnGUI - the array is >0");

        // Debug.Log("there are objects");
        foreach (Detection detectedObject in detectedObjectListToAddToScene)
        {

          // check if the 2D bounding box corners are not too close to the camera image edges
          var thresholdX = 0.1;
          var thresholdY = 0.05;

          if (detectedObject.LocationData?.RelativeBoundingBox?.Xmin > thresholdX && (detectedObject.LocationData?.RelativeBoundingBox?.Xmin + detectedObject.LocationData?.RelativeBoundingBox?.Width) < (1 - thresholdX) &&
                 detectedObject.LocationData?.RelativeBoundingBox?.Ymin > thresholdY && (detectedObject.LocationData?.RelativeBoundingBox?.Ymin + detectedObject.LocationData?.RelativeBoundingBox?.Height) < (1 - thresholdY))
          {
            Debug.Log("2D detection bounding box is in optimal location");
          }
          else
          {
            // skip this object as it is too close to the screen edges
            continue;
          }

          // aim the mid-bottom point of the bounding box

          var raycastX = (float)(1 - detectedObject.LocationData?.RelativeBoundingBox?.Xmin);
          var raycastY = (float)(detectedObject.LocationData?.RelativeBoundingBox?.Ymin);

          // // offset for targeting bottom center point
          // raycastX -= (float)(detectedObject.LocationData?.RelativeBoundingBox?.Width) / 2;
          // raycastY += (float)(detectedObject.LocationData?.RelativeBoundingBox?.Height);

          // offset for targeting center-center point
          raycastX -= (float)(detectedObject.LocationData?.RelativeBoundingBox?.Width) / 2;
          raycastY += (float)(detectedObject.LocationData?.RelativeBoundingBox?.Height) / 2;

          // convert to screen size pixels
          raycastX *= UnityEngine.Screen.width;
          raycastY *= UnityEngine.Screen.height;


          var detectedObjectName = detectedObject.Label[0];
          // if (!spawnedObjectNamesInScene.Contains(detectedObjectName) && allowedObjectLabels.Contains(detectedObjectName))
          if (allowedObjectLabels.Contains(detectedObjectName))
          {
            // do a raycast
            List<ARRaycastHit> hits = new List<ARRaycastHit>();

            // subtract raycastY from UnityEngine.Screen.height to mirror the Y for conversion
            var screenPosition = new Vector2(raycastX, UnityEngine.Screen.height - raycastY);

            // raycastManager.Raycast(screenPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinBounds);
            raycastManager.Raycast(screenPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.Depth);

            // raycast to gameobjects using Physics.Raycast
            RaycastHit hitGameObjects;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(screenPosition), out hitGameObjects))
            {
              if (hitGameObjects.collider.tag == "RealObjectProxy" || hitGameObjects.collider.tag == "RealObjectSphere")
              {
                // a RealObjectProxy has already been spawned here, so skip
                continue;
              }

            }


            if (hits.Count > 0)
            {
              GameObject newContainer = GameObject.Instantiate(objectToPlace, hits[0].pose.position, hits[0].pose.rotation);
              // newContainer.transform.LookAt(Camera.main.transform);

              // let's take a screenshot and crop that object
              // one potential issue is if OnGUI renders black boxes on the objects
              GUI.enabled = false;
              setAllARPlanesActive(false);
              cursorChildObject.SetActive(false);
              // yield return new WaitForEndOfFrame();
              Texture2D ScreenCapturedTexture = ScreenCapture.CaptureScreenshotAsTexture();
              GUI.enabled = true;

              // cropWindow bottom-left
              var cropWindowX = (float)(1 - detectedObject.LocationData?.RelativeBoundingBox?.Xmin);
              var cropWindowY = (float)(1 - detectedObject.LocationData?.RelativeBoundingBox?.Ymin);

              // offset for targeting bottom-left point
              cropWindowX -= (float)(detectedObject.LocationData?.RelativeBoundingBox?.Width);
              cropWindowY -= (float)(detectedObject.LocationData?.RelativeBoundingBox?.Height);

              // convert to screen size pixels
              cropWindowX *= UnityEngine.Screen.width;
              cropWindowY *= UnityEngine.Screen.height;

              var cropWindowWidth = UnityEngine.Screen.width * (float)(detectedObject.LocationData?.RelativeBoundingBox?.Width);
              var cropWindowHeight = UnityEngine.Screen.height * (float)(detectedObject.LocationData?.RelativeBoundingBox?.Height);


              Debug.Log("ScreenCapturedTexture 1 ");

              // crop it
              var ScreenCapturedTextureCropped = CropTexture(ScreenCapturedTexture, new UnityEngine.Rect(cropWindowX, cropWindowY, cropWindowWidth, cropWindowHeight));

              Debug.Log("ScreenCapturedTexture 2");

              // transfer the imagetexture variable
              newContainer.GetComponent<ImageQuery>().Texture2DImageOfObject = ScreenCapturedTextureCropped;
              // run Image Query
              Debug.Log("HTTPS - RunImageQuery 0");
              //newContainer.GetComponent<ImageQuery>().RunInitialImageQuery();
              StartCoroutine(newContainer.GetComponent<ImageQuery>().RunInitialImageQuery());
              Debug.Log("HTTPS - RunImageQuery 1");

              // Show the cropped image
              var rawImageObject = newContainer.transform.Find("UI/CanvasMetadata/imgObjectCaptured");
              rawImageObject.GetComponent<RawImage>().texture = ScreenCapturedTextureCropped;

              Debug.Log("ScreenCapturedTexture 3");

              // add a text label on top of the object
              var textObjectName = newContainer.transform.Find("UI/CanvasMetadata/textObjectName");
              textObjectName.GetComponent<TextMeshProUGUI>().text = ""; // (detectedObjectName + ": " + hits[0].distance + " / " + hits[0].sessionRelativeDistance);

              spawnedContainersList.Add(newContainer);

              // if spawned, add it to a list check later
              spawnedObjectNamesInScene.Add(detectedObjectName);

            }

          }

        }
        // then reset the variable
        detectedObjectListToAddToScene.Clear();

      }

    }

    if (setupInProgress)
    {
      // set up is still going on
      // can still set up object containers

      if (useCursor)
      {
        UpdateCursor();
      }

      // check if a touch exists an it's not an a UI element (e.g. button)
      if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began && !IsPointerOverUIObject())
      {
        if (useCursor)
        {
          GameObject newContainer = GameObject.Instantiate(objectToPlace, transform.position, transform.rotation);
          // newContainer.transform.parent = this.transform;
          // newContainer.name = "BallClone";

          var textObjectName = newContainer.transform.Find("UI/CanvasMetadata/textObjectName");
          textObjectName.GetComponent<TextMeshProUGUI>().text = "";

          spawnedContainersList.Add(newContainer);

        }


      }
    }
  }

  void UpdateCursor()
  {
    // get the middle point of the screen
    Vector2 screenPosition = Camera.main.ViewportToScreenPoint(new Vector2(0.5f, 0.5f));
    List<ARRaycastHit> hits = new List<ARRaycastHit>();
    // raycastManager.Raycast(screenPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinBounds);
    raycastManager.Raycast(screenPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.Depth);

    if (hits.Count > 0)
    {
      // raycast hits are sorted by distance, so the first one will be the closest hit.
      // and the last one will be the furthest away

      transform.position = hits[0].pose.position;
      transform.rotation = hits[0].pose.rotation;
    }
  }

  // hide detected planes (visuals)
  void setAllARPlanesActive(bool value)
  {
    foreach (var plane in m_ARPlaneManager.trackables)
    {
      plane.gameObject.SetActive(value);
    }

    m_ARPlaneManager.planePrefab.gameObject.SetActive(value);
    m_ARPlaneManager.SetTrackablesActive(value);
  }

  void buttonCursorCallback()
  {

    showMediaPipeBoundingBoxes = !showMediaPipeBoundingBoxes;
    cursorChildObject.SetActive(useCursor);

    if (showMediaPipeBoundingBoxes)
    {
      buttonCursor.GetComponentInChildren<TextMeshProUGUI>().text = "Bounding box ON";
    }
    else
    {
      buttonCursor.GetComponentInChildren<TextMeshProUGUI>().text = "Bounding box OFF";
    }

  }

  // function for toggling live MediaPipe detection
  void buttonMediaPipeCallback()
  {
    mediaPipeActive = !mediaPipeActive;

    if (mediaPipeActive)
    {
      buttonMediaPipe.GetComponentInChildren<TextMeshProUGUI>().text = "Detection ON";
      m_ARCameraManager.GetComponent<ARMPObjectDetection>().enabled = true;

    }
    else
    {
      buttonMediaPipe.GetComponentInChildren<TextMeshProUGUI>().text = "Detection OFF";
      m_ARCameraManager.GetComponent<ARMPObjectDetection>().enabled = false;
      gameObjectCameraPreview.GetComponent<RawImage>().enabled = false;

      UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
          objectText.text = "";
        });
    }
  }

  void buttonShowUICallback()
  {
    showUI = !showUI;

    if (showUI)
    {
      gameObjectUI.SetActive(true);
    }
    else
    {
      gameObjectUI.SetActive(false);
      gameObjectCameraPreview.GetComponent<RawImage>().enabled = false;

    }

  }

  // for switching setup <-> app modes
  void buttonPlanesCallback()
  {
    setupInProgress = !setupInProgress;

    // useCursor = !useCursor;
    // cursorChildObject.SetActive(useCursor);

    if (setupInProgress)
    {
      setAllARPlanesActive(true);
      buttonPlanes.GetComponentInChildren<TextMeshProUGUI>().text = "planes SHOWN";

      // foreach (var spawnedObject in spawnedContainersList)
      // {
      //   spawnedObject.transform.GetChild(0).gameObject.GetComponent<Renderer>().enabled = true;
      // }

      // also turn on cursor
      useCursor = true;
    }
    else
    {
      setAllARPlanesActive(false);
      buttonPlanes.GetComponentInChildren<TextMeshProUGUI>().text = "planes HIDDEN";

      // also turn off cursor
      useCursor = false;

    }

    cursorChildObject.SetActive(useCursor);
  }

  void buttonDeleteObjectsCallback()
  {
    foreach (var spawnedObject in spawnedContainersList)
    {
      try { Destroy(spawnedObject); }
      catch
      {
        Debug.Log("Couldn't delete spawnedObject!");
      }
    }
    spawnedContainersList = new List<GameObject>();
    spawnedObjectNamesInScene = new List<string>();

  }

  void buttonSpeechPanelCallback()
  {
    showSpeechPanel = !showSpeechPanel;

    if (showSpeechPanel)
    {
      //rect style
      speechPanel.SetActive(true);

    }
    else
    {
      speechPanel.SetActive(false);
    }
  }

  public void OutputCallback(object stream, OutputEventArgs<List<Detection>> eventArgs)
  {

    Debug.Log(eventArgs.value);

    var objectCount = eventArgs.value?.Count;
    Debug.Log("D objectCount: " + objectCount);

    if (objectCount > 0)
    {
      // copy the elements to the array so they can be rendered on OnGUI later
      // detectedObjectList = eventArgs.value; -> this won't work
      detectedObjectList = new List<Detection>(eventArgs.value);
      detectedObjectListToAddToScene = new List<Detection>(eventArgs.value);

      foreach (Detection detectedObject in eventArgs.value)
      {
        var detectedObjectName = detectedObject.Label[0];
        Debug.Log("Detected object label: " + detectedObjectName);
      }
    }
  }

  void OnGUI()
  {
    // using GUI for drawing MediaPipe-detected bounding boxed on objects
    if (!mediaPipeActive || !showMediaPipeBoundingBoxes)
    {
      // if not active, don't render anything
      return;
    }

    if (detectedObjectList?.Count > 0)
    {
      // Debug.Log("there are objects");
      foreach (Detection detectedObject in detectedObjectList)
      {
        // Debug.Log("D: OnGUI - the array label" + detectedObject.Label[0]);

        float textX = (float)((1 - detectedObject.LocationData?.RelativeBoundingBox?.Xmin) * UnityEngine.Screen.width);
        float textY = (float)((detectedObject.LocationData?.RelativeBoundingBox?.Ymin) * UnityEngine.Screen.height);

        // Debug.Log("detectedObject.LocationData?.RelativeBoundingBox?.Ymin: " + detectedObject.LocationData?.RelativeBoundingBox?.Ymin + " - " + detectedObject.Label[0]);

        float textWidth = (float)((detectedObject.LocationData?.RelativeBoundingBox?.Width) * UnityEngine.Screen.width);
        float textHeight = (float)((detectedObject.LocationData?.RelativeBoundingBox?.Height) * UnityEngine.Screen.height);


        // offset MediaPipe output vs GUI coordinates
        textX = textX - textWidth;

        objectTextStyle = new GUIStyle(GUI.skin.box)
        {
          alignment = TextAnchor.MiddleCenter,
          fontSize = 45
        };

        GUI.Box(new UnityEngine.Rect(textX, textY, textWidth, textHeight), detectedObject.Label[0], objectTextStyle);

        // check if the 2D bounding box corners are not too close to the camera image edges
        var thresholdX = 0.1;
        var thresholdY = 0.05;

        if (detectedObject.LocationData?.RelativeBoundingBox?.Xmin > thresholdX && (detectedObject.LocationData?.RelativeBoundingBox?.Xmin + detectedObject.LocationData?.RelativeBoundingBox?.Width) < (1 - thresholdX) &&
               detectedObject.LocationData?.RelativeBoundingBox?.Ymin > thresholdY && (detectedObject.LocationData?.RelativeBoundingBox?.Ymin + detectedObject.LocationData?.RelativeBoundingBox?.Height) < (1 - thresholdY))
        {
          Debug.Log("2D detection bounding box is in optimal location");
        }
        else
        {
          // skip this object as it is too close to the screen edges
          continue;
        }

        // draw a small square at the target location for raycasting
        // let's aim the mid-bottom point of the bounding box

        var raycastX = (float)(1 - detectedObject.LocationData?.RelativeBoundingBox?.Xmin);
        var raycastY = (float)(detectedObject.LocationData?.RelativeBoundingBox?.Ymin);

        // // offset for targeting bottom center point
        // raycastX -= (float)(detectedObject.LocationData?.RelativeBoundingBox?.Width) / 2;
        // raycastY += (float)(detectedObject.LocationData?.RelativeBoundingBox?.Height);

        // offset for targeting center-center point
        raycastX -= (float)(detectedObject.LocationData?.RelativeBoundingBox?.Width) / 2;
        raycastY += (float)(detectedObject.LocationData?.RelativeBoundingBox?.Height) / 2;

        // convert to screen size pixels
        raycastX *= UnityEngine.Screen.width;
        raycastY *= UnityEngine.Screen.height;

        var smallBoxSize = 12;
        // should possibly check if it exceeds screen dimensions
        GUI.Box(new UnityEngine.Rect(raycastX - smallBoxSize, raycastY - smallBoxSize, smallBoxSize * 2, smallBoxSize * 2), ".", objectTextStyle);

      }

      // then reset the variable
      // detectedObjectList.Clear();

    }
    
  }

}
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
  public bool auto_setup = false;
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

  // for manual registration (long press + circle)
  public bool allowLongPressSelect = false;
  private float longPressThreshold = 0.35f;
  private bool isLongPressing = false;
  private float pressStartTime = 0f;
  private bool fingerDown = false;
  private bool wasFingerDown = false;
  private bool longPressTriggered = false;
  private Vector2 startLongPressPos;

  public bool allowCircleSelect = false;
  private LineRenderer lineRenderer;
  private float drawingDistance = 10f;
  private float minDistanceThreshold = 5f;
  private Vector2 lastTouchPosition;
  private bool drawingCircle = false;

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

  private bool IsPointerOverGameObject()
  {
    // raycast to gameobjects using Physics.Raycast
    if (Input.touchCount > 0)
    {
      Touch touch = Input.GetTouch(0);

      RaycastHit hitGameObjects;
      if (Physics.Raycast(Camera.main.ScreenPointToRay(touch.position), out hitGameObjects))
      {
        if (hitGameObjects.collider.tag == "RealObjectProxy" || hitGameObjects.collider.tag == "RealObjectSphere")
        {
          return true;
        }
      }
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

    if (auto_setup) {
      Invoke("buttonPlanesCallback", 1f);
      Invoke("buttonMediaPipeCallback", 1f);
    }

     SetUpPathRenderer();
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
    // Debug.Log("ScreenCapturedTexture 1.5a " + cropRect.x + " " + cropRect.y + " " + cropRect.width + " " + cropRect.height);
    // var newPixels = sourceTexture.GetPixels(cropRect.x, cropRect.y, cropRect.width, cropRect.height);

    var newPixels = sourceTexture.GetPixels(cropRect.x, cropRect.y, cropRect.width, cropRect.height);
    // Debug.Log("ScreenCapturedTexture 1.5b ");
    var newTexture = new Texture2D(cropRect.width, cropRect.height);
    // Debug.Log("ScreenCapturedTexture 1.5c ");
    newTexture.SetPixels(newPixels);

    // Debug.Log("ScreenCapturedTexture 1.5d ");
    newTexture.Apply();
    // Debug.Log("ScreenCapturedTexture 1.5e ");
    return newTexture;
  }

  void Update()
  {
    if (allowLongPressSelect) CheckForLongPress();
    if (allowCircleSelect) UpdateDrawnPath();

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
            // Debug.Log("2D detection bounding box is in optimal location");
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


              // Debug.Log("ScreenCapturedTexture 1 ");

              // crop it
              var ScreenCapturedTextureCropped = CropTexture(ScreenCapturedTexture, new UnityEngine.Rect(cropWindowX, cropWindowY, cropWindowWidth, cropWindowHeight));

              // Debug.Log("ScreenCapturedTexture 2");

              // transfer the imagetexture variable
              newContainer.GetComponent<ImageQuery>().Texture2DImageOfObject = ScreenCapturedTextureCropped;
              // run Image Query
              // Debug.Log("HTTPS - RunImageQuery 0");
              //newContainer.GetComponent<ImageQuery>().RunInitialImageQuery();
              StartCoroutine(newContainer.GetComponent<ImageQuery>().RunInitialImageQuery());
              // Debug.Log("HTTPS - RunImageQuery 1");

              // Show the cropped image
              var rawImageObject = newContainer.transform.Find("UI/CanvasMetadata/imgObjectCaptured");
              rawImageObject.GetComponent<RawImage>().texture = ScreenCapturedTextureCropped;

              // Debug.Log("ScreenCapturedTexture 3");

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
    // Debug.Log("D objectCount: " + objectCount);

    if (objectCount > 0)
    {
      // copy the elements to the array so they can be rendered on OnGUI later
      // detectedObjectList = eventArgs.value; -> this won't work
      detectedObjectList = new List<Detection>(eventArgs.value);
      detectedObjectListToAddToScene = new List<Detection>(eventArgs.value);

      foreach (Detection detectedObject in eventArgs.value)
      {
        var detectedObjectName = detectedObject.Label[0];
        // Debug.Log("Detected object label: " + detectedObjectName);
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

  // Detects a long press using touch input.
    private void CheckForLongPress()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    // When the touch begins, record the time and reset flags.
                    fingerDown = true;
                    pressStartTime = Time.time;
                    longPressTriggered = false;
                    startLongPressPos = touch.position;
                    break;

                case TouchPhase.Stationary:
                case TouchPhase.Moved:
                    // If the finger is down and the press has been held long enough, trigger a long press.
                    if (fingerDown && !longPressTriggered && (Time.time - pressStartTime) >= longPressThreshold && (Time.time - pressStartTime) <= longPressThreshold + 0.1f)
                    {
                        if (Vector2.Distance(touch.position, startLongPressPos) <= 20) {
                        isLongPressing = true;
                        longPressTriggered = true;
                        // Handheld.Vibrate();

                        StartCoroutine(TryAddObjectFromLongPress());
                      }
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    // Reset the state when the touch ends.
                    fingerDown = false;
                    isLongPressing = false;
                    break;
            }
        }
        else
        {
            // If there are no active touches, ensure the state is reset.
            fingerDown = false;
            isLongPressing = false;
        }
    }

    // Commands haptic feedback with specified duration and intensity (Android only).
    private static void Vibrate(long milliseconds, int amplitude = -1)
    {
      if (UnityEngine.Application.platform != RuntimePlatform.Android)
          return;

      try
      {
          // Get the current Android activity from the UnityPlayer.
          AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
          AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

          // Get the Vibrator service.
          AndroidJavaObject vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
          if (vibrator == null)
              return;

          // Check the device's API level.
          AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION");
          int sdkInt = version.GetStatic<int>("SDK_INT");

          if (sdkInt >= 26)
          {
              // For API 26 and above, use VibrationEffect to customize amplitude.
              AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
              AndroidJavaObject vibrationEffect;

              // If a valid amplitude is provided, create a vibration effect with that amplitude.
              if (amplitude >= 0)
              {
                  vibrationEffect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude);
              }
              else
              {
                  // Use the default amplitude.
                  vibrationEffect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, null);
              }

              vibrator.Call("vibrate", vibrationEffect);
          }
          else
          {
              // For older devices, simply call vibrate with duration.
              vibrator.Call("vibrate", milliseconds);
          }
      }
      catch (System.Exception e)
      {
          Debug.LogWarning("Android Vibrator error: " + e.Message);
      }
  }

  // Sets up the Line Renderer component for drawing the finger path.
  void SetUpPathRenderer()
  {
    lineRenderer = gameObject.AddComponent<LineRenderer>();
    lineRenderer.transform.SetParent(Camera.main.transform);
    lineRenderer.positionCount = 0;
    // Use a simple material that supports vertex colors.
    lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    // Set the line color to white.
    lineRenderer.startColor = UnityEngine.Color.white;
    lineRenderer.endColor = UnityEngine.Color.white;
    // Set a fixed width for the line.
    lineRenderer.startWidth = 0.1f;
    lineRenderer.endWidth = 0.1f;
  }

  // Updates the Line Renderer to draw the finger path.
  private void UpdateDrawnPath()
  {
    if (fingerDown)
    {
        // If this is the start of a long press, clear any previous path and reset the last touch position.
        if (!wasFingerDown)
        {
            if (!IsPointerOverGameObject() && !IsPointerOverUIObject()) {
                drawingCircle = true;

                // start the path
                lineRenderer.positionCount = 0;
                Touch touch = Input.GetTouch(0);
                lastTouchPosition = touch.position;
                AddPoint(lastTouchPosition);
            }
        }
        else
        {
            if (drawingCircle && !isLongPressing) {
              Touch touch = Input.GetTouch(0);
              // Only add a new point if the finger has moved enough in screen space.
              if (Vector2.Distance(touch.position, lastTouchPosition) >= minDistanceThreshold)
              {
                  lastTouchPosition = touch.position;
                  AddPoint(lastTouchPosition);
              }
            }

            // make sure we're not drawing if a long press was triggered
            if (isLongPressing) {
              lineRenderer.positionCount = 0;
              drawingCircle = false;
            }
        }
    }
    else
    {
        // When the finger is lifted clear the path.
        if (wasFingerDown)
        {
            // Debug.Log("DRAW -- Path Length = " +  GetPathLengthInPixels(lineRenderer));
            if (drawingCircle && GetPathLengthInPixels(lineRenderer) > 300) {
              // Debug.Log("DRAW -- TRYING OBJECT");
              StartCoroutine(TryAddObjectFromPath());
            }

            // reset
            lineRenderer.positionCount = 0;
            drawingCircle = false;
        }
    }

    // Update the previous long press state.
    wasFingerDown = fingerDown;
  }

  void AddPoint(Vector2 screenPosition)
  {
    // Convert screen position to world position.
    Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, drawingDistance));
    int posCount = lineRenderer.positionCount;
    lineRenderer.positionCount = posCount + 1;
    lineRenderer.SetPosition(posCount, worldPosition);
  }

  private Vector2[] GetScreenCoordinatesFromLineRenderer(LineRenderer lr)
  {
      int pointCount = lr.positionCount;
      Vector2[] screenPoints = new Vector2[pointCount];

      for (int i = 0; i < pointCount; i++)
      {
          // Get the world position from the line renderer.
          Vector3 worldPos = lr.GetPosition(i);
          // Convert the world position to screen space.
          Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
          // Store only the x and y components.
          screenPoints[i] = new Vector2(screenPos.x, screenPos.y);
      }

      return screenPoints;
  }

  private float GetPathLengthInPixels(LineRenderer lr)
  {
      // Get the screen space coordinates for all points in the line renderer.
      Vector2[] screenPoints = GetScreenCoordinatesFromLineRenderer(lr);
      float totalLength = 0f;
      
      // Sum the distances between consecutive points.
      for (int i = 1; i < screenPoints.Length; i++)
      {
          totalLength += Vector2.Distance(screenPoints[i - 1], screenPoints[i]);
      }
      
      return totalLength;
  }

  // Called when manually registering using Circle to select
  private IEnumerator TryAddObjectFromPath() {

    // get bounding box of drawn path
    Vector2[] screenPoints = GetScreenCoordinatesFromLineRenderer(lineRenderer);
    
    // Initialize bounds.
    float xmin = float.MaxValue;
    float ymin = float.MaxValue;
    float xmax = float.MinValue;
    float ymax = float.MinValue;
    Vector2 center = Vector2.zero;

    if (screenPoints == null || screenPoints.Length == 0)
    {
        yield break;
    }
    
    // Determine the min and max values.
    foreach (Vector2 point in screenPoints)
    {
        if (point.x < xmin) xmin = point.x;
        if (point.x > xmax) xmax = point.x;
        if (point.y < ymin) ymin = point.y;
        if (point.y > ymax) ymax = point.y;
    }

    // add buffer
    float buffer = 50;
    xmin -= buffer;
    ymin -= buffer;
    xmax += buffer;
    ymax += buffer;

    // Clamp the values.
    xmin = Mathf.Clamp(xmin, 0, UnityEngine.Screen.width);
    xmax = Mathf.Clamp(xmax, 0, UnityEngine.Screen.width);
    ymin = Mathf.Clamp(ymin, 0, UnityEngine.Screen.height);
    ymax = Mathf.Clamp(ymax, 0, UnityEngine.Screen.height);
    
    // Calculate the center point.
    center = new Vector2((xmin + xmax) / 2f, (ymin + ymax) / 2f);

    // Debug.Log($"DRAW --- Bounding Box: xmin: {xmin}, ymin: {ymin}, xmax: {xmax}, ymax: {ymax}; Center: {center}");

    // Check if center hits existing XR Object
    var screenPosition = center;
    RaycastHit hitGameObjects;
    if (Physics.Raycast(Camera.main.ScreenPointToRay(screenPosition), out hitGameObjects))
    {
      if (hitGameObjects.collider.tag == "RealObjectProxy" || hitGameObjects.collider.tag == "RealObjectSphere")
      {
        // a RealObjectProxy has already been spawned here, so skip
        yield break;
      }

    }

    // Grab initial screenshot
    GUI.enabled = false;
    lineRenderer.enabled = false;  // hide line (to do: hide all graphical overlays)

    // Wait for the end of frame to ensure the line is not rendered.
    yield return new WaitForEndOfFrame();

    Texture2D ScreenCapturedTexture = ScreenCapture.CaptureScreenshotAsTexture();
    lineRenderer.enabled = true;
    GUI.enabled = true;

    // Get crop window
    var ScreenCapturedTextureCropped = CropTexture(ScreenCapturedTexture, new UnityEngine.Rect(xmin, ymin, xmax - xmin, ymax - ymin));

    // [DEBUG] image preview
    RawImage imagePreview = GameObject.FindWithTag("ObjectComparerImagePreview")?.GetComponent<RawImage>();
    if (imagePreview != null) {
      imagePreview.texture = ScreenCapturedTextureCropped;
    }

    // Raycast into AR Scene to get depth
    List<ARRaycastHit> hits = new List<ARRaycastHit>();
    raycastManager.Raycast(screenPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.Depth);

    if (hits.Count > 0)
    {
      GameObject newContainer = GameObject.Instantiate(objectToPlace, hits[0].pose.position, hits[0].pose.rotation);

      // transfer the imagetexture variable
      newContainer.GetComponent<ImageQuery>().Texture2DImageOfObject = ScreenCapturedTextureCropped;
      
      // run Image Query
      StartCoroutine(newContainer.GetComponent<ImageQuery>().RunInitialImageQuery());

      // Show the cropped image
      var rawImageObject = newContainer.transform.Find("UI/CanvasMetadata/imgObjectCaptured");
      rawImageObject.GetComponent<RawImage>().texture = ScreenCapturedTextureCropped;


      // add a text label on top of the object
      var textObjectName = newContainer.transform.Find("UI/CanvasMetadata/textObjectName");
      textObjectName.GetComponent<TextMeshProUGUI>().text = ""; // (detectedObjectName + ": " + hits[0].distance + " / " + hits[0].sessionRelativeDistance);

      spawnedContainersList.Add(newContainer);
    }
  }

  // Called when manually registering using Long Press to select
  private IEnumerator TryAddObjectFromLongPress() {

    // Calculate the center point.
    var center = Input.GetTouch(0).position;

    // add buffer
    float buffer = 400;
    float xmin = center.x - buffer;
    float ymin = center.y - buffer;
    float xmax = center.x + buffer;
    float ymax = center.y + buffer;

    // Clamp the values.
    xmin = Mathf.Clamp(xmin, 0, UnityEngine.Screen.width);
    xmax = Mathf.Clamp(xmax, 0, UnityEngine.Screen.width);
    ymin = Mathf.Clamp(ymin, 0, UnityEngine.Screen.height);
    ymax = Mathf.Clamp(ymax, 0, UnityEngine.Screen.height);

    // Debug.Log($"LONGPRESS --- Bounding Box: xmin: {xmin}, ymin: {ymin}, xmax: {xmax}, ymax: {ymax}; Center: {center}");

    // Check if center hits existing XR Object
    var screenPosition = center;
    RaycastHit hitGameObjects;
    if (Physics.Raycast(Camera.main.ScreenPointToRay(screenPosition), out hitGameObjects))
    {
      if (hitGameObjects.collider.tag == "RealObjectProxy" || hitGameObjects.collider.tag == "RealObjectSphere")
      {
        // a RealObjectProxy has already been spawned here, so skip
        yield break;
      }

    }

    // Grab initial screenshot
    GUI.enabled = false;
    lineRenderer.enabled = false;  // hide line (to do: hide all graphical overlays)

    // Wait for the end of frame to ensure the line is not rendered.
    yield return new WaitForEndOfFrame();

    Texture2D ScreenCapturedTexture = ScreenCapture.CaptureScreenshotAsTexture();
    lineRenderer.enabled = true;
    GUI.enabled = true;

    // Get crop window
    var ScreenCapturedTextureCropped = CropTexture(ScreenCapturedTexture, new UnityEngine.Rect(xmin, ymin, xmax - xmin, ymax - ymin));

    // [DEBUG] image preview
    RawImage imagePreview = GameObject.FindWithTag("ObjectComparerImagePreview")?.GetComponent<RawImage>();
    if (imagePreview != null) {
      imagePreview.texture = ScreenCapturedTextureCropped;
    }

    // Raycast into AR Scene to get depth
    List<ARRaycastHit> hits = new List<ARRaycastHit>();
    raycastManager.Raycast(screenPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.Depth);

    if (hits.Count > 0)
    {
      GameObject newContainer = GameObject.Instantiate(objectToPlace, hits[0].pose.position, hits[0].pose.rotation);

      // transfer the imagetexture variable
      newContainer.GetComponent<ImageQuery>().Texture2DImageOfObject = ScreenCapturedTextureCropped;
      
      // run Image Query
      StartCoroutine(newContainer.GetComponent<ImageQuery>().RunInitialImageQuery());

      // Show the cropped image
      var rawImageObject = newContainer.transform.Find("UI/CanvasMetadata/imgObjectCaptured");
      rawImageObject.GetComponent<RawImage>().texture = ScreenCapturedTextureCropped;

      // add a text label on top of the object
      var textObjectName = newContainer.transform.Find("UI/CanvasMetadata/textObjectName");
      textObjectName.GetComponent<TextMeshProUGUI>().text = ""; // (detectedObjectName + ": " + hits[0].distance + " / " + hits[0].sessionRelativeDistance);

      spawnedContainersList.Add(newContainer);

      // Vibrate if successful
      Vibrate(50,50);
    }
  }
}
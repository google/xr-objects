// Copyright 2024 Google LLC

// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using Mediapipe;
using Mediapipe.Unity;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Stopwatch = System.Diagnostics.Stopwatch;

// Class for integrating ARCore and MediaPipe real-time object detection
// i.e., pass ARCore real-time frames to MediaPipe ML model

public class ARMPObjectDetection : MonoBehaviour
{

  [SerializeField] private ARCameraManager _cameraManager;
  [SerializeField] private TextAsset _configText; // attach  'object_detection_gpu' or `face_detection_gpu.txt` 
                                                  // depending on the task

  [SerializeField]
  [Tooltip("The UI RawImage used to display the image on screen.")]
  private RawImage _rawImage;
  Texture2D m_Texture;
  public Boolean renderMLCapture;

  private CalculatorGraph _calculatorGraph;
  private NativeArray<byte> _buffer;
    // private byte[] _bufferTemp;
  private NativeArray<byte> _bufferTemp;

  private Stopwatch _stopwatch;
  private ResourceManager _resourceManager;
  private GpuResources _gpuResources;

  //private OutputStream<DetectionVectorPacket, List<Detection>> _faceDetectionsStream; ]
  private OutputStream<DetectionVectorPacket, List<Detection>> _outputDetectionsStream;

  // boolean for toggling the detection from outside scripts
  public Boolean MediaPipeDisabled = false;

  // D: reference the ARcursor script
  public UnityEvent<object, OutputEventArgs<List<Detection>>> callbackEvent;


  private IEnumerator Start()
  {
    _cameraManager.frameReceived += OnCameraFrameReceived;
    _stopwatch = new Stopwatch();

    _resourceManager = new StreamingAssetsResourceManager();
    yield return _resourceManager.PrepareAssetAsync("ssdlite_object_detection_labelmap.txt");
    yield return _resourceManager.PrepareAssetAsync("ssdlite_object_detection.bytes");

    _gpuResources = GpuResources.Create().Value();
    _calculatorGraph = new CalculatorGraph(_configText.text);
    _calculatorGraph.SetGpuResources(_gpuResources).AssertOk();


    _outputDetectionsStream = new OutputStream<DetectionVectorPacket, List<Detection>>(_calculatorGraph, "output_detections");
    // _outputDetectionsStream.AddListener(OutputCallback);
    _outputDetectionsStream.AddListener(callbackEvent.Invoke);


    var sidePacket = new SidePacket();
    sidePacket.Emplace("input_rotation", new IntPacket(0));
    sidePacket.Emplace("input_horizontally_flipped", new BoolPacket(false));
    sidePacket.Emplace("input_vertically_flipped", new BoolPacket(true));
    sidePacket.Emplace("model_type", new IntPacket(0));

    _calculatorGraph.StartRun(sidePacket).AssertOk();
    _stopwatch.Start();
  }

  private void OnDestroy()
  {
    Debug.Log("MediaPipe is turning off!");
    _cameraManager.frameReceived -= OnCameraFrameReceived;

    var status = _calculatorGraph.CloseAllPacketSources();
    if (!status.Ok())
    {
      Debug.Log($"Failed to close packet sources: {status}");
    }

    status = _calculatorGraph.WaitUntilDone();
    if (!status.Ok())
    {
      Debug.Log(status);
    }

    _calculatorGraph.Dispose();
    _gpuResources.Dispose();
    _buffer.Dispose();
  }

  void OnDisable()
  {
    Debug.Log("D: MediaPipe: script was disabled");
    MediaPipeDisabled = true;


  }

  void OnEnable()
  {
    Debug.Log("D: MediaPipe: script was enabled");
    MediaPipeDisabled = false;
  }

  private unsafe void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
  {

    // if the script has been disabled by the user, return already
    if (MediaPipeDisabled) { return; }

    if (_cameraManager.TryAcquireLatestCpuImage(out var image))
    {
      // var conversionParams = new XRCpuImage.ConversionParams(image, TextureFormat.RGBA32);

      // need to crop the image according to the screensize, otherwise it will detect things out of the user view!
      // https://github.com/Unity-Technologies/arfoundation-samples/issues/796#issuecomment-1640228457
      // XRCpuImage.Transformation imageTransformation = GetImageTransformation();

      // show on the screen
      // UpdateRawImage(_rawImage, image);

      int adjustedCpuImageHeight = (int)((float)image.width / (float)UnityEngine.Device.Screen.width * (float)UnityEngine.Device.Screen.height);
      int startY = (image.height - adjustedCpuImageHeight) / 2;

      InitBuffer(image, adjustedCpuImageHeight);

      var conversionParams = new XRCpuImage.ConversionParams
      {
        inputRect = new RectInt(0, startY, image.width, adjustedCpuImageHeight),
        outputDimensions = new Vector2Int(image.width, adjustedCpuImageHeight),
        // outputFormat = image.format.AsTextureFormat(),
        outputFormat = TextureFormat.RGBA32,
        //transformation = imageTransformation
      };




      var ptr = (IntPtr)NativeArrayUnsafeUtility.GetUnsafePtr(_buffer);
      image.Convert(conversionParams, ptr, _buffer.Length);
      image.Dispose();


      if (renderMLCapture)
      {
        // apply the texture to the RawImage
        // https://docs.unity.cn/Packages/com.unity.xr.arfoundation@1.1/manual/cpu-camera-image.html
        var length = image.width * adjustedCpuImageHeight * 4;
        _bufferTemp = new NativeArray<byte>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        // Debug.Log("DD: _bufferTemp 1");
        _buffer.CopyTo(_bufferTemp);
        // Debug.Log("DD: _bufferTemp 2");

        m_Texture = new Texture2D(
              conversionParams.outputDimensions.x,
              conversionParams.outputDimensions.y,
              conversionParams.outputFormat,
              false);

        // m_Texture.hideFlags = HideFlags.HideAndDontSave;

        // first destroy the old texture
        // https://forum.unity.com/threads/arfoundation-xrcpuimage-convertasync-memory-leak.924995/#post-6781148
        Destroy(_rawImage.texture);

        _rawImage.texture = m_Texture;
        m_Texture.LoadRawTextureData(_bufferTemp);
        m_Texture.Apply();

        // Done with our temporary data
        // THIS MIGHT BE DANGEROUS
        // Debug.Log("DD: _bufferTemp 3");
        _bufferTemp.Dispose();
        // Destroy(m_Texture); 
        // Debug.Log("DD: _bufferTemp 4");
      }

      // var imageFrame = new ImageFrame(ImageFormat.Types.Format.Srgba, image.width, image.height, 4 * image.width, _buffer);
      var imageFrame = new ImageFrame(ImageFormat.Types.Format.Srgba, image.width, adjustedCpuImageHeight, 4 * image.width, _buffer);
      var currentTimestamp = _stopwatch.ElapsedTicks / (TimeSpan.TicksPerMillisecond / 1000);
      var imageFramePacket = new ImageFramePacket(imageFrame, new Timestamp(currentTimestamp));

      _calculatorGraph.AddPacketToInputStream("input_video", imageFramePacket).AssertOk();

    }
  }


  private void InitBuffer(XRCpuImage image, int adjustedCpuImageHeight)
  {
    // var length = image.width * image.height * 4;
    var length = image.width * adjustedCpuImageHeight * 4;
    if (_buffer == null || _buffer.Length != length)
    {
      _buffer = new NativeArray<byte>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
      
    }
  }

}
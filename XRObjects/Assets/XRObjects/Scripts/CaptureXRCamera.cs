using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Class for capturing ARCore camera stream as a single frame

public class CaptureXRCamera : MonoBehaviour
{
  // when captureDue = true, then capture an image via the XR camera
  [HideInInspector] public bool captureDue = false;

  Texture2D m_Texture;

  [SerializeField] private ARCameraManager cameraManager;
  private ActionClass requestingActionClass;

  void OnEnable()
  {
    cameraManager.frameReceived += OnCameraFrameReceived;
  }

  void OnDisable()
  {
    cameraManager.frameReceived -= OnCameraFrameReceived;
  }

  public void CaptureNextTime(ActionClass callingActionClass){

    captureDue = true;
    requestingActionClass = callingActionClass;
    //return m_Texture;

  }

  unsafe void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
  {

    if (!captureDue) { return; }

    if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
      return;

    var conversionParams = new XRCpuImage.ConversionParams
    {
      // Get the entire image.
      inputRect = new RectInt(0, 0, image.width, image.height),

      // Downsample by 2.
      // outputDimensions = new Vector2Int(image.width / 2, image.height / 2),
      outputDimensions = new Vector2Int(image.width, image.height),

      // Choose RGBA format.
      outputFormat = TextureFormat.RGBA32

      // Flip across the vertical axis (mirror image).
      // transformation = XRCpuImage.Transformation.MirrorY
    };

    // See how many bytes you need to store the final image.
    int size = image.GetConvertedDataSize(conversionParams);

    // Allocate a buffer to store the image.
    var buffer = new NativeArray<byte>(size, Allocator.Temp);

    // Extract the image data
    image.Convert(conversionParams, new IntPtr(buffer.GetUnsafePtr()), buffer.Length);

    // The image was converted to RGBA32 format and written into the provided buffer
    // so you can dispose of the XRCpuImage. You must do this or it will leak resources.
    image.Dispose();


    // let's put it into a texture so you can visualize it.
    m_Texture = new Texture2D(
        conversionParams.outputDimensions.x,
        conversionParams.outputDimensions.y,
        conversionParams.outputFormat,
        false);

    m_Texture.LoadRawTextureData(buffer);
    m_Texture.Apply();

    // Done with your temporary data, so you can dispose it.
    buffer.Dispose();

    Debug.Log("CaptureCamera: captured");

    requestingActionClass.onImageCaptureFinished(m_Texture);

    captureDue = false;

  }

  


}
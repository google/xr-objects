// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mediapipe.Unity.ObjectDetection
{
  public class ObjectDetectionSolution : ImageSourceSolution<ObjectDetectionGraph>
  {
    [SerializeField] private DetectionListAnnotationController _outputDetectionsAnnotationController;

    protected override void OnStartRun()
    {
      Debug.Log("MediaPipe: starting");
      if (!runningMode.IsSynchronous())
      {
        graphRunner.OnOutputDetectionsOutput += OnOutputDetectionsOutput;
      }

      SetupAnnotationController(_outputDetectionsAnnotationController, ImageSourceProvider.ImageSource);
    }

    protected override void AddTextureFrameToInputStream(TextureFrame textureFrame)
    {
      graphRunner.AddTextureFrameToInputStream(textureFrame);
    }

    protected override IEnumerator WaitForNextValue()
    {
      List<Detection> outputDetections = null;

      if (runningMode == RunningMode.Sync)
      {
        var _ = graphRunner.TryGetNext(out outputDetections, true);
      }
      else if (runningMode == RunningMode.NonBlockingSync)
      {
        yield return new WaitUntil(() => graphRunner.TryGetNext(out outputDetections, false));
      }

      _outputDetectionsAnnotationController.DrawNow(outputDetections);
    }

    private void OnOutputDetectionsOutput(object stream, OutputEventArgs<List<Detection>> eventArgs)
    {
      _outputDetectionsAnnotationController.DrawLater(eventArgs.value);
      
      var objectCount = eventArgs.value?.Count;

      Debug.Log(objectCount);
      
      if(objectCount>0){

        Debug.Log("there are objects");

        foreach(Detection detectedObject in eventArgs.value){
          Debug.Log("detectedObject === " + detectedObject);

          Debug.Log("MediaPipe Label: " + detectedObject.Label[0]);
          // Debug.Log("LabelId = " + detectedObject.LabelId);
          Debug.Log("LocationData = " + detectedObject.LocationData);


        }

      }
      
      
      //foreach(ObjectDetectionSolution.)
    }
  }
}

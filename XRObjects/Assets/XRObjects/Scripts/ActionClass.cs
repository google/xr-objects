// Copyright 2024 Google LLC

// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Base class for digital actions with speech transcription and/or image capture

public class ActionClass : MonoBehaviour
{
  public virtual void onTranscriptionFinished(string speechTranscribedText)
  {
    // Debug.Log("onTranscriptionFinished - base class");
  }

  public virtual void onImageCaptureFinished(Texture2D image)
  {
    // Debug.Log("onImageCaptureFinished - base class");
  }

}

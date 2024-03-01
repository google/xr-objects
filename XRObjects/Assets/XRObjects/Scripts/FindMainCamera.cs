// Copyright 2024 Google LLC

// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//  Class for attaching the world camera to the UI canvas
// so we can detect touch events

public class FindMainCamera : MonoBehaviour
{

  private Canvas canvas;

  void Awake()
  {
    canvas = GetComponent<Canvas>();
    canvas.worldCamera = Camera.main;
  }


}

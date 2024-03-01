// Copyright 2024 Google LLC

// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class for ensuring the object always faces the user
public class LookAtCamera : MonoBehaviour
{
  // Update is called once per frame
  void Update()
  {
    // transform.LookAt(Camera.main.transform);
    transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
  }

}

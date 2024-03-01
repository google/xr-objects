// Copyright 2024 Google LLC

// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class for determining the name of the WiFi network
// the device is connected to. Although not utilized, it can be
// helpful for understanding object context (e.g., home or work)

// the user needs to give access to "Location" in AndroidManifest
// and in app's settings on the phone
// https://stackoverflow.com/a/47847947

public class WiFiNetworkIdentifier : MonoBehaviour
{
  
  private string WiFiSSID;
  void Start()
  {
    IdentifyWiFiSSID();
  }

  void IdentifyWiFiSSID()
  {
    AndroidJavaClass contextClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
    AndroidJavaObject currentActivity = contextClass.GetStatic<AndroidJavaObject>("currentActivity");
    AndroidJavaObject wifiManager = currentActivity.Call<AndroidJavaObject>("getSystemService", "wifi");

    AndroidJavaObject connectionInfo = wifiManager.Call<AndroidJavaObject>("getConnectionInfo");
    WiFiSSID = connectionInfo.Call<string>("getSSID");

    Debug.Log("WiFi SSID is: " + WiFiSSID);

  }
}

# UVC4UnityAndroid


Plugin project and samples to access UVC devices on Unity Android.

Copyright (c) 2014-2022 saki t_saki@serenegiant.com

 Licensed under the Apache License, Version 2.0 (the "License");
 you may not use this file except in compliance with the License.
 You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.

All files in the folder are under this Apache License, Version 2.0.   
Files under `UVC4UnityAndroid/Assets/UVC4UnityAndroidPlugin/Samples/` may have different license. Please read README.md in those folders well.


## Features:

* Supports UVC 1.0/1.1 and 1.5 devices (Most of UVC devices are UVC1.1).
* Supports `armeabi-v7a`, `arm64-v8a`, `x86` and `x86_64` architectures.  
  `arm64-v8a`, `x86` and `x86_64` require IL2CPP.
* Supports H.264, MJPEG and YUV and automatically decode video images into Texture of Unity. Please see sample scenes in Samples folder.   
   You can get H.264 stream from Ricoh THETA S(1920x1080@H.264,30fps, dual fisheye video images) and THETA V(3840x1920@H.264,30fps, equirectangular video images), THETA Z1(3840x1920@H.264,30fps, equirectangular video images), Logitech C920, C922, C930e etc.
* Supports multiple UVC devices at the same time(Although frame rate, number of UVC devices and video size will be limited by bandwidth and power supply of USB.)
* Supports both isochronous and bulk transfer.
* Supports USB2 and USB3(experimental). Unfortunately USB3 on many devices still have issues, especially when using isochronous transfer.
* Support changing video size.

## Limitations:  

* Apk with target API level 28 and more will not work well on Android 10 devices because of issues on Android 10 itself (Many devices of Android 10 and later were already solved this issues but you may come across this issues).
  Please see details about this on [Issue Tracker of Google](https://issuetracker.google.com/issues/145082934) and [My Blog](https://serenegiant.com/blog/?p=3696).
* This project is still in progress and some features are not available now. Ex. Controlling UVC devices is not implemented yet. Ex. controlling contrast, brightness, shutter condition etc. do not work yet.
* You can get video images only from UVC devices, some devices like EasyCap are not UVC device and can't get from them. Internal cameras on Android devices also are not supported.
* Backend libraries support UBS3, but USB3 on Android devices are still unstable and it may not work well. In that case please connect your UVC devices over USB2.
* This plugin can work on only real Android devices, can't work on editor of Unity and on `Unity Remote`
* User may need to touch app screen after giving USB permission on some Android devices by limitation os Android OS itself.
* Streaming with H.264 needs Android 8 and later devices (required API>=26 but API>=29 will be better for performance)
* Android system never send attach event for THETA V and THETA Z1(becauseof issues on Android system itself), as a result app can never start automatically. Please launch app by manually.
* Be sure to confirm THETA V and Z1 is in `live streaming` mode. THETA V and Z1 will report `USB Image Class` when they are in `camera mode` or `video mode` and app will detecte it. But this `USB Image Class` is not av `UVC(USB Video Class)` and have no video control interface and video control interface and can't get video images from it. 
* Currently only supports OpenGL|ES as Graphic API. Vulkan is not supported now. If you can't see video images, please confirm settings of Graphic API on player settings.

## Dependancies:

* This plugin uses `System.Text.Json`(and related packages) from Microsoft to parse JSON. You can install the package using `NuGet`(You can use [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity/releases))  
  If you need to use other package like `Json.NET` to parse json, you can use it with some modification.  


## How to use:

1. Create new project / open existing project by Unity.
2. Install `System.Text.Json` if you don't yet.
   1. Install 'NuGet'([NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity/releases)).
   2. Select `Manage NuGet Packages` from `NuGet` menu.
   3. Search `System.Text.Json` from search box.
   4. Select `System.Text.Json` and install it.
3. Import release package of `UVC4UnityAndroid`
4. Confirm Graphic API setting on player settings, only OpenGL|ES is supported.
5. Open one of sample scene from `UVC4UnityAndroid/Samples/Scenes` folder.
6. Build and run apk on your real Android device.
7. Connect UVC device(s) with Android Device. Some UVC devices / some Android devices may need powered USB hub between UVC device and Android device.

## Note:

* The plugin project is for Unity 2020.3.36f1 now and may need modification on other Unity version.
* This plugin setup `AndroidManifest.xml` to be able to keep permanent permission for UVC device(s).
   If you don't want this behavior, please remove following steps. The user need to give permission everytime they connect UVC device(s).
   1. Export your project as project of Android Studio from `Build Settings` window.
   2. Open the exported project with Android Studio.
   3. Open `AndroidManifest.xml` under `{project root}/src/main`
   4. Add `<activity android:name="com.serenegiant.uvcplugin.UsbPermissionActivity" tools:node="remove"/>` in `Application` section.
   5. Build apk with Android Studio.

## Release Note:

* r0.1.0 on 24 Dec. 2019
   * First release.
* r0.2.0 on 3 July. 2022

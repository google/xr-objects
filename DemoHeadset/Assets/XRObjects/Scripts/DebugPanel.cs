using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugPanel : MonoBehaviour
{
  // create a dictionary for holding debug messages in pairs (by keys)
  Dictionary<string, string> _debugLogs = new Dictionary<string, string>();
  public TextMeshProUGUI debugText;

  // private void Start(){
    
  // }
  // public void LoadLibrary()
  // {
  //   using (var system = new AndroidJavaClass("java.lang.System"))
  //   {
  //       system.CallStatic("loadLibrary", "mediapipe_jni");
  //   }
  // }

  private void Update()
  {
    Debug.Log("Time: " + Time.time);
  }

  private void OnEnable() => Application.logMessageReceived += HandleLog;

  private void OnDisable() => Application.logMessageReceived -= HandleLog;

  void HandleLog(string logString, string stackTrace, LogType type)
  {
    if (type == LogType.Log)
    {
      // get log keys, i.e., whatever is before ":"
      var splitSubstrings = logString.Split(char.Parse(":"));
      var debugKey = splitSubstrings[0];
      var debugValue = splitSubstrings.Length > 1 ? splitSubstrings[1] : "";

      if (_debugLogs.ContainsKey(debugKey))
        // if available in a prevuous message, replace with the new
        _debugLogs[debugKey] = debugValue;
      else
        _debugLogs.Add(debugKey, debugValue);

    }

    string displayText = "";
    foreach (KeyValuePair<string, string> log in _debugLogs)
    {
      if (log.Value == "")
        displayText += log.Key + "\n";
      else
        displayText += log.Key + ": " + log.Value + "\n";
    }

    debugText.text = displayText;
  }
}
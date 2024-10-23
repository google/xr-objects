// Copyright 2024 Google LLC

// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

using System.Text;
using System.Linq;
using TMPro;

// Class for making HTTP calls to the Gemini API
// don't forget to input your personal Gemini API key

public class ImageQuery : MonoBehaviour
{

  // Get your Gemini API key here: https://aistudio.google.com/app/apikey
  private string apiKey = "enter your key here";
  private string queryURL;

  [HideInInspector] public Texture2D Texture2DImageOfObject;

  // variable to store the object ID specific to the object
  [HideInInspector] public string objectConversationID;
  

  // for the input
  [System.Serializable]
  public class RequestContent
  {
    public RequestPart[] parts;
  }

  [System.Serializable]
  public class RequestPart
  {
    public string text;
    public InlineData inline_data;
  }

  [System.Serializable]
  public class InlineData
  {
    public string mime_type;
    public string data;
  }

  [System.Serializable]
  public class RequestBody
  {
    public RequestContent[] contents;
  }

  // for the output

  [System.Serializable]
  public class GeminiAPIResponse
  {
    public Candidate[] candidates;
  }

  [System.Serializable]
  public class Candidate
  {
    public Content content;
    public string finishReason;
    public int index;
    public SafetyRating[] safetyRatings;
  }

  [System.Serializable]
  public class Content
  {
    public Part[] parts;
    public string role;
  }

  [System.Serializable]
  public class Part
  {
    public string text;
  }

  [System.Serializable]
  public class SafetyRating
  {
    public string category;
    public string probability;
  }

  [System.Serializable]
  public class PromptFeedback
  {
    public SafetyRating[] safetyRatings;
  }


  public class CoroutineWithOutput
  {
    public Coroutine coroutine { get; private set; }
    private IEnumerator coroutineTarget;
    public object output;

    private IEnumerator RunCoroutine()
    {
      while (coroutineTarget.MoveNext())
      {
        output = coroutineTarget.Current;
        yield return output;
      }
    }

    public CoroutineWithOutput(MonoBehaviour owner, IEnumerator coroutineTarget)
    {
      this.coroutineTarget = coroutineTarget;
      this.coroutine = owner.StartCoroutine(RunCoroutine());
    }
  }


  void Awake()
  {

    queryURL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key=" + apiKey;

  }
  void Start()
  {
    // test if internet connection works
    // StartCoroutine(HTTPGet("https://www.google.com/"));
    // StartCoroutine(HTTPPost("https://httpbin.org/post", ""));
  }

  public string CreateJsonData(string promptText, Texture2D texture2DImageOfObject)
  {
    string base64Image = System.Convert.ToBase64String(texture2DImageOfObject.EncodeToJPG());

    // Manually constructing the JSON string to ensure correct format
    string jsonData = "{"
                    + "\"contents\":[{"
                    + "\"parts\":["
                    + "{\"text\":\"" + promptText + "\"},"
                    + "{\"inline_data\": { \"mime_type\":\"image/jpeg\", \"data\":\"" + base64Image + "\"}}"
                    + "]"
                    + "}]"
                    + "}";
    return jsonData;
  }

  // Sends a POST request to the Gemini API
  IEnumerator HTTPPost(string url, string bodyJsonString)
  {
    var request = new UnityWebRequest(url, "POST");
    Debug.Log("Gemini bodyJsonString: " + bodyJsonString);
    byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
    Debug.Log("Gemini bodyRaw: " + bodyRaw);

    request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
    request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
    //request.SetRequestHeader("Accept", "application/json");
    request.SetRequestHeader("Content-Type", "application/json");

    Debug.Log("Gemini SendWebRequest ");
    yield return request.SendWebRequest();
    Debug.Log("Gemini SendWebRequest done ");

    if (request.result != UnityWebRequest.Result.Success)
    {
      Debug.LogError($"Gemini Error: {request.error}");
    }
    else
    {
      Debug.Log($"Gemini Raw Response: {request.downloadHandler.text}");
      // Deserialize JSON response
      GeminiAPIResponse response = JsonUtility.FromJson<GeminiAPIResponse>(request.downloadHandler.text);

      Debug.Log($"Gemini Response: {response}");

      // Extract and log the text content from the first candidate's first part
      if (response.candidates != null && response.candidates.Length > 0 && response.candidates[0].content.parts != null && response.candidates[0].content.parts.Length > 0)
      {
        string extractedText = response.candidates[0].content.parts[0].text;
        Debug.Log($"Gemini --- Extracted Text: {extractedText}");
        yield return extractedText;
      }
      else
      {
        Debug.Log("Gemini --- No text content found in the response.");
        yield return "Gemini No text content found in the response.";
      }
    }
  }


  // method for running the initial query
  public IEnumerator RunInitialImageQuery(string initialPrompt = "Don't answer as complete sentence! Name/brand + model/type/species? (no size information)")
  {

    //create JsonString
    // string initialPrompt = "Don't answer as complete sentence! Name/brand + model/type/species? (no size information)";
    var bodyJsonString = CreateJsonData(initialPrompt, Texture2DImageOfObject);

    Debug.Log("HTTP bodyJsonString: " + bodyJsonString);

    CoroutineWithOutput cd = new CoroutineWithOutput(this, HTTPPost(queryURL, bodyJsonString));
    yield return cd.coroutine;
    Debug.Log("GEMINI cd.output = " + cd.output);  //  'success' or 'fail'

    if (cd.output != null)
    {
      transform.GetComponent<SetupObjectProxy>().updateMetadata(cd.output.ToString());
      Debug.Log("GEMINI cd.output.ToString() = " + cd.output.ToString());
    }

    //StartCoroutine(HTTPPost("https://httpbin.org/post", bodyJsonString));
  }

  // method for running the initial query but with a output callback
  public IEnumerator RunInitialImageQueryWithOutput(string prompt, System.Action<string> callback)
  {

    Debug.Log("COMPARE ---1 ");

    //create JsonString
    // string initialPrompt = "Don't answer as complete sentence! Name/brand + model/type/species? (no size information)";
    var bodyJsonString = CreateJsonData(prompt, Texture2DImageOfObject);

    Debug.Log("COMPARE RunInitialImageQueryWithOutput HTTP bodyJsonString: " + bodyJsonString);


    CoroutineWithOutput cd = new CoroutineWithOutput(this, HTTPPost(queryURL, bodyJsonString));
    yield return cd.coroutine;
    Debug.Log("COMPARE RunInitialImageQueryWithOutput result is " + cd.output);  //  'success' or 'fail'

    callback(cd.output.ToString());

  }

  // method for running a FOLLOW-UP query
  public IEnumerator RunFollowUpImageQuery(string prompt, System.Action<string> callback)
  {

    //create JsonString
    var bodyJsonString = CreateJsonData(prompt, Texture2DImageOfObject);

    Debug.Log("HTTP bodyJsonString: " + bodyJsonString);


    CoroutineWithOutput cd = new CoroutineWithOutput(this, HTTPPost(queryURL, bodyJsonString));
    yield return cd.coroutine;
    Debug.Log("Result is " + cd.output);  //  'success' or 'fail'

    callback(cd.output.ToString());

  }

  IEnumerator HTTPGet(string testURL)
  {

    using (UnityWebRequest www = UnityWebRequest.Get(testURL))
    {
      yield return www.SendWebRequest();

      if (www.isNetworkError || www.isHttpError)
      {
        Debug.Log(www.error);
      }
      else
      {
        Debug.Log("Get Request Completed!");
        Debug.Log("HTTP GET :" + www.downloadHandler.text);
        // Debug.Log("HTTP GET :" + www.d);
      }
    }
  }

}
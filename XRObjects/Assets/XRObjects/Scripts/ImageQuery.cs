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
// using System.Web;

public class ImageQuery : MonoBehaviour
{
  [HideInInspector] public Texture2D Texture2DImageOfObject;

  // variable to store the object ID specific to the object
  [HideInInspector] public string objectConversationID;

  // set up data structure for the query JSON - this is for the initial call
  [Serializable]
  public class ImageQueryRequestJSON
  {
    public string text;
    public model_specJson[] model_spec;
    [Serializable]
    public class model_specJson
    {
      public string modelKey;
      public string modelId;
    }

    public string inference_hints;

    public image_sourceJson[] image_source;
    [Serializable]
    public class image_sourceJson
    {
      public string raw_image;
    }
  }

  // set up data structure for returned Json Response
  [Serializable]
  public class ImageQueryResponseJSON
  {
    // public List<AnnotateImageResponse> responses;
    public String text;
    public String conversationId;
    public String conversationLogs;
    public float latencySec;
    public String previousLog;
  }

  // set up data structure for the FOLLOW-UP queries (once you know the conversation ID)
  [Serializable]
  public class ImageFollowUpQueryRequestJSON
  {
    public string text;
    public model_specJson[] model_spec;
    [Serializable]
    public class model_specJson
    {
      public string modelKey;
      public string modelId;
    }

    public string inference_hints;
    public string conversation_id;

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
  void Start()
  {
    // test if internet connection works
    // StartCoroutine(HTTPGet("https://www.google.com/"));
    // StartCoroutine(HTTPPost("https://httpbin.org/post", ""));

  }

  // method for running the initial query
  public IEnumerator RunInitialImageQuery(string initialPrompt = "Don't answer as complete sentence! Name/brand + model/type/species? (no size information)")
  {

    //create JsonString
    // string initialPrompt = "Don't answer as complete sentence! Name/brand + model/type/species? (no size information)";
    var bodyJsonString = CreateInitialJsonData(initialPrompt, Texture2DImageOfObject);

    Debug.Log("HTTP bodyJsonString: " + bodyJsonString);


    CoroutineWithOutput cd = new CoroutineWithOutput(this, HTTPPost("https://nlu-service.sandbox.googleapis.com/run_model_sync", bodyJsonString));
    yield return cd.coroutine;
    Debug.Log("result is " + cd.output);  //  'success' or 'fail'

    if (cd.output != null)
    {
      transform.GetComponent<SetupObjectProxy>().updateMetadata(cd.output.ToString());
    }

  }


  // method for running the initial query but with a output callback
  public IEnumerator RunInitialImageQueryWithOutput(string prompt, System.Action<string> callback)
  {

    //create JsonString
    // string initialPrompt = "Don't answer as complete sentence! Name/brand + model/type/species? (no size information)";
    var bodyJsonString = CreateInitialJsonData(prompt, Texture2DImageOfObject);

    // Debug.Log("COMPARE RunInitialImageQueryWithOutput HTTP bodyJsonString: " + bodyJsonString);

    CoroutineWithOutput cd = new CoroutineWithOutput(this, HTTPPost("https://nlu-service.sandbox.googleapis.com/run_model_sync", bodyJsonString));
    yield return cd.coroutine;
    // Debug.Log("COMPARE RunInitialImageQueryWithOutput result is " + cd.output);  //  'success' or 'fail'

    callback(cd.output.ToString());

  }

  // method for running a FOLLOW-UP query
  public IEnumerator RunFollowUpImageQuery(string prompt, System.Action<string> callback)
  {
    //create JsonString
    var bodyJsonString = CreateFollowUpJsonData(prompt);

    //Debug.Log("HTTP bodyJsonString: " + bodyJsonString);

    CoroutineWithOutput cd = new CoroutineWithOutput(this, HTTPPost("https://nlu-service.sandbox.googleapis.com/run_model_sync", bodyJsonString));
    yield return cd.coroutine;
    Debug.Log("result is " + cd.output);  //  'success' or 'fail'

    callback(cd.output.ToString());

  }

  // call for the first query (without knowing conversationId)
  public string CreateInitialJsonData(string promptText, Texture2D Texture2DImageOfObject)
  {
    string jsonData;

    // encode image as JPEG -> base64
    // Texture2DImageOfObject.SetPixels(pixels);
    // texture2D.Apply(false); // Not required. Because we do not need to be uploaded it to GPU
    byte[] ImageJPG = Texture2DImageOfObject.EncodeToJPG();
    string ImageBase64 = System.Convert.ToBase64String(ImageJPG);

    var _imageQueryRequestJSON = new ImageQueryRequestJSON()
    {

      // text = "What do you see in this photo? Give a short answer, not a full sentence. Do not put a period at the end of the answer. If you see a product, you can mention its brand, for example: San Pellegrino Naturali Aranciata sparkling drink",
      text = promptText,
      model_spec = new[]
        {
                // If the number of arrays increases, add more
                new ImageQueryRequestJSON.model_specJson()
                {
                modelKey =  "mms_extractor",
                modelId = "model_mms_fresh"
                }
            },
      inference_hints = "use_pali_vqa=True",
      image_source = new[]
        {
                // If the number of arrays increases, add more
                new ImageQueryRequestJSON.image_sourceJson()
                {
                raw_image =  ImageBase64 //image_bytes
                }
            }

    };

    jsonData = JsonUtility.ToJson(_imageQueryRequestJSON, false); // false = pretty print
    Debug.Log(jsonData);
    return jsonData;
  }

  // call for the FOLLOW-UP queries (when conversationId is known)
  public string CreateFollowUpJsonData(string promptText)
  {
    string jsonData;

    // encode image as JPEG -> base64
    // Texture2DImageOfObject.SetPixels(pixels);
    // texture2D.Apply(false); // Not required. Because we do not need to be uploaded it to GPU
    byte[] ImageJPG = Texture2DImageOfObject.EncodeToJPG();
    string ImageBase64 = System.Convert.ToBase64String(ImageJPG);

    var _imageQueryRequestJSON = new ImageFollowUpQueryRequestJSON()
    {

      // text = "What do you see in this photo? Give a short answer, not a full sentence. Do not put a period at the end of the answer. If you see a product, you can mention its brand, for example: San Pellegrino Naturali Aranciata sparkling drink",
      text = promptText,
      model_spec = new[]
        {
                // If the number of arrays increases, add more
                new ImageFollowUpQueryRequestJSON.model_specJson()
                {
                modelKey =  "mms_extractor",
                modelId = "model_mms_fresh"
                }
            },
      inference_hints = "use_pali_vqa=True",
      conversation_id = objectConversationID
    };

    jsonData = JsonUtility.ToJson(_imageQueryRequestJSON, false); // false = pretty print
    Debug.Log(jsonData);
    return jsonData;
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


  IEnumerator HTTPPost(string url, string bodyJsonString)
  {
    var request = new UnityWebRequest(url, "POST");

    // get rid of [ and ] from the string
    bodyJsonString = bodyJsonString.Replace("[", string.Empty);
    bodyJsonString = bodyJsonString.Replace("]", string.Empty);

    Debug.Log("HTTPS Cleaned Json: " + bodyJsonString);

    byte[] bodyRaw = System.Text.Encoding.Default.GetBytes(bodyJsonString);

    request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
    request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

    request.SetRequestHeader("Accept", "application/json");
    request.SetRequestHeader("Content-Type", "application/json");

    yield return request.SendWebRequest();

    var returnedJson = request.downloadHandler.text;
    Debug.Log("HTTPS request.responseCode: " + request.responseCode);
    Debug.Log("HTTPS request.text: " + returnedJson);
    Debug.Log("HTTPS request.text shorter:" + returnedJson.Replace("\n", "")); //.Replace(" ", ""));

    ImageQueryResponseJSON returnedJsonDeserialized = JsonUtility.FromJson<ImageQueryResponseJSON>(returnedJson);

    // get the LLM response text
    var responseLLM = returnedJsonDeserialized.text;
    objectConversationID = returnedJsonDeserialized.conversationId;

    // transform.GetComponent<SetupObjectProxy>().updateMetadata(response);

    if (string.IsNullOrEmpty(request.error))
    {
      Debug.Log("HTTPS success");
      yield return responseLLM;
    }
    else
    {
      Debug.Log("HTTPS error!");
      yield return "error";
    }

  }


}
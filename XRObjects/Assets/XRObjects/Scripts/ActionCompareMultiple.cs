// Copyright 2024 Google LLC

// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

// Class for comparing the identified objects (by stitching together their cropped images)
public class ActionCompareMultiple : ActionClass
{

  [SerializeField] private GameObject infoDisplayPanel;
  private Button compareButton;
  private string queryResultText = "listening...";
  private string transcribedUserPrompt = "n/a";
  private bool infoDisplayActive = false, questionQueryOngoing = false;

  private string objectsNameList = "";

  public Material Material0; // object not selected
  public Material Material1; // object metadata available
  public Material Material2; // object selected

  public Material MaterialCorrect; // object marked because it is the answer of the query

  void Start()
  {
    compareButton = this.gameObject.GetComponent<Button>();
    compareButton.onClick.AddListener(executeComparison);
  }

  private void executeComparison()
  {
    // if (!infoDisplayActive)
    // {
    if (infoDisplayPanel.GetComponent<InfoPanelManager>().currentAction != this.gameObject || !infoDisplayPanel.GetComponentInChildren<Image>().enabled)
    {

      // close all submenus
      Component[] gridLayoutGroups = GetComponentInParent<RadialLayout>().gameObject.GetComponentsInChildren<GridLayoutGroup>();
      foreach (Component gridLayoutGroup in gridLayoutGroups)
      {
        gridLayoutGroup.gameObject.SetActive(false);
      }

      // infoDisplayPanel.GetComponent<InfoPanelManager>().currentAction = this.gameObject;

      // set ObjectComparer's ImageQuery as the stitched image
      SetObjectComparerImage();

      // RunSearch();
      askQuestion();

      infoDisplayPanel.GetComponentInChildren<Button>(true).onClick.AddListener(askQuestion);

      if (transcribedUserPrompt == "n/a")
      {
        infoDisplayPanel.GetComponentInChildren<Button>(true).gameObject.SetActive(false);
      }
      else
      {
        infoDisplayPanel.GetComponentInChildren<Button>(true).gameObject.SetActive(true);
      }


      infoDisplayPanel.GetComponentInChildren<Image>().enabled = true;
      infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().enabled = true;
      infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().text = queryResultText;
    }
    else
    {

      infoDisplayPanel.GetComponentInChildren<Button>(true).gameObject.SetActive(false);
      infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().enabled = false;
      infoDisplayPanel.GetComponentInChildren<Image>().enabled = false;
    }




    infoDisplayPanel.GetComponent<InfoPanelManager>().currentAction = this.gameObject;
    infoDisplayActive = !infoDisplayActive;

    UnmarkAllObjects();
  }


  public void SetObjectComparerImage()
  {

    GameObject[] realObjectProxies = GameObject.FindGameObjectsWithTag("RealObjectSphere");
    List<Texture2D> texture2DList = new List<Texture2D>();


    // reset objectsNameList
    objectsNameList = "";
    for (int idx = 0; idx < realObjectProxies.Length; idx++)
    {

      GameObject realObject = realObjectProxies[idx];
      objectsNameList += "[" + (idx) + "] " + realObject.GetComponentInParent<SetupObjectProxy>().objectTitle + ", ";


      texture2DList.Add(realObject.GetComponentInParent<ImageQuery>().Texture2DImageOfObject);
    }

    // Remove all characters that are NOT letters, numbers, or square brackets
    // (quotes were creating problems when included in the prompt)
    objectsNameList = objectsNameList.Replace("\"", "");
    // objectsNameList = Regex.Replace(objectsNameList, "[^a-zA-Z0-9\\[\\]]", "");

    Debug.Log("WHICH OBJECTS objectsNameList" + objectsNameList);

    var stitchedTexture = StitchTexturesHorizontally(texture2DList.ToArray());

    if (stitchedTexture != null)
    {
      GameObject.Find("ObjectComparer").GetComponent<ImageQuery>().Texture2DImageOfObject = stitchedTexture;

      RawImage imagePreview = GameObject.FindWithTag("ObjectComparerImagePreview")?.GetComponent<RawImage>();
      if (imagePreview != null) {
        imagePreview.texture = stitchedTexture;
      }
    }



  }

  public Texture2D StitchTexturesHorizontally(Texture2D[] textures)
  {
    if (textures == null || textures.Length == 0)
    {
      Debug.LogError("Error: No textures available for stitching");
      return null;
    }

    int totalWidth = 0, maxHeight = 0;

    // calculate total width and maximum height among textures
    foreach (Texture2D tex in textures)
    {
      totalWidth += tex.width;
      if (tex.height > maxHeight) { maxHeight = tex.height; }
    }

    // create a new Texture2D for the stitched result
    Texture2D stitchedTexture = new Texture2D(totalWidth, maxHeight);

    int xOffset = 0;

    // iterate through each texture and copy its pixels to the stitched texture
    foreach (Texture2D tex in textures)
    {
      int width = tex.width;
      int height = tex.height;

      Color[] pixels = tex.GetPixels();

      // set pixels in the stitched texture
      stitchedTexture.SetPixels(xOffset, 0, width, height, pixels);
      xOffset += width;
    }

    // apply changes and return the stitched texture
    stitchedTexture.Apply();
    return stitchedTexture;
  }

  // initial query for general comparison
  public void RunSearch()
  {

    var prompt = "In only 1-2 sentences compare the objects/products here by searching the Internet and summarizing key facts and differences (in only 30 words).";
    // because this gameobject might be deactivated, attach the Coroutine to the MainCamera which will always be active
    MonoBehaviour cameraMono = Camera.main.GetComponent<MonoBehaviour>();
    _ = cameraMono.StartCoroutine(GameObject.Find("ObjectComparer").GetComponent<ImageQuery>().RunInitialImageQueryWithOutput(prompt, (result) =>
    {
      //Do something with the result variable
      Debug.Log("RunInitialImageQueryWithOutput HTTP RunSearch result: " + result);
      queryResultText = result.ToString();

      // don't show it if another process is ongoing
      if (questionQueryOngoing)
      {
        return;
      }

      infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().text = queryResultText;
      // questionQueryOngoing = false;

    }));

  }

  // custom user question
  private void askQuestion()
  {
    questionQueryOngoing = true;

    infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().text = "listening...";
    GameObject.Find("SpeechRecognizer").GetComponent<SpeechRecognizer>().StartListeningAndDisplay(infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>(), this);
  }

  public override void onTranscriptionFinished(string speechTranscribedText)
  {
    transcribedUserPrompt = speechTranscribedText;

    Debug.Log("HTTPS onTranscriptionFinished ");
    infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().text = "“<b>" + speechTranscribedText + "?</b>”\n\nthinking...";

    RunFollowUpSearch();
  }

  public void RunFollowUpSearch()
  {
    MonoBehaviour cameraMono = Camera.main.GetComponent<MonoBehaviour>();

    if (transcribedUserPrompt == "")
    {
      questionQueryOngoing = false;
    }

    string compare_pre_prompt = @$"You are powering an interactive tool called XR-Objects. 
                                    The user has provided you with an image of multiple objects and is asking a question. 
                                    
                                    Here is the list of the objects: {objectsNameList}.
                                    
                                    No need to mention index numbers in your response. 
                                    Keep your response extremely (extremely!) brief and concise - just a few words, maximum 1-2 sentences. 
                                    Don't say things like 'Based on the image…' or 'It looks like…'. 
                                    Use the internet as needed to help answer the question to the best of your ability. 
                                    
                                    Here is their query: ";
    string combinedPrompt = compare_pre_prompt + transcribedUserPrompt; 

    // _ = cameraMono.StartCoroutine(GameObject.Find("ObjectComparer").GetComponentInParent<ImageQuery>().RunFollowUpImageQuery(transcribedUserPrompt + "? No need to mention index numbers. Mention the object name.", (result) =>
    _ = cameraMono.StartCoroutine(GameObject.Find("ObjectComparer").GetComponentInParent<ImageQuery>().RunInitialImageQueryWithOutput(combinedPrompt, (result) =>
    {
      //Do something with the result variable
      Debug.Log("HTTP RunSearch result: " + result);
      queryResultText = result.ToString();
      infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().text = queryResultText;

      // add the "ask question" button
      infoDisplayPanel.GetComponentInChildren<Button>(true).GetComponentInChildren<TextMeshProUGUI>(true).text = "ask another question";
      infoDisplayPanel.GetComponentInChildren<Button>(true).gameObject.SetActive(true);

      questionQueryOngoing = false;

      Debug.Log("HTTP transcribedUserPrompt: " + transcribedUserPrompt);

      if (transcribedUserPrompt.ToLower().StartsWith("which"))
      {
        //var promptWhich = "Considering that the items are ordered from left to right with the first one being index 0, tell me ONLY the correct index, written as number characters: " + transcribedUserPrompt + "?   Absolutely don't give the indices of the other ones.";
        // var promptWhich = "Considering that the items are ordered from left to right with the first one being index 0, tell me ONLY the correct index, written as number characters: " + transcribedUserPrompt + "?  Only say the CORRECT indices. For example, only say '0' or '0 and 1'.";
        var promptWhich = @$"You are powering an interactive tool called XR-Objects. 
                            The user has provided you with an image of multiple objects and is asking a question.
                                    
                            Here is a list of the items and their indices:  {objectsNameList}.

                            Considering that these items in the image are ordered from left to right with the first one being index 0, 
                            answer the user's question by telling me ONLY the correct indecies written as a comma seperated list. 
                            For example: '0' or '0,3'.
                            
                            IMPORTANT: Do NOT include any other words in your response other than this list. 
                            Use the internet as needed to help answer the question to the best of your ability. 
                            
                            Here is the user's question: {transcribedUserPrompt}";

        RunFollowUpSearchWhich(promptWhich);
      }

    }));
    Debug.Log("HTTP RunSearch ended");

  }

  public void RunFollowUpSearchWhich(string promptWhich)
  {
    Debug.Log("HTTP onTranscriptionFinished RunSearch");
    //StartCoroutine(newContainer.GetComponent<ImageQuery>().RunFollowUpImageQuery("What is this?"));

    MonoBehaviour cameraMono = Camera.main.GetComponent<MonoBehaviour>();
    _ = cameraMono.StartCoroutine(GameObject.Find("ObjectComparer").GetComponentInParent<ImageQuery>().RunFollowUpImageQuery(promptWhich, (result) =>
    {
      //Do something with the result variable
      Debug.Log("HTTP RunSearch result: " + result);
      var correctObjectList = result.ToString();

      Debug.Log("WHICH OBJECTS IS STARTING");
      MarkObjectAfterComparison(correctObjectList);

    }));
    Debug.Log("HTTP RunSearch ended");

  }

  public void MarkObjectAfterComparison(string correctObjectList)
  {
    // correctObjectList comes in the form like "Only Energy Milk (2) has lactose."
    // adjusted that this probably now returns indices

    Debug.Log("WHICH OBJECTS 1 reply: " + correctObjectList);

    // Check if the input contains " than ", if so take what comes before
    int indexToCrop = correctObjectList.IndexOf(" than ");
    if (indexToCrop != -1)
    {
      // Return the substring before " than "
      correctObjectList = correctObjectList.Substring(0, indexToCrop);
    }

    // first get all objects images and stitch them together horizontally
    GameObject[] realObjectProxies = GameObject.FindGameObjectsWithTag("RealObjectSphere");
    // List<Texture2D> texture2DList = new List<Texture2D>();

    for (int idx = 0; idx < realObjectProxies.Length; idx++)
    {

      // GameObject realObject = realObjectProxies[idx].GetComponentInParent<SetupObjectProxy>().gameObject.transform.Find("Sphere").gameObject;
      GameObject realObject = realObjectProxies[idx];

      if (correctObjectList.Contains(idx.ToString()))
      {
        realObject.GetComponent<MeshRenderer>().material = MaterialCorrect;
      }
      else
      {
        realObject.GetComponent<MeshRenderer>().material = Material0;
      }

    }

  }

  public void UnmarkAllObjects() {
    // first get all objects images and stitch them together horizontally
    GameObject[] realObjectProxies = GameObject.FindGameObjectsWithTag("RealObjectSphere");
    // List<Texture2D> texture2DList = new List<Texture2D>();

    for (int idx = 0; idx < realObjectProxies.Length; idx++)
    {
      GameObject realObject = realObjectProxies[idx];
      realObject.GetComponent<MeshRenderer>().material = Material0;
      }
  }

}

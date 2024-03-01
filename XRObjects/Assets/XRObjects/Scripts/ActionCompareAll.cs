// Copyright 2024 Google LLC

// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Class for comparing all objects (the whole screenshot of the scene)
// Note: due to the limitations of LLMs, it is instead better to use
// the other script (ActionCompareMultiple), which crops and combines
// pre-detected objects for the image query
public class ActionCompareAll : ActionClass
{
  // public TextMeshProUGUI searchPanel;
  // public GameObject mainObjectProxy;
  [SerializeField] private GameObject infoDisplayPanel;
  private Button compareButton;
  private string queryResultText;
  private string transcribedUserPrompt;
  private bool infoDisplayActive = false;

  void Start()
  {
    compareButton = this.gameObject.GetComponent<Button>();
    compareButton.onClick.AddListener(startImageCapture);
  }

  private void startImageCapture()
  {
    // if (!infoDisplayActive)
    // if (infoDisplayPanel.GetComponent<InfoPanelManager>().currentAction != this.gameObject || !infoDisplayPanel.GetComponentInChildren<Image>().enabled)
    // {

      // GameObject.Find("ObjectComparer").GetComponent<CaptureXRCamera>().captureDue = true;

      GameObject.Find("ObjectComparer").GetComponent<CaptureXRCamera>().CaptureNextTime(this);

      // close all submenus
      Component[] gridLayoutGroups = GetComponentInParent<RadialLayout>().gameObject.GetComponentsInChildren<GridLayoutGroup>();
      foreach (Component gridLayoutGroup in gridLayoutGroups)
      {
        gridLayoutGroup.gameObject.SetActive(false);
      }

      infoDisplayPanel.GetComponentInChildren<Image>().enabled = true;
      infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().enabled = true;
      infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().text = "analyzing...";

    // }
    // else
    // {
    //   infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().enabled = false;
    //   infoDisplayPanel.GetComponentInChildren<Image>().enabled = false;
    // }

   infoDisplayPanel.GetComponent<InfoPanelManager>().currentAction = this.gameObject;
   infoDisplayActive = !infoDisplayActive;

  }

  public void RunSearch(Texture2D image)
  {
    // Debug.Log("COMPARE 1");
    //StartCoroutine(newContainer.GetComponent<ImageQuery>().RunFollowUpImageQuery("What is this?"));

    // GameObject.Find("ObjectComparer").GetComponent<ImageQuery>().RunInitialImageQuery(this);

    GameObject.Find("ObjectComparer").GetComponent<ImageQuery>().Texture2DImageOfObject = image;

    // add the "ask question" button
    infoDisplayPanel.GetComponentInChildren<Button>(true).gameObject.SetActive(true);
    infoDisplayPanel.GetComponentInChildren<Button>(true).onClick.AddListener(askQuestion);
    infoDisplayPanel.GetComponentInChildren<Button>(true).GetComponentInChildren<TextMeshProUGUI>(true).text = "ask question";

    // Debug.Log("COMPARE image wh: " + image.width + " " + image.height);
    // Debug.Log("COMPARE image: " + image);
    // Debug.Log("COMPARE GameObject.Find(ObjectComparer): " + GameObject.Find("ObjectComparer"));
    // Debug.Log("COMPARE GameObject.Find(ObjectComparer).GetComponent<ImageQuery>(): " + GameObject.Find("ObjectComparer").GetComponent<ImageQuery>());

    var prompt = "In only 1-2 sentences compare the objects/products here by searching the Internet and summarizing key facts and differences";

    // because this gameobject might be deactivated, attach the Coroutine to the MainCamera which will always be active
    MonoBehaviour cameraMono = Camera.main.GetComponent<MonoBehaviour>();
    _ = cameraMono.StartCoroutine(GameObject.Find("ObjectComparer").GetComponent<ImageQuery>().RunInitialImageQueryWithOutput(prompt, (result) =>
    {
      //Do something with the result variable
      Debug.Log("RunInitialImageQueryWithOutput HTTP RunSearch result: " + result);
      queryResultText = result.ToString();
      infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().text = queryResultText;

    }));
  }

  private void askQuestion(){
    
    // turn off the button
    // infoDisplayPanel.GetComponentInChildren<Button>(true).gameObject.SetActive(false);

    // infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().fontSize = 3f;
    // infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

    // infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().enabled = false;
    // infoDisplayPanel.GetComponentInChildren<Image>().enabled = false;
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
    _ = cameraMono.StartCoroutine(GameObject.Find("ObjectComparer").GetComponentInParent<ImageQuery>().RunFollowUpImageQuery(transcribedUserPrompt, (result) =>
    {
      //Do something with the result variable
      Debug.Log("HTTP RunSearch result: " + result);
      queryResultText = result.ToString();
      infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().text = queryResultText;

    }));
    //Debug.Log("HTTP RunSearch ended");
  }

  public override void onImageCaptureFinished(Texture2D image)
  {
    //transcribedUserPrompt = speechTranscribedText;
    Debug.Log("CaptureCamera onImageCaptureFinished ");
    // infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().text = "“<b>" + speechTranscribedText + "?</b>”\n\nthinking...";
    
    RunSearch(image);

  }






  // // Update is called once per frame
  // void Update()
  // {

  // }

}

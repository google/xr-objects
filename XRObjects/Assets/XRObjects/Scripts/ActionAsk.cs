// Copyright 2024 Google LLC

// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class ActionAsk : ActionClass
{
  [SerializeField] private GameObject infoDisplayPanel;
  private Button askButton;
  private string transcribedUserPrompt, queryResultText;
  private bool infoDisplayActive = false;

  void Start()
  {
    askButton = this.gameObject.GetComponent<Button>();
    askButton.onClick.AddListener(startSpeechRecognition);
  }

  private void startSpeechRecognition()
  {

    // if (!infoDisplayActive || !infoDisplayPanel.GetComponentInChildren<Image>().enabled) // todo: track which mainAction was last selected
    if (infoDisplayPanel.GetComponent<InfoPanelManager>().currentAction != this.gameObject || !infoDisplayPanel.GetComponentInChildren<Image>().enabled)
    {

      infoDisplayPanel.GetComponentInChildren<Button>(true).gameObject.SetActive(false);

      // close all submenus
      Component[] gridLayoutGroups = GetComponentInParent<RadialLayout>().gameObject.GetComponentsInChildren<GridLayoutGroup>();
      foreach (Component gridLayoutGroup in gridLayoutGroups)
      {
        gridLayoutGroup.gameObject.SetActive(false);
      }

      infoDisplayPanel.GetComponentInChildren<Image>().enabled = true;
      infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().enabled = true;
      infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().text = "listening...";
      GameObject.Find("SpeechRecognizer").GetComponent<SpeechRecognizer>().StartListeningAndDisplay(infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>(), this);

    }
    else
    {
      infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().enabled = false;
      infoDisplayPanel.GetComponentInChildren<Image>().enabled = false;
    }

    infoDisplayPanel.GetComponent<InfoPanelManager>().currentAction = this.gameObject;
    infoDisplayActive = !infoDisplayActive;

  }

  public override void onTranscriptionFinished(string speechTranscribedText)
  {
    //Debug.Log("onTranscriptionFinished");

    transcribedUserPrompt = speechTranscribedText;
    infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().text = "“<b>" + speechTranscribedText + "?</b>”\n\nthinking...";

    RunSearch();

  }

  public void RunSearch()
  {
    _ = StartCoroutine(GetComponentInParent<ImageQuery>().RunFollowUpImageQuery(transcribedUserPrompt, (result) =>
    {
      //Do something with the result variable
      Debug.Log("RunSearch result: " + result);
      queryResultText = result.ToString();
      infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().text = queryResultText;

    }));

  }



}

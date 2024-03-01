// Copyright 2024 Google LLC

// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Class for adding a spatial note
public class ActionAddNote : ActionClass
{

  [SerializeField] private GameObject infoDisplayPanel;
  private Button actionButton;
  private string transcribedUserNote;
  private bool infoDisplayActive = false; // note display

  void Start()
  {
    actionButton = this.gameObject.GetComponent<Button>();
    actionButton.onClick.AddListener(startSpeechRecognition);
  }

  private void startSpeechRecognition()
  {
    // hide "add" panel
    GetComponentInParent<ActionWithSubmenu>().toggleMenuVisibility();

    // if (!infoDisplayActive)
    // {
    infoDisplayPanel.GetComponentInChildren<Button>(true).gameObject.SetActive(false);

    infoDisplayPanel.GetComponentInChildren<Image>().enabled = true;
    infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().enabled = true;
    infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().text = "add new note:<br><b>listening...</b>";
    GameObject.Find("SpeechRecognizer").GetComponent<SpeechRecognizer>().StartListeningAndDisplay(infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>(), this);

    // }
    // else
    // {
    //   infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().enabled = false;
    //   infoDisplayPanel.GetComponentInChildren<Image>().enabled = false;
    // }

    infoDisplayPanel.GetComponent<InfoPanelManager>().currentAction = this.gameObject;
    infoDisplayActive = !infoDisplayActive;

  }

  public override void onTranscriptionFinished(string speechTranscribedText)
  {
    transcribedUserNote = speechTranscribedText;
    Debug.Log("HTTPS onTranscriptionFinished ");
    addNote();

  }

  private void addNote()
  {
    // show the text in the info panel
    // infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().fontSize = 4.5f;
    infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().text = "<size=150%>" + transcribedUserNote + "</size>";
    // hide the rest of the UI
    // GetComponentInParent<SetupObjectProxy>().gameObject;
    GetComponentInParent<SetupObjectProxy>().deselectObject();

    // but then show the note
    infoDisplayPanel.SetActive(true);


  }


}

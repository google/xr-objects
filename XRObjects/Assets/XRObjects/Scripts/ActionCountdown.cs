// Copyright 2024 Google LLC

// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Class for attaching a countdown timer on the object

public class ActionCountdown : ActionClass
{
  // public TextMeshProUGUI searchPanel;
  // public GameObject mainObjectProxy;
  [SerializeField] private GameObject infoDisplayPanel;
  private Button actionButton;
  //   private string transcribedUserNote;
  private bool infoDisplayActive = false; // note display

  private bool timerActive = false;

  private int totalDuration = 0;

  void Start()
  {
    actionButton = this.gameObject.GetComponent<Button>();
    actionButton.onClick.AddListener(setupDuration);
  }

  private void setupDuration()
  {

    infoDisplayPanel.GetComponentInChildren<Button>(true).gameObject.SetActive(false);

    infoDisplayPanel.GetComponentInChildren<Image>().enabled = true;
    infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().enabled = true;
    infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().text = "how long?:<br><b>listening...</b>";
    GameObject.Find("SpeechRecognizer").GetComponent<SpeechRecognizer>().StartListeningAndDisplay(infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>(), this);

    // close all submenus
    Component[] gridLayoutGroups = GetComponentInParent<RadialLayout>().gameObject.GetComponentsInChildren<GridLayoutGroup>();
    foreach (Component gridLayoutGroup in gridLayoutGroups)
    {
      gridLayoutGroup.gameObject.SetActive(false);
    }

  }

  public override void onTranscriptionFinished(string speechTranscribedText)
  {
    totalDuration = convertToSeconds(speechTranscribedText);

    if(totalDuration==0){
      infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().text = "couldn't pick that up :(";
      return;
    }
    setupTimer();

  }

  private void setupTimer()
  {
    // hide "add" panel
    GetComponentInParent<ActionWithSubmenu>().toggleMenuVisibility();

    infoDisplayPanel.GetComponent<InfoPanelManager>().currentAction = this.gameObject;

    timerActive = true;
    // make the panel visible
    infoDisplayPanel.GetComponentInChildren<Image>().enabled = true;
    infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().enabled = true;
    // infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().fontSize = 5.5f;
    infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
    // add the "stop timer" button
    infoDisplayPanel.GetComponentInChildren<Button>(true).gameObject.SetActive(true);
    infoDisplayPanel.GetComponentInChildren<Button>(true).onClick.AddListener(stopTimer);
    infoDisplayPanel.GetComponentInChildren<Button>(true).GetComponentInChildren<TextMeshProUGUI>(true).text = "stop timer";

    MonoBehaviour cameraMono = Camera.main.GetComponent<MonoBehaviour>();
    _ = cameraMono.StartCoroutine(runTimer());

    GetComponentInParent<SetupObjectProxy>().deselectObject();

    // but then show the note
    infoDisplayPanel.SetActive(true);

  }

  private int convertToSeconds(string input)
  {
    int totalSeconds = 0;
    string[] parts = input.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

    try
    {
      for (int i = 0; i < parts.Length; i += 2)
      {
        // assuming the format is always "[number] [unit]"
        int value = int.Parse(parts[i]);
        string unit = parts[i + 1].ToLower();

        if (unit.StartsWith("minute"))
        {
          totalSeconds += value * 60;
        }
        else if (unit.StartsWith("second"))
        {
          totalSeconds += value;
        }
        else
        {
          Debug.LogError("Unknown time unit: " + unit);
        }
      }
    }
    catch (System.Exception e)
    {
      Debug.LogError("Error parsing input string: " + e.Message);
      return 0;
    }

    return totalSeconds + 1; // add 1 extra second on start
  }

  private IEnumerator runTimer()
  {
    float timer = 0f;
    // var timerText = 

    while (timerActive)
    {
      // convert the timer value to minutes and seconds
      int minutes = Mathf.FloorToInt((totalDuration - timer) / 60f);
      int seconds = Mathf.FloorToInt((totalDuration - timer) % 60f);

      infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().text = "<size=183%>" + string.Format("{0:00}:{1:00}", minutes, seconds) + "</size>";

      timer += Time.deltaTime;

      yield return null;
    }

  }

  private void stopTimer()
  {
    timerActive = false;

    infoDisplayPanel.GetComponentInChildren<Button>(true).gameObject.SetActive(false);

    // infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().fontSize = 3f;
    infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

    infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().enabled = false;
    infoDisplayPanel.GetComponentInChildren<Image>().enabled = false;

  }

}

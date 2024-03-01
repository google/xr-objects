// Copyright 2024 Google LLC

// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Class for creating a stopwatch (not a countdown timer, unlike the script name)

public class ActionTimer : ActionClass
{
  [SerializeField] private GameObject infoDisplayPanel;
  private Button actionButton;
  //   private string transcribedUserNote;
  private bool infoDisplayActive = false; // note display

  private bool timerActive = false;

  void Start()
  {
    actionButton = this.gameObject.GetComponent<Button>();
    actionButton.onClick.AddListener(setupTimer);
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

    // hide the rest of the UI
    // GetComponentInParent<SetupObjectProxy>().gameObject;
    GetComponentInParent<SetupObjectProxy>().deselectObject();

    // but then show the note
    infoDisplayPanel.SetActive(true);

  }

  private IEnumerator runTimer()
  {
    float timer = 0f;

    while (timerActive)
    {
      // convert the timer value to minutes and seconds
      int minutes = Mathf.FloorToInt(timer / 60f);
      int seconds = Mathf.FloorToInt(timer % 60f);

      infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().text = "<size=183%>" + string.Format("{0:00}:{1:00}", minutes, seconds) + "</size>";

      timer += Time.deltaTime;

      yield return null;
    }

  }

  private void stopTimer(){
    timerActive = false;

    infoDisplayPanel.GetComponentInChildren<Button>(true).gameObject.SetActive(false);

    // infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().fontSize = 3f;
    infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

    infoDisplayPanel.GetComponentInChildren<TextMeshProUGUI>().enabled = false;
    infoDisplayPanel.GetComponentInChildren<Image>().enabled = false;

  }

}

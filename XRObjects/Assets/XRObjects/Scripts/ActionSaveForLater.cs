// Copyright 2024 Google LLC

// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Class for adding an object to a dummy list

public class ActionSaveForLater : ActionClass
{
  [SerializeField] private GameObject infoDisplayPanel;
  private Button actionButton;
  private bool infoDisplayActive = false; // note display

  private GameObject popupMessagePanel;

  void Start()
  {
    actionButton = this.gameObject.GetComponent<Button>();
    actionButton.onClick.AddListener(sendMessage);

    popupMessagePanel = GameObject.Find("MessagePanel");
  }

  private void sendMessage()
  {
    // hide other panels
    GetComponentInParent<ActionWithSubmenu>().toggleMenuVisibility();
    // GetComponentInParent<ActionWithSubmenu>().gameObject.transform.parent.GetComponentInParent<ActionWithSubmenu>().toggleMenuVisibility();

    // change the sender name of the popup
    var senderName = this.GetComponentInChildren<TextMeshProUGUI>().text;
    var messageText = "";

    if (senderName == "save for later")
    {
      messageText = "Saved for later!";
    }
    else if (senderName == "set a reminder")
    {
      messageText = "Reminder set for tomorrow!";
    }
    else if (senderName == "add to cart")
    {
      messageText = "<b>Added to cart</b>: \n" + GetComponentInParent<SetupObjectProxy>().objectTitle;
    }

    Debug.Log("Saveforlater " + messageText);
    popupMessagePanel.GetComponentInChildren<TextMeshProUGUI>().text = messageText;
    Debug.Log("Saveforlater done");

    // because this gameobject might be deactivated, attach the Coroutine to the MainCamera which will always be active
    MonoBehaviour cameraMono = Camera.main.GetComponent<MonoBehaviour>();
    _ = cameraMono.StartCoroutine(showAndHidePopup());


  }


  // method for demoing sample messages
  IEnumerator showAndHidePopup()
  {
    yield return new WaitForSeconds(0.2f); // Wait for 3 seconds after button click

    CanvasGroup canvasGroup = popupMessagePanel.GetComponent<CanvasGroup>();

    if (canvasGroup != null)
    {
      // start with the popup fully transparent
      canvasGroup.alpha = 0f;

      // fade in the popup
      float timer = 0f;
      float duration = 0.5f; // duration of the fade-in
      while (timer < duration)
      {
        canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / duration);
        timer += Time.deltaTime;
        yield return null;
      }

      canvasGroup.alpha = 1f; // popup fully visible

      yield return new WaitForSeconds(1.5f); // wait for 4 seconds

      // fade out the popup
      timer = 0f;
      duration = 0.5f; // duration of the fade-out 
      while (timer < duration)
      {
        canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / duration);
        timer += Time.deltaTime;
        yield return null;
      }

      canvasGroup.alpha = 0f; // popup is  transparent

      // destroy the popup after fade-out
      // Destroy(popupInstance);

    }
  }

}

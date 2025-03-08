// Copyright 2024 Google LLC

// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.EventSystems;

// Class for setting up the virtual GameObject
// as the proxy for the detected real-world object

public class SetupObjectProxy : MonoBehaviour
{
  private bool objectIsSelected = false;
  public Material Material0; // object not selected
  public Material Material1; // object metadata available
  public Material Material2; // object selected

  public GameObject rectMenu, metadataMenu, circularMenu, panelInfoDisplay;

  private GameObject mainObjectProxy, circularPanel;
  public GameObject sphere;

  public string objectTitle;

  // variable to store the object ID specific to the object
  // public string objectConversationID;

  // Start is called before the first frame update
  void Start()
  {
    // turn off all Canvases in the beginning

    // turn off rect menu
    // rectMenu = transform.Find("UI/CanvasRectangular").gameObject;
    rectMenu.GetComponent<Canvas>().enabled = false;

    // turn off metadata menu
    // metadataMenu = transform.Find("UI/CanvasMetadata").gameObject;
    metadataMenu.GetComponent<Canvas>().enabled = false;

    // turn on circular menu
    // circularMenu = transform.Find("UI/CanvasCircular").gameObject;
    circularMenu.GetComponent<Canvas>().enabled = false;

    circularPanel = circularMenu.GetComponentInChildren<RadialLayout>().gameObject;

    panelInfoDisplay.GetComponentInChildren<TextMeshProUGUI>().enabled = false;
    panelInfoDisplay.GetComponentInChildren<Image>().enabled = false;

    mainObjectProxy = this.gameObject;

    // hide the button of ActionControl
    circularPanel.GetComponentInChildren<ActionControl>(true).enabled = false;
    circularPanel.GetComponentInChildren<ActionControl>(true).gameObject.SetActive(false);

  }

  // Update is called once per frame
  void Update()
  {
    // check if touching the object proxy
    // if ((Input.touchCount > 0) && (Input.GetTouch(0).phase == TouchPhase.Began))
    // check if a touch exists an it's not an a UI element (e.g. button)
    if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began && !IsPointerOverUIObject())
    {

      // RaycastHit _raycastHit;
      Ray raycast = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);


      RaycastHit[] _raycastHits;
      _raycastHits = Physics.RaycastAll(raycast, Mathf.Infinity);
      // sort by distance to hit the closest one
      System.Array.Sort(_raycastHits, (a, b) => a.distance.CompareTo(b.distance));

      foreach (var _raycastHit in _raycastHits)
      {

        // check if the sphere has been touched
        if (_raycastHit.collider.gameObject == sphere)
        { //_raycastHit.collider.CompareTag("RealObjectSphere") &&
          // object has been touched, let's change material (color)

          if (objectIsSelected)
          {
            // object was selected previously, let's deselect
            //transform.Find("Sphere").GetComponent<MeshRenderer>().material = Material0;
            _raycastHit.collider.GetComponent<MeshRenderer>().material = Material0;
            circularMenu.GetComponent<Canvas>().enabled = false;
            metadataMenu.GetComponent<Canvas>().enabled = false;

            objectIsSelected = false;
          }
          else
          {
            // object wasn't selected previously, let's select it now
            //transform.Find("Sphere").GetComponent<MeshRenderer>().material = Material2;
            _raycastHit.collider.GetComponent<MeshRenderer>().material = Material2;
            circularMenu.GetComponent<Canvas>().enabled = true;
            circularPanel.SetActive(true);
            metadataMenu.GetComponent<Canvas>().enabled = true;

            objectIsSelected = true;
          }
        }
      }


    }
  }

  private bool IsPointerOverUIObject()
  {
    if (EventSystem.current.IsPointerOverGameObject())
      return true;

    for (int touchIndex = 0; touchIndex < Input.touchCount; touchIndex++)
    {
      Touch touch = Input.GetTouch(touchIndex);
      if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
        return true;
    }

    return false;
  }

  public void deselectObject()
  {
    sphere.GetComponent<MeshRenderer>().material = Material0;
    // circularMenu.GetComponent<Canvas>().enabled = false;
    circularPanel.SetActive(false);
    metadataMenu.GetComponent<Canvas>().enabled = false;

    objectIsSelected = false;

  }

  string GetSubstringAfter(string originalString, string removeAfter)
  {
    int index = originalString.IndexOf(removeAfter);
    if (index != -1) // If the substring exists in the string
    {
      // Get the substring that comes after the substring
      return originalString.Substring(index + removeAfter.Length);
    }
    else
    {
      // The substring doesn't exist, return the original string
      return originalString;
    }
  }
  public void updateMetadata(string newMetadata)
  {
    // var textObjectName = transform.Find("UI/CanvasMetadata/textObjectName");
    // textObjectName.GetComponent<TextMeshProUGUI>().text = newMetadata;

    // cover cases like "This is a lemonade"
    newMetadata = GetSubstringAfter(newMetadata, " is a ");
    newMetadata = GetSubstringAfter(newMetadata, " is an ");
    newMetadata = GetSubstringAfter(newMetadata, " is ");

    // if the LLM didn't give a suitable result, delete the GameObject
    var newMetadataFirstWord = newMetadata.Split(' ').FirstOrDefault();

    if (newMetadata == "Unsuitable" || newMetadata == "Error" || newMetadata == "" || newMetadataFirstWord == "A" || newMetadataFirstWord == "The" || newMetadataFirstWord == "There" || newMetadataFirstWord == "1")
    {
      Destroy(mainObjectProxy);
      return;
    }

    // check if it is a widget, if so, then add the "control" menu
    if (newMetadata.Contains("Echo") || newMetadata.Contains("Daikin") || newMetadata.ToLower().Contains("speaker") || newMetadata.ToLower().Contains("thermostat") || newMetadata.ToLower().Contains("google home") || newMetadata.ToLower().Contains("google nest") || newMetadata.Contains("Nest"))
    {
      circularPanel.GetComponentInChildren<ActionControl>(true).gameObject.SetActive(true);
      // restart ActionControl script to rearrange the layout of actions
      circularPanel.GetComponentInChildren<ActionControl>(true).enabled = false;
      circularPanel.GetComponentInChildren<ActionControl>(true).enabled = true;

    }

    // check if the title has a dot at the end
    if (newMetadata.EndsWith("."))
    {
      // remove the dot at the end
      newMetadata = newMetadata.Substring(0, newMetadata.Length - 1);
    }

    // replace the text with the returned object name
    metadataMenu.GetComponentInChildren<TextMeshProUGUI>().text = newMetadata;

    objectTitle = newMetadata;

    if (!objectIsSelected)
    {
      // if object hasn't been selected yet, slightly change the color to indicate metadata
      // transform.Find("Sphere").GetComponent<MeshRenderer>().material = Material1;
      sphere.GetComponent<MeshRenderer>().material = Material1;
    }

    // run general Web Search after identifying the object
    mainObjectProxy.GetComponentInChildren<ActionSearch>().RunSearch();

  }

}

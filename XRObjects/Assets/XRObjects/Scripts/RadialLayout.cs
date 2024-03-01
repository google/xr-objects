/*
Radial Layout Group by Just a Pixel (Danny Goodayle) - http://www.justapixel.co.uk
Copyright (c) 2015

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

// 


using UnityEngine;
using UnityEngine.UI;

public class RadialLayout : LayoutGroup
{
  public float fDistance;
  [Range(0f, 360f)]
  // public float MinAngle, MaxAngle, StartAngle, fOffsetAngle;
  public float fOffsetAngle = 34f;
  private bool setupDone = false;
  protected override void OnEnable()
  {
    Debug.Log("Radial OnEnable");
    base.OnEnable();
    if (!setupDone)
    {
      CalculateRadial();
      setupDone = true;
    }
  }

  void Start()
  {
    Debug.Log("Radial Start");
    CalculateRadial();
    setupDone = true;
  }


  public override void SetLayoutHorizontal()
  {
  }
  public override void SetLayoutVertical()
  {
  }
  public override void CalculateLayoutInputVertical()
  {
    Debug.Log("Radial CalculateLayoutInputVertical");
    if (!setupDone)
    {
      CalculateRadial();
      setupDone = true;
    }

  }
  public override void CalculateLayoutInputHorizontal()
  {
    Debug.Log("Radial CalculateLayoutInputHorizontal");
    if (!setupDone)
    {
      CalculateRadial();
      setupDone = true;
    }
  }

#if UNITY_EDITOR
  protected override void OnValidate()
  {
    Debug.Log("Radial OnValidate"); base.OnValidate();
    CalculateRadial();
  }
#endif

  // public void CalculateRadial()
  // {
  //   m_Tracker.Clear();
  //   if (transform.childCount == 0)
  //     return;
  //   float fOffsetAngle = ((MaxAngle - MinAngle)) / (transform.childCount - 1);

  //   float fAngle = StartAngle;
  //   for (int i = 0; i < transform.childCount; i++)
  //   {
  //     RectTransform child = (RectTransform)transform.GetChild(i);
  //     if (child != null)
  //     {
  //       //Adding the elements to the tracker stops the user from modifiying their positions via the editor.
  //       m_Tracker.Add(this, child,
  //       DrivenTransformProperties.Anchors |
  //       DrivenTransformProperties.AnchoredPosition |
  //       DrivenTransformProperties.Pivot);
  //       Vector3 vPos = new Vector3(Mathf.Cos(fAngle * Mathf.Deg2Rad), Mathf.Sin(fAngle * Mathf.Deg2Rad), 0);
  //       child.localPosition = vPos * fDistance;
  //       //Force objects to be center aligned, this can be changed however I'd suggest you keep all of the objects with the same anchor points.
  //       child.anchorMin = child.anchorMax = child.pivot = new Vector2(0.5f, 0.5f);
  //       fAngle += fOffsetAngle;
  //     }
  //   }



  // }

  // void CalculateRadial()
  // {
  //   int activeChildCount = 0;
  //   for (int i = 0; i < transform.childCount; i++)
  //   {
  //     var child = transform.GetChild(i);
  //     if (child && child.gameObject.activeSelf)
  //     {
  //       activeChildCount++;
  //     }
  //   }


  //   m_Tracker.Clear();
  //   if (activeChildCount == 0)
  //     return;

  //   float sAngle = 360 / activeChildCount * (activeChildCount - 1);

  //   if (MinAngle > sAngle)
  //     MinAngle = sAngle;

  //   float fOffsetAngle = ((sAngle - MinAngle)) / (activeChildCount - 1);



  //   float fAngle = StartAngle;
  //   for (int i = 0; i < transform.childCount; i++)
  //   {
  //     RectTransform child = (RectTransform)transform.GetChild(i);
  //     if (child != null && child.gameObject.activeSelf)
  //     {
  //       //Adding the elements to the tracker stops the user from modifiying their positions via the editor.
  //       m_Tracker.Add(this, child,
  //           DrivenTransformProperties.Anchors |
  //           DrivenTransformProperties.AnchoredPosition |
  //           DrivenTransformProperties.Pivot);
  //       Vector3 vPos = new Vector3(Mathf.Cos(fAngle * Mathf.Deg2Rad), Mathf.Sin(fAngle * Mathf.Deg2Rad), 0);
  //       child.localPosition = vPos * fDistance;
  //       //Force objects to be center aligned, this can be changed however I'd suggest you keep all of the objects with the same anchor points.
  //       child.anchorMin = child.anchorMax = child.pivot = new Vector2(0.5f, 0.5f);
  //       fAngle += fOffsetAngle;
  //     }

  //   }

  // }

  public void CalculateRadial()
  {
    int activeChildCount = 0;
    for (int i = 0; i < transform.childCount; i++)
    {
      var child = transform.GetChild(i);
      if (child && child.gameObject.activeSelf)
      {
        activeChildCount++;
      }
    }

    m_Tracker.Clear();

    if (activeChildCount == 0){ return; }

    // float fOffsetAngle = ((MaxAngle - MinAngle)) / (activeChildCount - 1);

    // float fAngle = StartAngle;
    float fAngle = 90 - fOffsetAngle * (activeChildCount-1) / 2;

    for (int i = 0; i < transform.childCount; i++)
    {
      RectTransform child = (RectTransform)transform.GetChild(transform.childCount - i - 1);
      if (child != null && child.gameObject.activeSelf)
      {
        // Adding the elements to the tracker stops the user from modifiying their positions via the editor.
        m_Tracker.Add(this, child,
        DrivenTransformProperties.Anchors |
        DrivenTransformProperties.AnchoredPosition |
        DrivenTransformProperties.Pivot);
        Vector3 vPos = new Vector3(Mathf.Cos(fAngle * Mathf.Deg2Rad), Mathf.Sin(fAngle * Mathf.Deg2Rad), 0);
        child.localPosition = vPos * fDistance;
        // Force objects to be center aligned, this can be changed
        // however I'd suggest you keep all of the objects with the same anchor points.
        child.anchorMin = child.anchorMax = child.pivot = new Vector2(0.5f, 0.5f);
        fAngle += fOffsetAngle;
      }

    }

  }


}
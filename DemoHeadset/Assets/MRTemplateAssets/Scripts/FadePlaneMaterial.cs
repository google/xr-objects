using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class FadePlaneMaterial : MonoBehaviour
{
    // attached game object for fading
    public GameObject Plane;

    // fade speed length
    public float fadeSpeed = 1f;

    // fade alpha speed length
    public float fadeAlphaSpeed = 0.5f;

    // view radius
    public float viewRadius = 2.5f;

    // alpha
    public float alpha = 0.5f;

    Coroutine m_FadeDotsCoroutine;
    Coroutine m_FadeAlphaCoroutine;
    static readonly int k_DotViewRadius = Shader.PropertyToID("_DotViewRadius");
    static readonly int k_Alpha = Shader.PropertyToID("_Alpha");

    void Awake()
    {
        Renderer rend = Plane.transform.GetComponent<Renderer>();
        rend.material.SetFloat(k_DotViewRadius, 0f);
        rend.material.SetFloat(k_Alpha, 0f);
        FadePlane(true);
    }

    public void FadePlane(bool visible)
    {
        if (m_FadeDotsCoroutine != null)
            StopCoroutine(m_FadeDotsCoroutine);

        if (m_FadeAlphaCoroutine != null)
            StopCoroutine(m_FadeAlphaCoroutine);

        m_FadeAlphaCoroutine = StartCoroutine(FadeAlpha(visible));
        m_FadeDotsCoroutine = StartCoroutine(FadeDots(visible));
    }

    //Fade Coroutine
    public IEnumerator FadeDots(bool visible)
    {
        yield return new WaitForSeconds(fadeAlphaSpeed);

        Renderer rend = Plane.transform.GetComponent<Renderer>();
        float viewRadiusValue = rend.material.GetFloat(k_DotViewRadius);

        if (!visible)
        {
            //while loop to deincrement Alpha value until object is invisible
            while (rend.material.GetFloat(k_DotViewRadius) > 0f)
            {
                viewRadiusValue -= Time.deltaTime / fadeSpeed;
                rend.material.SetFloat(k_DotViewRadius, viewRadiusValue);
                yield return null;
            }
            rend.material.SetFloat(k_DotViewRadius, 0f);
        }
        else if (visible)
        {
            //while loop to increment object Alpha value until object is opaque
            while (rend.material.GetFloat(k_DotViewRadius) < viewRadius)
            {
                viewRadiusValue += Time.deltaTime / fadeSpeed;
                rend.material.SetFloat(k_DotViewRadius, viewRadiusValue);
                yield return null;
            }
            rend.material.SetFloat(k_DotViewRadius, viewRadius);
        }
    }

    public IEnumerator FadeAlpha(bool visible)
    {
        Renderer rend = Plane.transform.GetComponent<Renderer>();
        float alphaValue = rend.material.GetFloat(k_Alpha);

        if (!visible)
        {
            //while loop to deincrement Alpha value until object is invisible
            while (rend.material.GetFloat(k_Alpha) > 0f)
            {
                alphaValue -= Time.deltaTime / fadeAlphaSpeed;
                rend.material.SetFloat(k_DotViewRadius, alphaValue);
                yield return null;
            }
            rend.material.SetFloat(k_Alpha, 0f);
        }
        else if (visible)
        {
            //while loop to increment object Alpha value until object is opaque
            while (rend.material.GetFloat(k_Alpha) < alpha)
            {
                alphaValue += Time.deltaTime / fadeAlphaSpeed;
                rend.material.SetFloat(k_Alpha, alphaValue);
                yield return null;
            }
            rend.material.SetFloat(k_Alpha, alpha);
        }
    }
}

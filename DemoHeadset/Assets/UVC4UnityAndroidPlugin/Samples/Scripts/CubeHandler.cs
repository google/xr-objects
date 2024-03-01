//#define ENABLE_LOG
/*
 * Copyright (c) 2014 - 2022 t_saki@serenegiant.com 
 */

using System;
using UnityEngine;

using UnityEngine.EventSystems;

public class CubeHandler : MonoBehaviour,
	IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
	/**
	 * このスクリプトで操作するGameObject
	 * 未割り当ての場合はこのスクリプトがセットされているGameObjectを使う
	 */
	public GameObject TargetObject;

	private Transform taregtTransform;
	private Vector3 force = new Vector3();

	private const float FACTOR = 1.0f;
	private const float DECAY_RATE = 0.90f;

	// Start is called before the first frame update
	void Start()
	{	
		if (TargetObject == null)
		{   // TargetObjectが割り当てられていないときは
			// このスクリプトがセットされているゲームオブジェクトを使う
			TargetObject = gameObject;
		}
		taregtTransform = TargetObject.transform;
	}

//	// Update is called once per frame
//	void Update()
//	{
//
//	}

	void FixedUpdate()
	{
		// 回転させる
		taregtTransform.Rotate(force * Time.deltaTime, Space.World);
		// 減衰させる
		force *= DECAY_RATE;
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
	}

	public void OnDrag(PointerEventData eventData)
	{
		// 移動量に応じて角度計算
		float xAngle = eventData.delta.y * FACTOR;
		float yAngle = -eventData.delta.x * FACTOR;

		force += new Vector3(xAngle, yAngle, 0.0f);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
	}

	public void OnPointerClick(PointerEventData eventData)
	{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
		Console.WriteLine("OnClick:");
#endif
	}
}

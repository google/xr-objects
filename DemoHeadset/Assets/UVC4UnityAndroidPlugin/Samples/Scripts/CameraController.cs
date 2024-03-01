/*
 * Copyright (c) 2014 - 2022 t_saki@serenegiant.com 
 */
using Serenegiant;
using UnityEngine;
using UnityEngine.EventSystems;
using static Serenegiant.TouchEventManager;

public class CameraController : MonoBehaviour
{
	/**
	 * このスクリプトで操作するカメラ(GameObject)
	 * 未割り当ての場合はこのスクリプトがセットされているGameObjectを使う
	 */
	public GameObject TargetCamera;

	private TouchEventManager manager;
	private Transform taregtTransform;
	private Vector3 force = new Vector3();
	private Vector2 touchPosition = new Vector2();

	private const float FACTOR = 0.2f;
	private const float DECAY_RATE = 0.95f;
	private const float FORCE_LIMIT = 50;
	private const float FORCE_LIMIT2 = FORCE_LIMIT * FORCE_LIMIT;
	
	// Start is called before the first frame update
	void Start()
    {
		if (TargetCamera == null)
		{   // TargetObjectが割り当てられていないときは
			// このスクリプトがセットされているゲームオブジェクトを使う
			TargetCamera = gameObject;
		}
		taregtTransform = TargetCamera.transform;
		manager = new TouchEventManager();
	}

	// Update is called once per frame
	void Update()
    {
		manager.Update();

		var touch = manager.GetTouch();

		switch (touch.state) {
			case TouchState.Began:
				touchPosition.Set(touch.position.x, touch.position.y);
				break;
			case TouchState.Moved:
				var delta = touch.position - touchPosition;
				// 移動量に応じて角度計算
				float xAngle = delta.y * FACTOR;
				float yAngle = -delta.x * FACTOR;

				force.Set(xAngle, yAngle, 0.0f);
				if (force.sqrMagnitude > FORCE_LIMIT2)
				{
					force = force.normalized * FORCE_LIMIT;
				}
				break;
		}
	}

	void FixedUpdate()
	{
		// 回転させる
		taregtTransform.Rotate(force * Time.deltaTime, Space.World);
		// 減衰させる
		force *= DECAY_RATE;
	}

}

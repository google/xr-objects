//#define ENABLE_LOG
/*
 * Copyright (c) 2014 - 2022 t_saki@serenegiant.com 
 */

using UnityEngine;

namespace Serenegiant {
	public class TouchEventManager
	{
		public enum TouchState
		{
			// タッチ無し
			None = -1,
			// タッチ開始
			Began = TouchPhase.Began,
			// タッチ移動
			Moved = TouchPhase.Moved,
			// タッチ静止
			Stationary = TouchPhase.Stationary,
			// タッチ終了
			Ended = TouchPhase.Ended,
			// タッチキャンセル
			Canceled = TouchPhase.Canceled,
		}

		public class TouchEvent
		{
			public Vector2 position;
			public TouchState state;

			/**
			 * コンストラクタ
			 * @param touched
			 * @param position
			 * @param phase
			 */
			public TouchEvent(Vector2? position = null, TouchState state = TouchState.Began)
			{
				if (position == null)
				{
					this.position = new Vector2(0, 0);
				}
				else
				{
					this.position = (Vector2)position;
				}
				this.state = state;
			}

			/**
			 * コピーコンストラクタ
			 * @param other
			 */
			public TouchEvent(TouchEvent other)
			{
				this.position = new Vector2(other.position.x, other.position.y);
				this.state = other.state;
			}
		}

		private TouchEvent touchEvent = new TouchEvent();

		/**
		 * デフォルトコンストラクタ
		 */
		public TouchEventManager()
		{
		}

		/**
		 * タッチ状態の更新
		 * MonoBehaviourの下位クラスのUpdateから呼ぶこと
		 */
		public void Update()
		{
			touchEvent.state = TouchState.None;

			if (Application.isEditor)
			{   // エディタで実行中…マウスの状態でタッチイベントをシミュレートする
				if (Input.GetMouseButtonDown(0))
				{   // タッチしたとき
					touchEvent.state = TouchState.Began;
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				Console.WriteLine("タッチした:");
#endif
				}

				if (Input.GetMouseButtonUp(0))
				{   // 離したとき
					touchEvent.state = TouchState.Ended;
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				Console.WriteLine("タッチした:");
#endif
				}

				if (Input.GetMouseButton(0))
				{   // 押し続けているとき
					touchEvent.state = TouchState.Moved;
#if (!NDEBUG && DEBUG && ENABLE_LOG)
				Console.WriteLine("押し続けている");
#endif
				}

				if (touchEvent.state != TouchState.None)
				{   // タッチイベントがあるときは座標を取得
					touchEvent.position = Input.mousePosition;
				}
			}
			else
			{   // 実機で実行中
				if (Input.touchCount > 0)
				{
					Touch touch = Input.GetTouch(0);
					touchEvent.position = touch.position;
					touchEvent.state = (TouchState)touch.phase;
				}
			}
		}

		/**
		 * 現在のタッチ状態を取得
		 */
		public TouchEvent GetTouch()
		{
			return new TouchEvent(touchEvent);
		}
	}

} // namespace Serenegiant

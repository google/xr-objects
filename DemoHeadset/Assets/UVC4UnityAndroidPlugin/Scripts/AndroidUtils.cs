//#define ENABLE_LOG
/*
 * Copyright (c) 2014 - 2022 t_saki@serenegiant.com 
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

#if UNITY_ANDROID
#if UNITY_2018_3_OR_NEWER
using UnityEngine.Android;
#endif
#endif

namespace Serenegiant
{

	public class AndroidUtils : MonoBehaviour
	{
		public const string FQCN_UNITY_PLAYER = "com.unity3d.player.UnityPlayer";
		public const string PERMISSION_CAMERA = "android.permission.CAMERA";

		public enum PermissionGrantResult
		{
			PERMISSION_GRANT = 0,
			PERMISSION_DENY = -1,
			PERMISSION_DENY_AND_NEVER_ASK_AGAIN = -2
		}

		private const string TAG = "AndroidUtils#";
		private const string FQCN_PLUGIN = "com.serenegiant.androidutils.AndroidUtils";

		//--------------------------------------------------------------------------------
		/**
		 * ライフサイクルイベント用のデリゲーター
		 * @param resumed true: onResume, false: onPause
		 */
		public delegate void LifecycleEventHandler(bool resumed);

		/***
		 * GrantPermissionでパーミッションを要求したときのコールバック用delegateer
		 * @param permission
		 * @param grantResult 0:grant, -1:deny, -2:denyAndNeverAskAgain
		*/
		public delegate void OnPermission(string permission, PermissionGrantResult result);

		//--------------------------------------------------------------------------------
		/**
		 * パーミッション要求時のタイムアウト
		 */
		public static float PermissionTimeoutSecs = 30;
	
		public event LifecycleEventHandler LifecycleEvent;

		public static bool isPermissionRequesting;
		private static PermissionGrantResult grantResult;

		void Awake()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}Awake:");
#endif
#if UNITY_ANDROID
			Input.backButtonLeavesApp = true;   // 端末のバックキーでアプリを終了できるようにする
			Initialize();
#endif
		}

		//--------------------------------------------------------------------------------
		// Java側からのイベントコールバック

		/**
		 * onStartイベント
		 */
		public void OnStartEvent()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnStartEvent:");
#endif
		}

		/**
		 * onResumeイベント
		 */
		public void OnResumeEvent()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnResumeEvent:");
#endif
			LifecycleEvent?.Invoke(true);
		}

		/**
		 * onPauseイベント
		 */
		public void OnPauseEvent()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnPauseEvent:");
#endif
			LifecycleEvent?.Invoke(false);
		}

		/**
		 * onStopイベント
		 */
		public void OnStopEvent()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnStopEvent:");
#endif
		}

		/**
		 * パーミッションを取得できた
		 */
		public void OnPermissionGrant()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnPermissionGrant:");
#endif
			grantResult = PermissionGrantResult.PERMISSION_GRANT;
			isPermissionRequesting = false;
		}

		/**
		 * パーミッションを取得できなかった
		 */
		public void OnPermissionDeny()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnPermissionDeny:");
#endif
			grantResult = PermissionGrantResult.PERMISSION_DENY;
			isPermissionRequesting = false;
		}

		/**
		 * パーミッションを取得できずパーミッションダイアログを再び表示しないように設定された
		 */
		public void OnPermissionDenyAndNeverAskAgain()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}OnPermissionDenyAndNeverAskAgain:");
#endif
			grantResult = PermissionGrantResult.PERMISSION_DENY_AND_NEVER_ASK_AGAIN;
			isPermissionRequesting = false;
		}

		//--------------------------------------------------------------------------------
#if UNITY_ANDROID
		/**
		 * プラグインの初期化実行
		 */
		private void Initialize()
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}Initialize:{gameObject.name}");
#endif
			using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
			{
				clazz.CallStatic("initialize",
					AndroidUtils.GetCurrentActivity(), gameObject.name);
			}
		}

		/**
		 * 指定したパーミッションを保持しているかどうかを取得
		 * @param permission
		 * @param 指定したパーミッションを保持している
		 */
		public static bool HasPermission(string permission)
		{
			using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
			{
				return clazz.CallStatic<bool>("hasPermission",
					AndroidUtils.GetCurrentActivity(), permission);
			}
		}

		/**
		 * 指定したパーミッションの説明を表示する必要があるかどうかを取得
		 * @param permission
		 * @param 指定したパーミッションの説明を表示する必要がある
		 */
		public static bool ShouldShowRequestPermissionRationale(string permission)
		{
			using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
			{
				return clazz.CallStatic<bool>("shouldShowRequestPermissionRationale",
					AndroidUtils.GetCurrentActivity(), permission);
			}
		}

		/**
		 * パーミッション要求
		 * こっちはJava側でRationaleの処理等を行わない
		 * @param permission
		 * @param callback
		 */
		public static IEnumerator RequestPermission(string permission, OnPermission callback)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}GrantPermission:{permission}");
#endif
			if (!HasPermission(permission))
			{
				grantResult = PermissionGrantResult.PERMISSION_DENY;
				isPermissionRequesting = true;
				using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
				{
					clazz.CallStatic("requestPermission",
						AndroidUtils.GetCurrentActivity(), permission);
				}
				float timeElapsed = 0;
				while (isPermissionRequesting)
				{
					if ((PermissionTimeoutSecs > 0) && (timeElapsed > PermissionTimeoutSecs))
					{
						isPermissionRequesting = false;
						yield break;
					}
					timeElapsed += Time.deltaTime;
					yield return null;
				}
				callback(permission, grantResult);
			}
			else
			{
				callback(permission, PermissionGrantResult.PERMISSION_GRANT);
			}
	
			yield break;
		}

		/**
		 * パーミッション要求
		 * こっちはJava側でRationaleの処理等を行う
		 * @param permission
		 * @param callback
		 */
		public static IEnumerator GrantPermission(string permission, OnPermission callback)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}GrantPermission:{permission}");
#endif
			if (!HasPermission(permission))
			{
				grantResult = PermissionGrantResult.PERMISSION_DENY;
				isPermissionRequesting = true;
				using (AndroidJavaClass clazz = new AndroidJavaClass(FQCN_PLUGIN))
				{
					clazz.CallStatic("grantPermission",
						AndroidUtils.GetCurrentActivity(), permission);
				}
				float timeElapsed = 0;
				while (isPermissionRequesting)
				{
					if ((PermissionTimeoutSecs > 0) && (timeElapsed > PermissionTimeoutSecs))
					{
						isPermissionRequesting = false;
						yield break;
					}
					timeElapsed += Time.deltaTime;
						yield return null;
				}
				callback(permission, grantResult);
			}
			else
			{
				callback(permission, PermissionGrantResult.PERMISSION_GRANT);
			}
	
			yield break;
		}

		/**
		 * カメラパーミッションを要求
		 * @param callback
		 */
		public static IEnumerator GrantCameraPermission(OnPermission callback)
		{
#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}GrantCameraPermission:");
#endif
			if (CheckAndroidVersion(23))
			{
				// Android9以降ではUVC機器アクセスにもCAMERAパーミッションが必要
				yield return GrantPermission(PERMISSION_CAMERA, callback);
			}
			else
			{
				// Android 6 未満ではパーミッション要求処理は不要
				callback(PERMISSION_CAMERA, PermissionGrantResult.PERMISSION_GRANT);
			}

			yield break;
		}


		//================================================================================

		/**
		 * UnityPlayerActivityを取得
		 */
		public static AndroidJavaObject GetCurrentActivity()
		{
			using (AndroidJavaClass playerClass = new AndroidJavaClass(FQCN_UNITY_PLAYER))
			{
				return playerClass.GetStatic<AndroidJavaObject>("currentActivity");
			}
		}

		/**
		 * 指定したバージョン以降かどうかを確認
		 * @param apiLevel
		 * @return true: 指定したバージョン以降で実行されている, false: 指定したバージョンよりも古い端末で実行されている
		 */
		public static bool CheckAndroidVersion(int apiLevel)
		{
			using (var VERSION = new AndroidJavaClass("android.os.Build$VERSION"))
			{
				return VERSION.GetStatic<int>("SDK_INT") >= apiLevel;
			}
		}

#endif // #if UNITY_ANDROID

	} // class AndroidUtils

} // namespace Serenegiant


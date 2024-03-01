//#define ENABLE_LOG
/*
 * Copyright (c) 2014 - 2022 t_saki@serenegiant.com 
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Serenegiant.UVC
{
	/**
	 * UVC機器のフィルタ定義クラス
	 */
	[Serializable]
	public class UVCFilter
	{
		private const string TAG = "UVCFilter#";

		/**
		 * インスペクタでフィルターのコメントを表示するための文字列(スクリプトでは使わない)
		 */
		public string Description;
		/**
		 * マッチするベンダーID
		 * 0なら全てにマッチする
		 */
		public int Vid;
		/**
		 * マッチするプロダクトID
		 * 0なら全てにマッチする
		 */
		public int Pid;
		/**
		 * マッチする機器名
		 * null/emptyならチェックしない
		 */
		public string DeviceName;
		/**
		 * 除外フィルタとして扱うかどうか
		 */
		public bool IsExclude;

		//--------------------------------------------------------------------------------

		/**
		 * 引数のUVC機器にマッチするかどうかを取得
		 * @param device
		 */
		public bool Match(UVCDevice device)
		{
			bool result = device != null;

			if (result)
			{
				result &= ((Vid <= 0) || (Vid == device.vid))
					&& ((Pid <= 0) || (Pid == device.pid))
					&& (String.IsNullOrEmpty(DeviceName)
						|| DeviceName.Equals(device.name)
						|| DeviceName.Equals(device.name)
						|| (String.IsNullOrEmpty(device.name) || device.name.Contains(DeviceName))
						|| (String.IsNullOrEmpty(device.name) || device.name.Contains(DeviceName))
					);
			}

			return result;
		}

		//--------------------------------------------------------------------------------

		/**
		 * UVC機器のフィルタ処理用
		 * filtersがnullの場合はマッチしたことにする
		 * 除外フィルターにヒットしたときはその時点で評価を終了しfalseを返す
		 * 除外フィルターにヒットせず通常フィルターのいずれかにヒットすればtrueを返す
		 * @param device
		 * @param filters Nullable
		 */
		public static bool Match(UVCDevice device, List<UVCFilter> filters/*Nullable*/)
		{
			return Match(device, filters != null ? filters.ToArray() : (null as UVCFilter[]));
		}

		/**
		 * UVC機器のフィルタ処理用
		 * filtersがnullの場合はマッチしたことにする
		 * 除外フィルターにヒットしたときはその時点で評価を終了しfalseを返す
		 * 除外フィルターにヒットせず通常フィルターのいずれかにヒットすればtrueを返す
		 * @param device
		 * @param filters Nullable
		 */
		public static bool Match(UVCDevice device, UVCFilter[] filters/*Nullable*/)
		{
			var result = true;

			if ((filters != null) && (filters.Length > 0))
			{
				result = false;
				foreach (var filter in filters)
				{
					if (filter != null)
					{
						var b = filter.Match(device);
						if (b && filter.IsExclude)
						{   // 除外フィルターにヒットしたときはその時点でフィルタ処理を終了
							result = false;
							break;
						}
						else
						{   // どれか一つにヒットすればいい
							result |= b;
						}
					}
					else
					{
						// 空フィルターはマッチしたことにする
						result = true;
					}

				}
			}

#if (!NDEBUG && DEBUG && ENABLE_LOG)
			Console.WriteLine($"{TAG}Match({device}):result={result}");
#endif
			return result;
		}

	} // class UVCFilter

} // namespace Serenegiant.UVC

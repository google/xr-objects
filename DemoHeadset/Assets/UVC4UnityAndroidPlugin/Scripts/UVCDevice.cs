//#define ENABLE_LOG
/*
 * Copyright (c) 2014 - 2022 t_saki@serenegiant.com 
 */

using System;
using System.Runtime.InteropServices;

/*
 * THETA S  vid:1482, pid:1001
 * THETA V  vid:1482, pid:1002
 * THETA Z1 vid:1482, pid:1005
 */

namespace Serenegiant.UVC
{

	[Serializable]
	public class UVCDevice
	{
		public readonly Int32 id;
		public readonly int vid;
		public readonly int pid;
		public readonly int deviceClass;
		public readonly int deviceSubClass;
		public readonly int deviceProtocol;

		public readonly string name;

		public UVCDevice(IntPtr devicePtr) {
			id = GetId(devicePtr);
			vid = GetVendorId(devicePtr);
			pid = GetProductId(devicePtr);
			name = GetName(devicePtr);
			deviceClass = GetDeviceClass(devicePtr);
			deviceSubClass = GetDeviceSubClass(devicePtr);
			deviceProtocol = GetDeviceProtocol(devicePtr);
		}

		public override string ToString()
		{
			return $"{base.ToString()}(id={id},vid={vid},pid={pid},name={name},deviceClass={deviceClass},deviceSubClass={deviceSubClass},deviceProtocol={deviceProtocol})";
		}


		/**
		 * Ricohの製品かどうか
		 * @param info
		 */
		public bool IsRicoh
		{
			get { return (vid == 1482); }
		}

        /**
		 * THETA S/V/Z1のいずれかかどうか
		 */
        public bool IsTHETA
        {
            get { return IsTHETA_S || IsTHETA_V || IsTHETA_Z1; }
        }
   
		/**
		 * THETA Sかどうか
		 */
		public bool IsTHETA_S
		{
			get { return (vid == 1482) && (pid == 10001); }
		}

		/**
		 * THETA Vかどうか
		 */
		public bool IsTHETA_V
		{
			// THETA Vからのpid=872はUVCでなくて動かないので注意(THETA側は静止画/動画モード)
			get { return (vid == 1482) && (pid == 10002); }
		}

        /**
		 * THETA Z1かどうか
		 * @param info
		 */
        public bool IsTHETA_Z1
        {
            // THETA Z1からのpid=877はUVCではなくて動かないので注意(THETA側は静止画/動画モード)
            get { return (vid == 1482) && (pid == 10005); }
        }

        //--------------------------------------------------------------------------------
        // プラグインのインターフェース関数
        //--------------------------------------------------------------------------------
        /**
		 * 機器idを取得(これだけはpublicにする)
		 */
        [DllImport("unityuvcplugin", EntryPoint = "DeviceInfo_get_id")]
		public static extern Int32 GetId(IntPtr devicePtr);

		/**
			* デバイスクラスを取得
			*/
		[DllImport("unityuvcplugin", EntryPoint = "DeviceInfo_get_device_class")]
		private static extern Byte GetDeviceClass(IntPtr devicePtr);

		/**
			* デバイスサブクラスを取得
			*/
		[DllImport("unityuvcplugin", EntryPoint = "DeviceInfo_get_device_sub_class")]
		private static extern Byte GetDeviceSubClass(IntPtr devicePtr);

		/**
			* デバイスプロトコルを取得
			*/
		[DllImport("unityuvcplugin", EntryPoint = "DeviceInfo_get_device_protocol")]
		private static extern Byte GetDeviceProtocol(IntPtr devicePtr);

		/**
			* ベンダーIDを取得
			*/
		[DllImport("unityuvcplugin", EntryPoint = "DeviceInfo_get_vendor_id")]
		private static extern UInt16 GetVendorId(IntPtr devicePtr);

		/**
			* プロダクトIDを取得
			*/
		[DllImport("unityuvcplugin", EntryPoint = "DeviceInfo_get_product_id")]
		private static extern UInt16 GetProductId(IntPtr devicePtr);

		/**
			* 機器名を取得
			*/
		[DllImport("unityuvcplugin", EntryPoint = "DeviceInfo_get_name")]
		[return: MarshalAs(UnmanagedType.LPStr)]
		private static extern string GetName(IntPtr devicePtr);

	} // UVCDevice

} // namespace Serenegiant.UVC


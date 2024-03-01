/*
 * Copyright (c) 2014 - 2022 t_saki@serenegiant.com 
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Serenegiant.UVC
{

  /**
   * UVC関係のイベントハンドリングインターフェース
   */
  public interface IUVCDrawer
  {
    /**
     * UVC機器が接続された
     * @param manager 呼び出し元のUVCManager
     * @param device 接続されたUVC機器情報
     * @return true: UVC機器を使う, false: UVC機器を使わない
     */
    bool OnUVCAttachEvent(UVCManager manager, UVCDevice device);
    /**
     * UVC機器が取り外された
     * @param manager 呼び出し元のUVCManager
     * @param device 接続されたUVC機器情報
     */
    void OnUVCDetachEvent(UVCManager manager, UVCDevice device);
    /**
     * IUVCDrawerが指定したUVC機器の映像を描画できるかどうかを取得
     * @param manager 呼び出し元のUVCManager
     * @param device 接続されたUVC機器情報
     */
    bool CanDraw(UVCManager manager, UVCDevice device);
    /**
     * UVC機器からの映像取得を開始した
     * @param manager 呼び出し元のUVCManager
     * @param device 接続されたUVC機器情報
     * @param tex UVC機器からの映像を受け取るTextureオブジェクト
     */
    void OnUVCStartEvent(UVCManager manager, UVCDevice device, Texture tex);
    /**
     * UVC機器からの映像取得を終了した
     * @param manager 呼び出し元のUVCManager
     * @param device 接続されたUVC機器情報
     */
    void OnUVCStopEvent(UVCManager manager, UVCDevice device);

  }   // interface IUVCDrawer

}	// namespace Serenegiant.UVC

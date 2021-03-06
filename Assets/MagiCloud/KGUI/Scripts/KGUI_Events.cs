﻿using UnityEngine;
using System;
using UnityEngine.Events;

namespace MagiCloud.KGUI
{
    [Serializable]
    public class ButtonEvent : UnityEvent<int> { }
    [Serializable]
    public class EventObject : UnityEvent<GameObject> { }
    [Serializable]
    public class EventVector3 : UnityEvent<int, Vector3> { }

    [Serializable]
    public class EventFloat : UnityEvent<float> { }

    /// <summary>
    /// Button是否在区域内
    /// </summary>
    [Serializable]
    public class PanelEvent : UnityEvent<int, bool> { }

    [Serializable]
    public class ToggleEvent : UnityEvent<bool> { }

    /// <summary>
    /// Button组在切换Button时，重置事件触发
    /// </summary>
    [Serializable]
    public class ButtonGroupReset : UnityEvent<KGUI_ButtonBase> { }

    /// <summary>
    /// KGUI事件
    /// </summary>
    public class KGUI_Events
    {
        public static event Action<int, Camera, Ray> EventUIRay;

        /// <summary>
        /// 发送UI射线（该射线目前只支持自定义摄像机）
        /// </summary>
        /// <param name="ray"></param>
        public static void SendUIRay(int handIndex, Camera camera, Ray ray)
        {
            if (EventUIRay != null)
            {
                EventUIRay(handIndex, camera, ray);
            }
        }
    }
}
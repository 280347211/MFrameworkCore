﻿using UnityEngine;
using MagiCloud.Core.Events;

namespace MagiCloud.Core.MInput
{
    /// <summary>
    /// 手势(光标)
    /// </summary>
    public class MInputHand
    {
        private bool isPressed;
        private bool isEnable;

        private Vector3 currentPoint = Vector3.zero;//当前帧
        private Vector3? lastPoint;//上一帧

        private Vector3 lerpPoint = Vector3.zero; //差值帧

        private MInputHandStatus handStatus = MInputHandStatus.Idle;

        public IHandUI HandUI { get; private set; }

        /// <summary>
        /// 手势状态
        /// </summary>
        public MInputHandStatus HandStatus {
            get { return handStatus; }
            set { handStatus = value; }
        }

        /// <summary>
        /// 手平台（以便多平台操作）
        /// </summary>
        public OperatePlatform Platform { get; private set; }

        /// <summary>
        /// 手编号
        /// </summary>
        public int HandIndex { get; set; }

        /// <summary>
        /// 是否按下
        /// </summary>
        public bool IsPressed {
            get {
                return isPressed;
            }
        }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsEnable {
            get {
                return isEnable;
            }
            set {

                isEnable = value;

                isPressed = false;
                handStatus = MInputHandStatus.Idle;
                currentPoint = Vector3.zero;
                lastPoint = null;
                lerpPoint = Vector3.zero;

                if (HandUI != null)
                    HandUI.IsEnable = value;
            }
        }

        /// <summary>
        /// 屏幕坐标
        /// </summary>
        public Vector3 ScreenPoint {
            get {
                return currentPoint;
            }
        }

        /// <summary>
        /// 屏幕向量（当前帧与上一帧的差值）
        /// </summary>
        public Vector3 ScreenVector {
            get {
                return lerpPoint;
            }
        }

        public MInputHand(int handIndex,IHandUI handUI,OperatePlatform platform)
        {
            HandIndex = handIndex;
            HandUI = handUI;

            Platform = platform;
        }

        /// <summary>
        /// 每帧执行
        /// </summary>
        /// <param name="inputPoint">输入端世界坐标值</param>
        public void OnUpdate(Vector3 inputPoint)
        {
            if (!isEnable) return;

            currentPoint = inputPoint;

            if (lastPoint == null)
                lastPoint = currentPoint;

            //移动坐标
            if (HandUI != null)
                HandUI.MoveHand(ScreenPoint);

            //获取到射线
            Ray ray = MUtility.ScreenPointToRay(ScreenPoint);

            Ray uiRay = MUtility.UIScreenPointToRay(ScreenPoint);

            //发送射线信息
            EventHandRay.SendListener(ray, HandIndex);
            EventHandUIRay.SendListener(uiRay, HandIndex);
            EventHandRays.SendListener(ray, uiRay, HandIndex);

            //算直接坐标的插值
            lerpPoint = currentPoint - lastPoint.Value;

            //记录上一帧
            lastPoint = currentPoint;
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void SetGrip()
        {
            if (!isEnable) return;

            if (isPressed) return;

            isPressed = true;

            EventHandGrip.SendListener(HandIndex);

            handStatus = MInputHandStatus.Grip;
        }

        /// <summary>
        /// 抬起
        /// </summary>
        public void SetIdle()
        {
            if (!isEnable) return;

            if (!isPressed) return;
            isPressed = false;

            EventHandIdle.SendListener(HandIndex);

            handStatus = MInputHandStatus.Idle;
        }

        ///// <summary>
        ///// 设置被抓取
        ///// </summary>
        ///// <param name="func"></param>
        //public void SetGrab(System.Func<IOperateObject> func)
        //{
        //    //如果里面存在限制
        //    if (ActionConstraint.BindCount != 0 && ActionConstraint.IsBindOther(ActionConstraint.Grab_Action)) return;

        //    if (func == null) return;

        //    OperateObject = func();

        //    EventHandGrabObjectKey.SendListener(OperateObject.GrabObject, HandIndex);

        //    EventHandGrabObject.SendListener(OperateObject.GrabObject,HandIndex);

        //    handStatus = MInputHandStatus.Grab;
        //}

        ///// <summary>
        ///// 设置释放
        ///// </summary>
        ///// <param name="target"></param>
        //public void SetRelease(GameObject target)
        //{
        //    EventHandReleaseObjectKey.SendListener(target, HandIndex);
        //    EventHandReleaseObject.SendListener(target, HandIndex);

        //    handStatus = MInputHandStatus.Release;
        //}

        /// <summary>
        /// 设置手势大小
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="size"></param>
        public void SetHandIcon(Sprite icon, Vector2? size = null)
        {
            if (HandUI == null) return;

            HandUI.SetHandIcon(icon, size);
        }
    }
}


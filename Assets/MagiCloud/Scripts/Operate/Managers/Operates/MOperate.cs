﻿using System;
using MagiCloud.Core.MInput;
using System.Collections.Generic;
using MagiCloud.Core;
using UnityEngine;
using MagiCloud.Features;
using MagiCloud.Core.Events;

namespace MagiCloud
{
    /// <summary>
    /// 针对一个手势的处理
    /// </summary>
    public class MOperate
    {
        /// <summary>
        /// 与之关联的输入端手势
        /// </summary>
        public MInputHand InputHand;

        public UIOperate UIOperate;

        public Func<bool> RayExternaLimit;//射线外部限制

        private OperaObject operaObject; //操作物体，包含高亮、虚影、标签、能抓取、旋转、自定义等
        //操作物体
        public IOperateObject OperateObject; //只针对抓取

        /// <summary>
        /// 外部抓取移动操作
        /// </summary>
        public Action<IOperateObject, int> OnGrab; //外部抓取操作
        public Action<IOperateObject, int, float> OnSetGrab; //设置物体被抓取
        public Action<IOperateObject, int> OnGrabing;
        public Action<IOperateObject, int> OnError;
        public Action<IOperateObject, int> OnInvalid;

        public MOperate(MInputHand inputHand, Func<bool> func)
        {
            this.InputHand = inputHand;
            RayExternaLimit = func;
            UIOperate = new UIOperate(inputHand);
        }

        /// <summary>
        /// 激活
        /// </summary>
        public void OnEnable()
        {
            if (InputHand == null) return;

            EventHandRays.AddListener(OnRay, ExecutionPriority.High);
            EventHandIdle.AddListener(OnIdle, ExecutionPriority.High);

            InputHand.IsEnable = true;

            UIOperate.IsEnable = true;
        }

        /// <summary>
        /// 禁用
        /// </summary>
        public void OnDisable()
        {
            EventHandRays.RemoveListener(OnRay);
            EventHandIdle.RemoveListener(OnIdle);

            InputHand.IsEnable = false;

            UIOperate.IsEnable = false;
        }

        void OnIdle(int handIndex)
        {
            if (handIndex != InputHand.HandIndex) return;

            if (OperateObject != null)
            {
                SetObjectRelease(); //释放

            }
        }

        void OnRay(Ray ray, Ray uiRay, int handIndex)
        {
            if (handIndex != InputHand.HandIndex) return;

            RaycastHit hit;

            //限制处理
            if (RayExternaLimit != null && RayExternaLimit())
            {
                OnNoRayTarget();
            }
            //执行UI操作
            else if (UIOperate.OnUIRay(uiRay))
            {
                OnNoRayTarget();
            }
            //物体处理
            else if (Physics.Raycast(ray, out hit, 10000, 1 << MOperateManager.layerRay | 1 << MOperateManager.layerObject))
            {
                OnRayTarget(hit);
            }
            else
            {
                OnNoRayTarget();
            }
        }

        /// <summary>
        /// 没有照射到物体时的处理
        /// </summary>
        void OnNoRayTarget()
        {
            HideHighLight();
            HideLabel();

            if (InputHand.HandStatus == MInputHandStatus.Idle)
            {
                if (operaObject != null)
                {
                    EventHandRayTargetExit.SendListener(operaObject.FeaturesObject.gameObject, InputHand.HandIndex);
                }

                //OperateObject与operaObject对象是一样的，都指定的是同一个物体
                if (OperateObject != null)
                {
                    OperateObject.HandStatus = MInputHandStatus.Idle;
                }

                operaObject = null;
                OperateObject = null;
            }
        }

        /// <summary>
        /// 照射物体处理
        /// </summary>
        /// <param name="hit"></param>
        void OnRayTarget(RaycastHit hit)
        {
            EventHandRayTarget.SendListener(hit, InputHand.HandIndex);

            switch (InputHand.HandStatus)
            {
                case MInputHandStatus.Idle:

                    if (hit.collider == null)
                    {
                        OnNoRayTarget();
                        return;
                    }

                    if (operaObject != null && operaObject.gameObject == hit.collider.gameObject) return;

                    if (operaObject != null)
                    {
                        //处理之前的
                        OnNoRayTarget();
                    }

                    operaObject = hit.collider.GetComponent<OperaObject>();
                    EventHandRayTargetEnter.SendListener(operaObject.FeaturesObject.gameObject, InputHand.HandIndex);

                    if (operaObject == null) return;
                    ShowLabel();

                    //显示高亮
                    ShowHightLight(false);

                    break;
                case MInputHandStatus.Grip:

                    if (operaObject != null && operaObject.gameObject != hit.collider.gameObject) return;
                    HideLabel();

                    InputHand.HandStatus = MInputHandStatus.Grab;//将该手状态设置为抓取状态

                    break;
                case MInputHandStatus.Grab:
                    if (operaObject == null)
                    {
                        InputHand.HandStatus = MInputHandStatus.Idle;
                        return;
                    }

                    ShowHightLight(true);
                    HideLabel();

                    OperateObject = HandleGrab(operaObject.FeaturesObject.operaType);

                    if (OperateObject != null)
                    {
                        OperateObject.HandStatus = MInputHandStatus.Grabing;

                        EventHandGrabObjectKey.SendListener(OperateObject.GrabObject, InputHand.HandIndex);
                        EventHandGrabObject.SendListener(OperateObject.GrabObject, InputHand.HandIndex);
                    }

                    InputHand.HandStatus = MInputHandStatus.Grabing;

                    break;
                case MInputHandStatus.Invalid:
                    if (OnInvalid != null)
                        OnInvalid(OperateObject, InputHand.HandIndex);
                    break;
                case MInputHandStatus.Error:

                    if (OnError != null)
                    {
                        OnError(OperateObject, InputHand.HandIndex);
                    }

                    break;
                case MInputHandStatus.Grabing:
                    //不可操作中，表示正在有物体进行操作，不可进行其他操作
                    if (OnGrabing != null)
                    {
                        OnGrabing(OperateObject, InputHand.HandIndex);
                    }

                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 处理抓取
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private IOperateObject HandleGrab(ObjectOperaType type)
        {
            switch (type)
            {
                case ObjectOperaType.无:
                    break;
                case ObjectOperaType.能抓取:

                    //调用物体的抓取
                    var canGrab = operaObject.GetComponent<MCCanGrab>();

                    //直接返回
                    if (canGrab.HandStatus != MInputHandStatus.Idle) return null;

                    //不同的操作端具备不同的操作，所以应该让外部调用
                    if (OnGrab != null)
                    {
                        OnGrab(canGrab, InputHand.HandIndex);
                    }

                    return canGrab;
                case ObjectOperaType.物体自身旋转:

                    var rotation = operaObject.GetComponent<MCObjectRatation>();

                    //如果物体不是闲置状态，则直接返回
                    if (rotation.HandStatus != MInputHandStatus.Idle) return null;

                    rotation.OnOpen();

                    return rotation;
                case ObjectOperaType.自定义:

                    var customize = operaObject.GetComponent<MCustomize>();

                    if (customize.HandStatus != MInputHandStatus.Idle) return null;

                    customize.OnOpen(InputHand.HandIndex);

                    return customize;
                default:
                    break;
            }

            return null;
        }

        /// <summary>
        /// 松手时释放
        /// </summary>
        /// <param name="type"></param>
        private void HandleIdle(ObjectOperaType type)
        {
            switch (type)
            {
                case ObjectOperaType.自定义:

                    var customize = operaObject.GetComponent<MCustomize>();

                    if (customize != null)
                        customize.OnClose();

                    break;
                case ObjectOperaType.物体自身旋转:

                    var rotation = operaObject.GetComponent<MCObjectRatation>();
                    if (rotation != null)
                        rotation.OnClose();

                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 设置物体释放
        /// </summary>
        public void SetObjectRelease()
        {
            HandleIdle(operaObject.FeaturesObject.operaType);
            InputHand.HandStatus = MInputHandStatus.Idle;
            OperateObject.HandStatus = MInputHandStatus.Idle;

            EventHandReleaseObjectKey.SendListener(OperateObject.GrabObject, InputHand.HandIndex);
            EventHandReleaseObject.SendListener(OperateObject.GrabObject, InputHand.HandIndex);
        }

        /// <summary>
        /// 设置物体被抓取
        /// </summary>
        /// <param name="target"></param>
        /// <param name="zValue"></param>
        public void SetObjectGrab(GameObject target, float zValue)
        {
            var feature = target.GetComponent<FeaturesObjectController>();

            if (feature == null)
                throw new Exception("设置被抓取的物体不存在FeatureObjectController脚本");

            operaObject = feature.Opera;
            OperateObject = HandleGrab(feature.operaType);

            if (OperateObject != null)
            {
                OperateObject.HandStatus = MInputHandStatus.Grabing;

                //设置物体被抓取
                if (OnSetGrab != null)
                {
                    OnSetGrab(OperateObject, InputHand.HandIndex, zValue);
                }

                EventHandGrabObjectKey.SendListener(OperateObject.GrabObject, InputHand.HandIndex);
                EventHandGrabObject.SendListener(OperateObject.GrabObject, InputHand.HandIndex);
            }
        }

        /// <summary>
        /// 获取到被抓取的物体
        /// </summary>
        /// <returns></returns>
        public GameObject GetObjectGrab()
        {
            if (InputHand.HandStatus != MInputHandStatus.Grabing) return null;

            return OperateObject == null ? null : OperateObject.GrabObject;
        }

        #region Features

        /// <summary>
        /// 显示标签
        /// </summary>
        private void ShowLabel()
        {
            if (operaObject != null && operaObject.FeaturesObject.ActiveLabel)
                operaObject.FeaturesObject.AddLabel().label.OnEnter();
        }

        /// <summary>
        /// 隐藏标签
        /// </summary>
        private void HideLabel()
        {
            if (operaObject != null && operaObject.FeaturesObject.ActiveLabel)
                operaObject.FeaturesObject.AddLabel().label.OnExit();
        }

        /// <summary>
        /// 隐藏高亮
        /// </summary>
        private void HideHighLight()
        {
            if (operaObject != null && operaObject.FeaturesObject.ActiveHighlight && !InputHand.IsPressed)
            {
                operaObject.GetComponent<HighlightObject>().HideHighLight();
            }
        }

        /// <summary>
        /// 显示高亮
        /// </summary>
        private void ShowHightLight(bool isGrab)
        {
            //判断是否激活高亮
            if (operaObject != null && operaObject.FeaturesObject.ActiveHighlight)
            {
                operaObject.GetComponent<HighlightObject>().ShowHighLight(isGrab);
            }
        }

        #endregion
    }
}

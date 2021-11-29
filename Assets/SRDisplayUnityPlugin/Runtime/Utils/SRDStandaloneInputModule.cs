/*
 * Copyright 2019,2020,2021 Sony Corporation
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

using SRD.Core;

namespace SRD.Utils
{
    /// <summary>
    /// A <a href="https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.EventSystems.StandaloneInputModule.html">StandaloneInputModule</a> extended class for Spatial Reality Display.
    /// </summary>
    public class SRDStandaloneInputModule : StandaloneInputModule
    {
        private SRDManager _srdManager;
        private SRDCameras _srdCameras;

        private readonly MouseState m_MouseState = new MouseState();

        protected override void Start()
        {
            _srdManager = SRDSceneEnvironment.GetSRDManager();
            _srdCameras = new SRDCameras(_srdManager);
        }

        protected override MouseState GetMousePointerEventData(int id)
        {
            Debug.Assert(_srdManager != null);
            Debug.Assert(_srdCameras != null);

            // Populate the left button...
            PointerEventData leftData;
            var created = GetPointerData(kMouseLeftId, out leftData, true);

            leftData.Reset();

            if(created)
            {
                leftData.position = _srdCameras.ScreenToSRDScreen(input.mousePosition);
            }

            Vector2 pos = _srdCameras.ScreenToSRDScreen(input.mousePosition);

            if(Cursor.lockState == CursorLockMode.Locked)
            {
                // We don't want to do ANY cursor-based interaction when the mouse is locked
                leftData.position = new Vector2(-1.0f, -1.0f);
                leftData.delta = Vector2.zero;
            }
            else
            {
                leftData.delta = pos - leftData.position;
                leftData.position = pos;
            }

            leftData.scrollDelta = input.mouseScrollDelta;
            leftData.button = PointerEventData.InputButton.Left;
            eventSystem.RaycastAll(leftData, m_RaycastResultCache);
            var raycast = FindFirstRaycast(m_RaycastResultCache);
            raycast.screenPosition = _srdCameras.WorldToSRDScreenPoint(raycast.worldPosition);
            leftData.pointerCurrentRaycast = raycast;
            m_RaycastResultCache.Clear();
            // copy the apropriate data into right and middle slots
            PointerEventData rightData;
            GetPointerData(kMouseRightId, out rightData, true);
            CopyFromTo(leftData, rightData);
            rightData.button = PointerEventData.InputButton.Right;

            PointerEventData middleData;
            GetPointerData(kMouseMiddleId, out middleData, true);
            CopyFromTo(leftData, middleData);
            middleData.button = PointerEventData.InputButton.Middle;

            m_MouseState.SetButtonState(PointerEventData.InputButton.Left, StateForMouseButton(0), leftData);
            m_MouseState.SetButtonState(PointerEventData.InputButton.Right, StateForMouseButton(1), rightData);
            m_MouseState.SetButtonState(PointerEventData.InputButton.Middle, StateForMouseButton(2), middleData);

            return m_MouseState;
        }
    }
}

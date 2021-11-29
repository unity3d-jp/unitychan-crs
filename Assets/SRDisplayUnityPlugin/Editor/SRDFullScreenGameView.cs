/*
 * Copyright 2019,2020,2021 Sony Corporation
 */


using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

using SRD.Core;
using SRD.Utils;

namespace SRD.Editor
{
    internal class SRDFullScreenGameView
    {
        const string FullScreenMenuPath = "SpatialRealityDisplay/SRDisplay GameView (Full Screen)";
        private static EditorApplication.CallbackFunction OnPostClosingTempGameView;

#if UNITY_EDITOR_WIN
        [MenuItem(FullScreenMenuPath + " _F11", false, 2001)]
#endif
        public static void ExecuteFullScreen()
        {
            if(EditorApplication.isPlaying)
            {
                Debug.Log("SRDisplay GameView cannot be changed in Play Mode");
                return;
            }

            if (Menu.GetChecked(FullScreenMenuPath))
            {
                SRD.Editor.AsssemblyWrapper.GameView.CloseAllSRDGameView();
                Menu.SetChecked(FullScreenMenuPath, false);
            }
            else
            {
                // check whether SDK is available or not.
                SrdXrDeviceInfo[] devices = { new SrdXrDeviceInfo(), };
                if (SRDCorePlugin.EnumerateDevices(devices, 1) == SrdXrResult.ERROR_SYSTEM_INVALID)
                {
                    SRDCorePlugin.ShowMessageBox("Confirm", SRDHelper.SRDMessages.DLLNotFoundError,
                                                 Debug.LogWarning);
                    return;
                }
                if(!SRDSettings.LoadScreenRect())
                {
                    SRDCorePlugin.ShowMessageBox("Confirm", SRDHelper.SRDMessages.DisplayConnectionError,
                                                 Debug.LogWarning);
                    return;
                }
                if(IsWrongSettings())
                {
                    EditorApplication.update += RequestGameViewSize;
                    OnPostClosingTempGameView += SetupGameViewAfterCloseTempGameView;
                    Menu.SetChecked(FullScreenMenuPath, true);
                    return;
                }
                SRD.Editor.AsssemblyWrapper.GameView.CloseAllUnityGameView();
                SetupGameView();
                Menu.SetChecked(FullScreenMenuPath, true);
            }
        }

        static bool IsWrongSettings()
        {
            return PlayerSettings.defaultIsNativeResolution == true
                   || PlayerSettings.defaultScreenWidth != SRDSettings.DeviceInfo.ScreenRect.Width
                   || PlayerSettings.defaultScreenHeight != SRDSettings.DeviceInfo.ScreenRect.Height;
        }

        [InitializeOnLoadMethod]
        static void SetSceneInitializer()
        {
            EditorApplication.delayCall += () =>
            {
                if(SRD.Editor.AsssemblyWrapper.GameView.CountSRDGameView() == 0 || IsWrongSettings())
                {
                    EditorApplication.update += RequestGameViewSize;
                }
                EditorApplication.update += CloseUnityGameView;
            };
        }

        private static void CloseUnityGameView()
        {
            // normal gameviews only
            if(!Menu.GetChecked(FullScreenMenuPath))
            {
                if(EditorApplication.isPlaying)
                {
                    SRD.Editor.AsssemblyWrapper.GameView.TakeOneUnityGameView();
                }
            }
            // SRD GameView opened
            else
            {
                SRD.Editor.AsssemblyWrapper.GameView.CloseAllUnityGameView();
            }
        }

        private static void RequestSRDSizeGameView()
        {
            SRDSettings.LoadScreenRect();
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = SRDSettings.DeviceInfo.ScreenRect.Width;
            PlayerSettings.defaultScreenHeight = SRDSettings.DeviceInfo.ScreenRect.Height;
        }

        private static void RequestGameViewSize()
        {
            RequestSRDSizeGameView();
            // 正しくないGameViewが生成されたとしてもGameViewが存在しなければ
            // GameViewSizesが更新されないので生成する
            SetupGameView();

            EditorApplication.update -= RequestGameViewSize;
            EditorApplication.update += CloseTemporaryGameView;
        }

        private static void CloseTemporaryGameView()
        {
            // GameViewSizesを更新する仕事を終えたGameViewを閉じる
            SRD.Editor.AsssemblyWrapper.GameView.CloseAllSRDGameView();

            var destinationSize = SRDSettings.DeviceInfo.ScreenRect.Resolution;
            if(SRD.Editor.AsssemblyWrapper.GameViewSizeList.IsReadyDestinationSize(destinationSize))
            {
            }
            else
            {
                Debug.LogWarning("Fail to create destination size GameView. If you have a wrong size of SRDisplayGameView, please re-open SRDisplayGameView.");
            }
            EditorApplication.update -= CloseTemporaryGameView;
            if(OnPostClosingTempGameView != null)
            {
                OnPostClosingTempGameView.Invoke();
            }
        }

        private static void SetupGameView()
        {
            var gameView = new SRD.Editor.AsssemblyWrapper.GameView();
            gameView.position = SRDSettings.DeviceInfo.ScreenRect.Position;
            gameView.size = SRDSettings.DeviceInfo.ScreenRect.Resolution;
            gameView.scale = 1.0f;
            gameView.targetDisplay = 0;
            gameView.noCameraWarning = false;
            gameView.Apply();
        }

        private static void SetupGameViewAfterCloseTempGameView()
        {
            SRD.Editor.AsssemblyWrapper.GameView.CloseAllUnityGameView();
            SetupGameView();
            OnPostClosingTempGameView -= SetupGameViewAfterCloseTempGameView;
        }
    }
}

/*
 * Copyright 2019,2020 Sony Corporation
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SRD.Utils
{
    internal static partial class SRDHelper
    {
        public static class SRDMessages
        {
            private enum SRDMessageType
            {
                AppCloseMessage,
                DisplayConnectionError, DeviceConnectionError, USB3ConnectionError,
                DeviceNotFoundError, DLLNotFoundError,
                DisplayInterruptionError, DeviceInterruptionError, AppConflictionError,
                FullscreenGameViewError, SRDManagerNotFoundError
            }

            private static Dictionary<SRDMessageType, string> SRDMessagesDictEn = new Dictionary<SRDMessageType, string>()
            {
                {SRDMessageType.AppCloseMessage, "The application will be terminated."},
                {SRDMessageType.DisplayConnectionError, "Failed to detect SR Display. Make sure HDMI cable is connected correctly between PC and SR Display."},
                {SRDMessageType.DeviceConnectionError, "Failed to detect SR Display. Make sure USB 3.0 cable is connected correctly between PC and SR Display."},
                {
                    SRDMessageType.USB3ConnectionError, string.Join("\n", new string[]{
                        "SR Display is not recognized correctly. Please make sure SR Display and PC's USB 3.0 port are connected with USB3.0 cable. Also, please try following steps.",
                        "    1. Unplug USB cable from PC's USB 3.0 port.",
                        "    2. Turn SR Display's power off.",
                        "    3. Plug USB cable into PC's USB 3.0 port.",
                        "    4. Wait for 30 seconds.",
                        "    5. Turn SR Display's power on.",
                        "    6. Launch this application again.\n",
                    })
                },
                {SRDMessageType.DeviceNotFoundError, "Failed to find SR Display device. Make sure SR Display device is powered on."},
                {SRDMessageType.DLLNotFoundError, "SR Display SDK is not found. SR Display SDK may be not installed correctly. Try to re-install with SRD installer."},
                {SRDMessageType.DisplayInterruptionError, "HDMI connection has been interrupted."},
                {SRDMessageType.DeviceInterruptionError, "USB connection has been interrupted."},
                {SRDMessageType.AppConflictionError, "Another SR Display application is already running. Please close it and start this application again."},
                {SRDMessageType.FullscreenGameViewError, "Failed to detect SR Display. Make sure HDMI cable is connected correctly between PC and SR Display."},
                {SRDMessageType.SRDManagerNotFoundError, "No SRDManager. You must add active SRDManager for SR Display Apps."},
            };

            private static Dictionary<SRDMessageType, string> _messageDict;
            private static Dictionary<SRDMessageType, string> MessageDict
            {
                get
                {
                    if(_messageDict == null)
                    {
                        _messageDict = SRDMessagesDictEn;
                    }
                    return _messageDict;
                }
            }


            public static string AppCloseMessage
            {
                get { return MessageDict[SRDMessageType.AppCloseMessage]; }
            }
            public static string DisplayConnectionError
            {
                get { return MessageDict[SRDMessageType.DisplayConnectionError]; }
            }
            public static string DeviceConnectionError
            {
                get { return MessageDict[SRDMessageType.DeviceConnectionError]; }
            }
            public static string USB3ConnectionError
            {
                get { return MessageDict[SRDMessageType.USB3ConnectionError]; }
            }
            public static string DeviceNotFoundError
            {
                get { return MessageDict[SRDMessageType.DeviceNotFoundError]; }
            }
            public static string DisplayInterruptionError
            {
                get { return MessageDict[SRDMessageType.DisplayInterruptionError]; }
            }
            public static string DeviceInterruptionError
            {
                get { return MessageDict[SRDMessageType.DeviceInterruptionError]; }
            }
            public static string DLLNotFoundError
            {
                get { return MessageDict[SRDMessageType.DLLNotFoundError]; }
            }
            public static string AppConflictionError
            {
                get { return MessageDict[SRDMessageType.AppConflictionError]; }
            }

            public static string FullscreenGameView
            {
                get { return MessageDict[SRDMessageType.FullscreenGameViewError]; }
            }
            public static string SRDManagerNotFoundError
            {
                get { return MessageDict[SRDMessageType.SRDManagerNotFoundError]; }
            }
        }
    }
}

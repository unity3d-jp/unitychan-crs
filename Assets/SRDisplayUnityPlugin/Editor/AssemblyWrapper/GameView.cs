/*
 * Copyright 2019,2020 Sony Corporation
 */


using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;

namespace SRD.Editor.AsssemblyWrapper
{
    internal class GameViewSizeList
    {
        public static bool IsReadyDestinationSize(Vector2 destinationSize)
        {
            return FindDestinationIndex(destinationSize) != -1;
        }

        public static int FindDestinationIndex(Vector2 destinationSize)
        {
            var sizes = GetSizes();
            return sizes.FindIndex(size => size.width == destinationSize.x && size.height == destinationSize.y);
        }

        private static List<GameViewSnapshot.Size> GetSizes()
        {
            var source = typeof(UnityEditor.Editor).Assembly;
            // class GameViewSizes : ScriptableSingleton<GameViewSizes>
            var gameViewSizesType = source.GetType("UnityEditor.GameViewSizes");
            var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizesType);
            var gameViewSizes = singletonType.GetProperty("instance").GetValue(null);

            var currentGroupType = (GameViewSizeGroupType)gameViewSizes.GetType()
                                   .GetProperty("currentGroupType")
                                   .GetValue(gameViewSizes);
            var sizeGroup = gameViewSizes.GetType()
                            .GetMethod("GetGroup")
                            .Invoke(gameViewSizes, new object[] { (int)currentGroupType });
            var totalCount = (int)sizeGroup.GetType()
                             .GetMethod("GetTotalCount")
                             .Invoke(sizeGroup, new object[] { });

            var displayTexts = sizeGroup.GetType()
                               .GetMethod("GetDisplayTexts")
                               .Invoke(sizeGroup, null) as string[];
            Debug.Assert(totalCount == displayTexts.Length);
            var sizes = Enumerable.Range(0, totalCount)
                        .Select(i => ToGameViewSize(sizeGroup, i, displayTexts[i]))
                        .ToList();

            return sizes;
        }

        private static GameViewSnapshot.Size ToGameViewSize(object sizeGroup, int index, string name)
        {
            var gameViewSize = sizeGroup.GetType()
                               .GetMethod("GetGameViewSize")
                               .Invoke(sizeGroup, new object[] { index });
            var width = (int)gameViewSize.GetType()
                        .GetProperty("width")
                        .GetValue(gameViewSize);
            var height = (int)gameViewSize.GetType()
                         .GetProperty("height")
                         .GetValue(gameViewSize);
            return new GameViewSnapshot.Size(width, height, name);
        }
    }

    internal class GameView
    {
        static readonly BindingFlags nonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        //static readonly BindingFlags publicStatic = BindingFlags.Static | BindingFlags.Public;
#if UNITY_2019_3_OR_NEWER
        static readonly BindingFlags publicInstance = BindingFlags.Instance | BindingFlags.Public;
        static readonly BindingFlags nonPublicStatic = BindingFlags.Static | BindingFlags.NonPublic;
#endif
        static readonly string srdGameViewName = "SRD Game View";
        static readonly string forceCloseGameViewMessage = "Multiple GameViews cannot be open at the same time in Spatial Reality Display. Force closes the GameView tabs.";

        private EditorWindow gameView;

        // keep values because assembly refererence values are different befor Apply
        private Vector2 applyPosition;
        private Vector2 applySize;
        private int applyIndex = 0;

        public static int CountSRDGameView()
        {
            var srdGameViews = GetGameViews()
                               .AsEnumerable()
                               .Where(w => w.name == srdGameViewName);
            return srdGameViews.Count();
        }

        public static void TakeOneUnityGameView()
        {
            var unityGameViews = GetGameViews()
                                 .AsEnumerable()
                                 .Where(w => w.name != srdGameViewName)
                                 .Reverse()
                                 .Skip(1);

            foreach(var view in unityGameViews)
            {
                Debug.Log(forceCloseGameViewMessage);
                view.Close();
            }
        }

        public static void CloseAllUnityGameView()
        {
            var unityGameViews = GetGameViews()
                                 .AsEnumerable()
                                 .Where(w => w.name != srdGameViewName);

            foreach(var view in unityGameViews)
            {
                Debug.Log(forceCloseGameViewMessage);
                view.Close();
            }
        }

        public static void CloseAllSRDGameView()
        {
            var srdGameViews = GetGameViews()
                               .AsEnumerable()
                               .Where(w => w.name == srdGameViewName);

            foreach(var view in srdGameViews)
            {
                view.Close();
            }
        }

        private static EditorWindow[] GetGameViews()
        {
            var source = typeof(UnityEditor.Editor).Assembly;
            var gameViewType = source.GetType("UnityEditor.GameView");
            return Resources.FindObjectsOfTypeAll(gameViewType) as EditorWindow[];
        }

        public GameView()
        {
            var source = typeof(UnityEditor.Editor).Assembly;

            var gameViewType = source.GetType("UnityEditor.GameView");
            gameView = (EditorWindow)EditorWindow.CreateInstance(gameViewType);
            gameView.name = srdGameViewName;
            ShowWithPopupMenu();

            applyPosition = position;
            applySize = size;
        }

        private int windowToolbarHeight
        {
            get
            {
                var source = typeof(UnityEditor.Editor).Assembly;
                var editorGUI = source.GetType("UnityEditor.EditorGUI");
#if UNITY_2019_3_OR_NEWER
                var windowToolbarHeightObj = editorGUI.GetField("kWindowToolbarHeight", nonPublicStatic).GetValue(source);
                var windowToolbarHeightObjType = source.GetType("UnityEditor.StyleSheets.SVC`1[System.Single]");
                var windowToolbarHeight = (float)(windowToolbarHeightObjType.GetProperty("value", publicInstance).GetValue(windowToolbarHeightObj));
                return (int)windowToolbarHeight;
#else
                return (int)editorGUI.GetField("kWindowToolbarHeight", BindingFlags.NonPublic | BindingFlags.Static).GetRawConstantValue();
#endif
            }
        }

        private int selectedSizeIndex
        {
            get
            {
                return (int)gameView.GetType()
                       .GetProperty("selectedSizeIndex", nonPublicInstance)
                       .GetValue(gameView);
            }
            set
            {
                gameView.GetType()
                .GetProperty("selectedSizeIndex", nonPublicInstance)
                .SetValue(gameView, value);
            }
        }

        public int targetDisplay
        {
            set
            {
#if UNITY_2019_3_OR_NEWER
                gameView.GetType()
                .GetProperty("targetDisplay", nonPublicInstance)
                .SetValue(gameView, value);
#else
                gameView.GetType()
                .GetField("m_TargetDisplay", nonPublicInstance)
                .SetValue(gameView, value);
#endif

            }
        }

        public float scale
        {
            set
            {
                var zoomArea = gameView.GetType()
                               .GetField("m_ZoomArea", nonPublicInstance)
                               .GetValue(gameView);
                zoomArea.GetType()
                .GetField("m_Scale", nonPublicInstance)
                .SetValue(zoomArea, new Vector2(value, value));
            }
        }

        public bool noCameraWarning
        {
            set
            {
                gameView.GetType()
                .GetField("m_NoCameraWarning", nonPublicInstance)
                .SetValue(gameView, value);
            }
        }

        public Vector2 size
        {
            get
            {
                return gameView.position.size;
            }
            set
            {
                var result = GameViewSizeList.FindDestinationIndex(value);
                if(result == -1)
                {
                    //Debug.Log("Show temporary game view for updating GameViewSizes");
                }
                else
                {
                    applySize = new Vector2(value.x, value.y + windowToolbarHeight);
                    applyIndex = result;
                }
            }
        }

        public void Apply()
        {
            gameView.maxSize = applySize;
            gameView.minSize = applySize;
            gameView.position = new Rect(applyPosition, applySize);
            // selectedSizeIndexが変更されるまではgameView.position等が反映されない
            selectedSizeIndex = applyIndex;
        }

        public Vector2 position
        {
            get
            {
                return gameView.position.position;
            }
            set
            {
                applyPosition = new Vector2(value.x, value.y - windowToolbarHeight);
            }
        }

        private void ShowWithPopupMenu()
        {
            var source = typeof(UnityEditor.Editor).Assembly;
            var showModeType = source.GetType("UnityEditor.ShowMode");
            var gameViewType = source.GetType("UnityEditor.GameView");

            var popupMenu = Enum.ToObject(showModeType, 1);
            var showWithMode = gameViewType.GetMethod("ShowWithMode", nonPublicInstance);
            showWithMode.Invoke(gameView, new[] { popupMenu });
        }
    }
}

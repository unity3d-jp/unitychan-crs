/*
 * Copyright 2019,2020 Sony Corporation
 */


using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SRD.Editor.AsssemblyWrapper
{
    // SRD.AsssemblyWrapper.GameViewによるデータアクセスの度に発生するリフレクションを避ける
    internal class GameViewSnapshot
    {
        public GameViewSnapshot(
            Vector2 position,
            Vector2 size,
            List<Size> sizes,
            int selectedSizeIndex,
            GameViewSizeGroupType currentGroupType)
        {
            this.position = position;
            this.size = size;
            this.sizes = sizes;
            this.selectedSizeIndex = selectedSizeIndex;
            this.currentGroupType = currentGroupType;
        }

        public Vector2 position { get; }
        public Vector2 size { get; }
        public List<Size> sizes { get; }
        public int selectedSizeIndex { get; }
        public GameViewSizeGroupType currentGroupType { get; }

        internal class Size
        {
            public Size(int width, int height, string name)
            {
                this.width = width;
                this.height = height;
                this.name = name;
            }
            public int width { get; }
            public int height { get; }
            public string name { get; }
        }
    }
}

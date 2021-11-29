/*
 * Copyright 2019,2020 Sony Corporation
 */

using UnityEngine;

using SRD.Core;

namespace SRD.Utils
{
    internal partial class SRDSettings
    {
        public static bool Load()
        {
            return SRDDeviceInfo.Load();
        }

        public static bool LoadScreenRect()
        {
            return SRDDeviceInfo.LoadScreenRect();
        }

        public static bool LoadBodyBounds()
        {
            return SRDDeviceInfo.LoadBodyBounds();
        }

        public static SRDDeviceInfo DeviceInfo { get { return SRDDeviceInfo.Instance; } }

        public class SRDDeviceInfo
        {
            private static SRDDeviceInfo instance = new SRDDeviceInfo();
            public static SRDDeviceInfo Instance { get { return instance; } }

            public static bool Load()
            {
                var resSR = LoadScreenRect();
                var resBB = LoadBodyBounds();
                return (resSR && resBB);
            }

            public static bool LoadScreenRect()
            {
                var screenRect = loadScreenRect();
                if(screenRect == null)
                {
                    _screenRect = getDefaultScreenRect();
                    return false;
                }
                _screenRect = screenRect;
                return true;
            }

            public static bool LoadBodyBounds()
            {
                var bodyBounds = loadBodyBounds();
                if(bodyBounds == null)
                {
                    _bodyBounds = getDefaultBodyBounds();
                    return false;
                }
                _bodyBounds = bodyBounds;
                return true;
            }

            private SRDDeviceInfo(ScreenRect resolution, BodyBounds displayBounds)
            {
                _screenRect = resolution;
                _bodyBounds = displayBounds;
            }

            private SRDDeviceInfo() : this(new ScreenRect(), new BodyBounds())
            {
                // do nothing
            }

            private static ScreenRect _screenRect;
            public ScreenRect ScreenRect { get { return _screenRect; } }

            private static BodyBounds _bodyBounds;
            public BodyBounds BodyBounds { get { return _bodyBounds; } }

            private static SRDDeviceInfo getDefault()
            {
                return new SRDDeviceInfo(getDefaultScreenRect(), getDefaultBodyBounds());
            }

            private static ScreenRect getDefaultScreenRect()
            {
                return new ScreenRect();
            }

            private static BodyBounds getDefaultBodyBounds()
            {
                return new BodyBounds();
            }

            private static ScreenRect loadScreenRect()
            {
                ScreenRect screenRect;
                if(!SRDCorePlugin.GetSRDScreenRect(out screenRect))
                {
                    return null;
                }
                return screenRect;
            }

            private static BodyBounds loadBodyBounds()
            {
                BodyBounds bodyBounds;
                if(!SRDCorePlugin.GetSRDBodyBounds(SRDSessionHandler.SessionHandle, out bodyBounds))
                {
                    return null;
                }
                if(bodyBounds.Width == 0)
                {
                    return null;
                }
                return bodyBounds;
            }
        }

        [System.Serializable]
        public class ScreenRect
        {
            public static readonly int DefaultWidth = 3840;
            public static readonly int DefaultHeight = 2160;
            public static readonly int DefaultLeft = 0;
            public static readonly int DefaultTop = 0;

            public ScreenRect() : this(ScreenRect.DefaultLeft, ScreenRect.DefaultTop,
                                           ScreenRect.DefaultWidth, ScreenRect.DefaultHeight)
            {
                // do nothing
            }

            public ScreenRect(int left, int top, int width, int height)
            {
                _left = left;
                _top = top;
                _width = width;
                _height = height;
            }

            [SerializeField]
            private int _width;
            public int Width { get { return _width; } }

            [SerializeField]
            private int _height;
            public int Height { get { return _height; } }

            [SerializeField]
            private int _left;
            public int Left { get { return _left; } }

            [SerializeField]
            private int _top;
            public int Top { get { return _top; } }

            public Vector2Int Resolution { get { return new Vector2Int(_width, _height); } }
            public Vector2Int Position { get { return new Vector2Int(_left, _top); } }
        }

        [System.Serializable]
        public class BodyBounds
        {
            public static readonly float DefaultWidth = 0.345f;
            public static readonly float DefaultHeight = 0.137f;
            public static readonly float DefaultDepth = 0.137f;

            public BodyBounds() : this(BodyBounds.DefaultWidth, BodyBounds.DefaultHeight, BodyBounds.DefaultDepth)
            {
                // do nothing
            }

            public BodyBounds(float width, float height, float depth)
            {
                _width = width;
                _height = height;
                _depth = depth;

                _leftUp = new Vector3(-this.Width / 2f, this.Height, this.Depth);
                _leftBottom = new Vector3(-this.Width / 2f, 0.0f, 0.0f);
                _rightUp = new Vector3(this.Width / 2f, this.Height, this.Depth);
                _rightBottom = new Vector3(this.Width / 2f, 0.0f, 0.0f);
                _center = new Vector3(0f, this.Height / 2f, this.Depth / 2f);
                _boxSize = new Vector3(this.Width, this.Height, this.Depth);
            }

            public BodyBounds(Rect rect, float tiltRad) : this(rect.width, rect.height * Mathf.Sin(tiltRad), rect.height * Mathf.Cos(tiltRad))
            {
                // do nothing
            }

            [SerializeField]
            private float _width;
            public float Width { get { return _width; } }
            [SerializeField]
            private float _height;
            public float Height { get { return _height; } }
            [SerializeField]
            private float _depth;
            public float Depth { get { return _depth; } }

            // in PositionTrackingCoord
            private Vector3 _leftUp;
            public Vector3 LeftUp { get { return _leftUp; } }
            private Vector3 _leftBottom;
            public Vector3 LeftBottom { get { return _leftBottom; } }
            private Vector3 _rightUp;
            public Vector3 RightUp { get { return _rightUp; } }
            private Vector3 _rightBottom;
            public Vector3 RightBottom { get { return _rightBottom; } }

            public Vector3[] EdgePositions { get { return new Vector3[] { this.LeftUp, this.LeftBottom, this.RightBottom, this.RightUp }; } }

            private Vector3 _center;
            public Vector3 Center { get { return _center; } }
            private Vector3 _boxSize;
            public Vector3 BoxSize { get { return _boxSize; } }
        }
    }
}

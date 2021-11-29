/*
 * Copyright 2021 Sony Corporation
 */


using UnityEngine;

namespace SRD.Sample.Common
{
    [RequireComponent(typeof(TextMesh))]
    internal class FPSShower : MonoBehaviour
    {
        [SerializeField, Tooltip("The period in seconds for counting frames.")]
        private float _frameCountPeriod = 0.5f;
        public float frameCountPeriod
        {
            get { return _frameCountPeriod;  }
            set { if(value > 0) _frameCountPeriod = value; }
        }

        [Tooltip("A format string for displaying FPS.")]
        public string format = "FPS: #";

        private float _nextCalculationTime;
        private int _framesCount;

        public float fps
        {
            get;
            private set;
        }

        void Start()
        {
            _nextCalculationTime = Time.realtimeSinceStartup + _frameCountPeriod;
            _framesCount = 0;
        }

        void Update()
        {
            if(_nextCalculationTime < Time.realtimeSinceStartup)
            {
                fps = _framesCount / _frameCountPeriod;
                ShowFPS();

                _nextCalculationTime += _frameCountPeriod;
                _framesCount = 0;
            }
            ++_framesCount;
        }

        void OnValidate()
        {
            if(_frameCountPeriod <= 0)
            {
                _frameCountPeriod = 0.5f;
                Debug.LogWarning("Counting Period Second must be greater than 0!");
            }

            // Show formatting sample
            ShowFPS(123.45f);
        }

        private void ShowFPS()
        {
            ShowFPS(fps);
        }

        private void ShowFPS(float value)
        {
            TextMesh textMesh = this.GetComponent<TextMesh>();
            if(textMesh != null)
            {
                textMesh.text = value.ToString(format);
            }
            else
            {
                Debug.LogWarning("TextMesh to show FPS is not exist!");
            }
        }
    }
}

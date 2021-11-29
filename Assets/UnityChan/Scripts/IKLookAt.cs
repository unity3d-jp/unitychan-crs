using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.WebCam;

namespace UnityChan
{
    public class IKLookAt : MonoBehaviour, ILookAt
    {

        public Animator animator;

        [Range(0.0f, 1.0f)] public float bodyWeight = 0.0f;
        [Range(0.0f, 1.0f)] public float headWeight = 0.4f;
        [Range(0.0f, 1.0f)] public float clampWeight = 0.3f;
        
        [Range(0.1f, 1.0f)] public float transitionSec = 1.0f;
        
        private GameObject _watchingObject;
        private bool _isFaceTrackingActive;

        public float _currentWeight = 0.0f;
        private float _lastLookAtStateChangedTime;

        void Start()
        {
            var manager = GameObject.Find("LookAtManager");
            if (manager != null)
            {
                var lkMgr = manager.GetComponent<LookAtManager>();
                lkMgr.Register(this);
            }
        }

        void Update()
        {
            
        }

        float QuadInOut(float t)
        {
            return (1 - Mathf.Pow(1 - Mathf.Abs(t * 2 - 1), 2)) * Mathf.Sign(t - 0.5f) / 2 + 0.5f;
        }
        private void OnDestroy()
        {
            var manager = GameObject.Find("LookAtManager");
            if (manager == null) return;
            var lkMgr = manager.GetComponent<LookAtManager>();
            lkMgr.Unregister(this);
        }

        public void EnableLookAt(bool lookAtEnabled, GameObject obj)
        {
            if (_isFaceTrackingActive != lookAtEnabled)
            {
                _lastLookAtStateChangedTime = Time.time;
            }
            
            _isFaceTrackingActive = lookAtEnabled;
            _watchingObject = obj;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            var weight = QuadInOut(Mathf.Clamp01((Time.time - _lastLookAtStateChangedTime)/transitionSec));
            _currentWeight = _isFaceTrackingActive ? weight : 1.0f - weight;

            animator.SetLookAtWeight(_currentWeight, bodyWeight, headWeight, 0.0f, clampWeight);
            
            if (_watchingObject != null)
            {
                animator.SetLookAtPosition(_watchingObject.transform.position);
            }
        }
    }
}
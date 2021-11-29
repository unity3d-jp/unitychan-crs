/*
 * Copyright 2019,2020 Sony Corporation
 */


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SRD.Core;

namespace SRD.Utils
{
    /// <summary>
    /// A utility component for changing SRDViewSpaceScale.
    /// The scale of the GameObject that has this component follows SRDViewSpaceScale automatically.
    /// </summary>
    [ExecuteInEditMode]
    public class SRDViewSpaceScaleFollower : MonoBehaviour
    {
        [Tooltip("If this is enabled, the scale of the GameObject is always the same as SRDViewSpaceScale.")]
        [SerializeField]
        private bool _absoluteFollow = false;

        private SRDManager _srdManager;
        private Vector3 _baseThisPos;
        private Vector3 _baseThisScale;
        private float _baseViewSpaceScale;

        private void Initialize()
        {
            Debug.Assert(_srdManager == null);

            _srdManager = SRDSceneEnvironment.GetSRDManager();
            if(_srdManager == null)
            {
                Debug.LogWarning(SRDHelper.SRDMessages.SRDManagerNotFoundError);
                return;
            }

            _srdManager.OnSRDViewSpaceScaleChangedEvent.AddListener(this.OnViewSpaceScaleChanged);
            UpdateParameters();
        }

        void Update()
        {
            if(_srdManager == null)
            {
                Initialize();
                return;
            }

            if(this.transform.hasChanged)
            {
                UpdateParameters();
                this.transform.hasChanged = false;
            }
        }

        private void UpdateParameters()
        {
            Debug.Assert(_srdManager != null);

            _baseThisPos = _srdManager.transform.InverseTransformPoint(this.transform.position);
            if(_absoluteFollow)
            {
                this.transform.localScale = _srdManager.SRDViewSpaceScale * Vector3.one;
            }
            _baseThisScale = this.transform.localScale;
            _baseViewSpaceScale = _srdManager.SRDViewSpaceScale;
        }

        internal void OnViewSpaceScaleChanged(float newViewSpaceScale)
        {
            Debug.Assert(_srdManager != null);

            if(!this.enabled)
            {
                return;
            }

            this.transform.position = _srdManager.transform.TransformPoint(_baseThisPos);
            if(_absoluteFollow)
            {
                this.transform.localScale = _srdManager.SRDViewSpaceScale * Vector3.one;
            }
            else
            {
                var scale = newViewSpaceScale / _baseViewSpaceScale;
                this.transform.localScale = _baseThisScale * scale;
            }

        }
    }
}

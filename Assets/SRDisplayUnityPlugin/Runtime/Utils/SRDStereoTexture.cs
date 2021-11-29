/*
 * Copyright 2019,2020 Sony Corporation
 */
using UnityEngine;

namespace SRD.Utils
{
    [DisallowMultipleComponent]
    internal class SRDStereoTexture : MonoBehaviour
    {
        public Texture2D leftTexture;
        public Texture2D rightTexture;

        private Texture2D _oldLeft;
        private Texture2D _oldRight;

        private bool _changed = true;
        public bool Changed
        {
            get => _changed;
        }

        private void Update()
        {
            if(_oldLeft != leftTexture || _oldRight != rightTexture)
            {
                _changed = true;
            }
        }

        public void ResolveChanges()
        {
            _oldLeft = leftTexture;
            _oldRight = rightTexture;
            _changed = false;
        }
    }

}

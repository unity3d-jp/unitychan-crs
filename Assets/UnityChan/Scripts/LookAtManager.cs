using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityChan
{
    public class LookAtManager : MonoBehaviour
    {
        public GameObject lookAtTarget;
        private bool _lookAtEnabled = true;

        private List<ILookAt> _subscribers;
        
        public void LookAtEnabled(bool lookAtEnabled)
        {
            _lookAtEnabled = lookAtEnabled;
            if (_subscribers == null)
            {
                _subscribers = new List<ILookAt>();
            }

            foreach (var obj in _subscribers)
            {
                obj.EnableLookAt(_lookAtEnabled, lookAtTarget);
            }
            
            Debug.Log("[LookAt Event]:" + _lookAtEnabled + " subscriber:" + _subscribers.Count);
        }

        public void Register(ILookAt obj)
        {
            if (_subscribers == null)
            {
                _subscribers = new List<ILookAt>();
            }
            _subscribers.Add(obj);
            obj.EnableLookAt(_lookAtEnabled, lookAtTarget);
        }

        public void Unregister(ILookAt obj)
        {
            _subscribers.Remove(obj);
        }
    }
}
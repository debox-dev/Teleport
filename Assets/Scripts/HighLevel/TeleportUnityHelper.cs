using System;
using UnityEngine;

namespace DeBox.Teleport
{
    public class TeleportUnityHelper : MonoBehaviour
    {
        private Action _fixedUpdateCallback;

        public void Initialize(Action fixedUpdateCallback)
        {
            _fixedUpdateCallback = fixedUpdateCallback;
        }

        private void FixedUpdate()
        {
            _fixedUpdateCallback?.Invoke();
        }

        public void Deinitialize()
        {
            _fixedUpdateCallback = null;
            Destroy(gameObject);
        }
    }
}

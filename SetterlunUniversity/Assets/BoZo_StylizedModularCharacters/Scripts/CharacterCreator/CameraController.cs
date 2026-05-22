using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Bozo.ModularCharacters
{
    public class CameraController : MonoBehaviour
    {
        Camera cam;
        float position;
        public float scrollSpeed;
        public Transform startPosition;
        public Transform endPosition;
        public Vector2 fov;
        public Slider slider;

        public float tweenSpeed;

        float tweenTimer;
        float currentPosition;
        float targetPosition;

        [SerializeField] CameraPositions[] cameraPositions;
        Dictionary<OutfitType, CameraPositions> camPos = new Dictionary<OutfitType, CameraPositions>();

        private void Awake()
        {
            cam = Camera.main;
            foreach (var item in cameraPositions)
            {
                camPos.Add(item.type, item);
            }
        }

        private void Update()
        {
            if (Input.GetAxis("Mouse ScrollWheel") > 0f) // forward
            {
                position += scrollSpeed;
                tweenTimer = -1;
            }
            else if (Input.GetAxis("Mouse ScrollWheel") < 0f) // backwards
            {
                position -= scrollSpeed;
                tweenTimer = -1;
            }

            slider.value = position;
            position = Mathf.Clamp(position, 0, 1);

            if (tweenTimer >= 0)
            {
                position = Mathf.Lerp(targetPosition, currentPosition, tweenTimer);
                tweenTimer -= Time.deltaTime * tweenSpeed;
            }

            cam.transform.position = Vector3.Lerp(startPosition.position, endPosition.position, position);
            cam.transform.rotation = Quaternion.Lerp(startPosition.rotation, endPosition.rotation, position);
            cam.fieldOfView = Mathf.Lerp(fov.x, fov.y, position);
        }

        public void SetPosition(float value)
        {
            position = value;
        }

        public void tweenPosition(float value)
        {
            tweenTimer = 1;
            targetPosition = value;
            currentPosition = position;
        }

        public void TweenPosition(OutfitType type)
        {
            CameraPositions settings = null;
            if(!camPos.TryGetValue(type, out settings)) { settings = cameraPositions[0]; }
            startPosition = settings.ZoomOutPos;
            endPosition = settings.ZoomInPos;
            fov.x = settings.fovOut;
            fov.y = settings.fovIn;

            tweenTimer = 1;
            targetPosition = settings.startingPosition;
            currentPosition = position;
        }

        [System.Serializable]
        private class CameraPositions
        {
            public OutfitType type;
            public Transform ZoomOutPos;
            public Transform ZoomInPos;
            public float fovOut;
            public float fovIn;
            public float startingPosition;
        }
    }
}

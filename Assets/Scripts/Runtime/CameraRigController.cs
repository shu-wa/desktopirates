using UnityEngine;

namespace Desktopirates
{
    public sealed class CameraRigController : MonoBehaviour
    {
        public float CurrentYaw { get; private set; }
        public int DirectionIndex => Mathf.RoundToInt(CurrentYaw / 45f) & 7;

        private Camera targetCamera;
        private float targetYaw;

        public void Initialize(Camera camera)
        {
            targetCamera = camera;
            CurrentYaw = 0f;
            targetYaw = 0f;
            ApplyCameraTransform();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Q)) targetYaw -= 45f;
            if (Input.GetKeyDown(KeyCode.E)) targetYaw += 45f;

            CurrentYaw = Mathf.MoveTowardsAngle(CurrentYaw, targetYaw, Time.deltaTime * 260f);
            ApplyCameraTransform();
        }

        private void ApplyCameraTransform()
        {
            if (targetCamera == null) return;
            Quaternion yaw = Quaternion.Euler(0f, CurrentYaw, 0f);
            Vector3 target = new Vector3(0f, 0.15f, 0f);
            Vector3 position = target + yaw * new Vector3(0f, 8.2f, -9.3f);
            Quaternion rotation = Quaternion.LookRotation(target - position, Vector3.up);
            targetCamera.transform.SetPositionAndRotation(position, rotation);
            targetCamera.transform.position += targetCamera.transform.up * 1.25f;
        }
    }
}

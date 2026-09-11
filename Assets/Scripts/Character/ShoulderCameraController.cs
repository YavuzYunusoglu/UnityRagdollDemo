using UnityEngine;
using UnityEngine.InputSystem;

namespace RagdollDemo.Character
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class ShoulderCameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField, Min(0f)] private float pivotHeight = 1.55f;
        [SerializeField] private float shoulderOffset = 0.55f;
        [SerializeField, Min(0.1f)] private float distance = 2.8f;

        [Header("Mouse Look")]
        [Tooltip("Degrees per mouse pixel. Mouse delta is already a per-frame displacement.")]
        [SerializeField, Min(0.001f)] private float sensitivity = 0.12f;
        [SerializeField] private float initialPitch = 12f;
        [SerializeField] private float minimumPitch = -35f;
        [SerializeField] private float maximumPitch = 70f;
        [SerializeField] private bool invertY;

        [Header("Camera Collision")]
        [SerializeField, Min(0.01f)] private float collisionRadius = 0.2f;
        [SerializeField, Min(0f)] private float collisionPadding = 0.05f;
        [SerializeField] private LayerMask collisionMask = ~0;

        [Header("Fall And Recovery Follow")]
        [Tooltip("Tracks the actual chest while falling, keeping the horizon level and mouse look independent.")]
        [SerializeField, Min(0.01f)] private float fallFollowSmoothTime = 0.12f;
        [Tooltip("Normal Player pivotu ile göğüs takip pivotu arasındaki geçiş süresi (saniye). Küçük değer düşüşe daha hızlı kamera tepkisi verir.")]
        [SerializeField, Min(0.01f)] private float fallBlendTime = 0.18f;
        [Tooltip("Kamera engelden uzaklaştığında omuz mesafesine yumuşakça geri dönme süresi (saniye).")]
        [SerializeField, Min(0.01f)] private float collisionReturnTime = 0.16f;

        private readonly RaycastHit[] obstructionHits = new RaycastHit[32];
        private float pitch;
        private bool skipNextMouseDelta;
        private PhysicsCharacterBalanceController balance;
        private Transform chestTarget;
        private Vector3 chestLocalPivot;
        private Vector3 trackedChestPivot;
        private Vector3 chestPivotVelocity;
        private float fallBlend;
        private float fallBlendVelocity;
        private float currentBoomLength;
        private float boomVelocity;
        public Vector3 CurrentPivot { get; private set; }

        public float Yaw { get; private set; }
        public bool HasControl => isActiveAndEnabled && Application.isFocused
            && Cursor.lockState == CursorLockMode.Locked && Time.timeScale > 0f;

        private void OnEnable()
        {
            if (target == null)
            {
                Debug.LogError("ShoulderCameraController needs a Target (Player).", this);
                enabled = false;
                return;
            }

            Yaw = target.eulerAngles.y;
            pitch = Mathf.Clamp(initialPitch, minimumPitch, maximumPitch);
            balance = target.GetComponent<PhysicsCharacterBalanceController>();
            chestTarget = balance != null ? balance.ChestTransform : null;
            Vector3 standingPivot = target.position + Vector3.up * pivotHeight;
            if (chestTarget != null) chestLocalPivot = chestTarget.InverseTransformPoint(standingPivot);
            trackedChestPivot = standingPivot;
            chestPivotVelocity = Vector3.zero;
            fallBlend = fallBlendVelocity = boomVelocity = 0f;
            currentBoomLength = new Vector3(shoulderOffset, 0f, -distance).magnitude;
            SetCursorLock(true);
        }

        private void OnDisable() => SetCursorLock(false);

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
                SetCursorLock(false);
        }

        private void Update()
        {
            if (!Application.isFocused)
                return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetCursorLock(false);
                return;
            }

            Mouse mouse = Mouse.current;
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (Time.timeScale > 0f && mouse != null && mouse.leftButton.wasPressedThisFrame)
                    SetCursorLock(true);
                return;
            }

            if (!HasControl || mouse == null)
                return;

            if (skipNextMouseDelta)
            {
                skipNextMouseDelta = false;
                return;
            }

            Vector2 delta = mouse.delta.ReadValue() * sensitivity;
            Yaw = Mathf.Repeat(Yaw + delta.x, 360f);
            pitch = Mathf.Clamp(pitch + delta.y * (invertY ? 1f : -1f), minimumPitch, maximumPitch);
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            // Read the interpolated body position after physics; look remains responsive every frame.
            Quaternion rotation = Quaternion.Euler(pitch, Yaw, 0f);
            Vector3 pivot = target.position + Vector3.up * pivotHeight;
            float dt = Time.deltaTime;
            if (chestTarget != null && dt > 0f)
            {
                Vector3 chestPivot = chestTarget.TransformPoint(chestLocalPivot);
                bool falling = balance != null && balance.IsPhysicalFall;
                if (!falling && fallBlend < 0.001f)
                {
                    trackedChestPivot = chestPivot;
                    chestPivotVelocity = Vector3.zero;
                }
                else
                    trackedChestPivot = Vector3.SmoothDamp(trackedChestPivot, chestPivot,
                        ref chestPivotVelocity, fallFollowSmoothTime, Mathf.Infinity, dt);
                fallBlend = Mathf.SmoothDamp(fallBlend, falling ? 1f : 0f,
                    ref fallBlendVelocity, fallBlendTime, Mathf.Infinity, dt);
                pivot = Vector3.Lerp(pivot, trackedChestPivot, fallBlend);
            }
            CurrentPivot = pivot;
            Vector3 offset = rotation * new Vector3(shoulderOffset, 0f, -distance);
            float length = offset.magnitude;
            Vector3 direction = offset / length;
            float allowedLength = length;

            int count = Physics.SphereCastNonAlloc(pivot, collisionRadius, direction,
                obstructionHits, length, collisionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = obstructionHits[i];
                if (hit.transform == target || hit.transform.IsChildOf(target))
                    continue;
                // PhysicsRig is a sibling of Player, not its child.
                if (balance != null && balance.OwnsBody(hit.rigidbody)) continue;
                allowedLength = Mathf.Min(allowedLength, Mathf.Max(0f, hit.distance - collisionPadding));
            }

            if (allowedLength < currentBoomLength)
            {
                currentBoomLength = allowedLength;
                boomVelocity = 0f;
            }
            else if (dt > 0f)
                currentBoomLength = Mathf.SmoothDamp(currentBoomLength, allowedLength,
                    ref boomVelocity, collisionReturnTime, Mathf.Infinity, dt);
            transform.SetPositionAndRotation(pivot + direction * currentBoomLength, rotation);
        }

        private void SetCursorLock(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
            skipNextMouseDelta = true;
        }
    }
}

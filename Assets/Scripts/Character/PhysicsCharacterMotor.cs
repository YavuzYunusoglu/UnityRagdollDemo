using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RagdollDemo.Character
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class PhysicsCharacterMotor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private ShoulderCameraController view;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 4f;
        [SerializeField, Min(0f)] private float turnSpeed = 540f;
        [SerializeField, Min(0f)] private float acceleration = 25f;
        [SerializeField, Min(0f)] private float braking = 35f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0f;
        [SerializeField] private bool accelerateConnectedBodies = true;

        [Header("Ground Detection")]
        [SerializeField, Min(0.01f)] private float groundCheckDistance = 0.1f;
        [SerializeField, Range(0f, 85f)] private float maxGroundAngle = 55f;
        [SerializeField] private LayerMask groundMask = ~0;

        [Header("Step Handling")]
        [SerializeField, Min(0f)] private float maxStepHeight = 0.25f;
        [SerializeField, Min(0.01f)] private float stepCheckDistance = 0.12f;
        [SerializeField, Min(0f)] private float stepUpSpeed = 3f;
        [SerializeField, Range(0f, 0.05f)] private float stepSkin = 0.02f;

        private readonly RaycastHit[] groundHits = new RaycastHit[16];
        private readonly RaycastHit[] stepHits = new RaycastHit[16];
        private readonly HashSet<Rigidbody> characterBodies = new HashSet<Rigidbody>();
        private Rigidbody body;
        private CapsuleCollider capsule;
        private InputActionAsset runtimeActions;
        private InputAction moveAction;
        private Vector2 moveInput;
        private float controlledYaw;

        public bool IsGrounded { get; private set; }
        public Vector2 LastMoveInput { get; private set; }
        public Vector3 LastDesiredVelocity { get; private set; }
        public Vector3 LastTargetAcceleration { get; private set; }
        public Vector3 LastAppliedAcceleration { get; private set; }
        public bool SteppedThisFixedUpdate { get; private set; }
        public float LastStepRise { get; private set; }
        public bool AcceleratesConnectedBodies => accelerateConnectedBodies;

        
        
        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            controlledYaw = body.rotation.eulerAngles.y;
            CacheCharacterBodies();

            if (inputActions == null || view == null || capsule.direction != 1)
            {
                Debug.LogError("PhysicsCharacterMotor needs Input Actions, View and a Y-axis capsule.", this);
                enabled = false;
                return;
            }

            // Own this instance so enabling/disabling the player cannot alter other input users.
            runtimeActions = Instantiate(inputActions);
            moveAction = runtimeActions.FindAction("Player/Move", false);
            if (moveAction == null)
            {
                Debug.LogError("Input Actions must contain Player/Move (Vector2).", this);
                enabled = false;
            }
        }

        private void OnEnable() => moveAction?.Enable();

        private void OnDisable()
        {
            moveAction?.Disable();
            moveInput = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (runtimeActions != null)
                Destroy(runtimeActions);
        }

        private void Update()
        {
            moveInput = view.HasControl
                ? Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f)
                : Vector2.zero;
        }

        private void FixedUpdate()
        {
            SteppedThisFixedUpdate = false;
            LastStepRise = 0f;

            controlledYaw = Mathf.MoveTowardsAngle(
                controlledYaw,
                view.Yaw,
                turnSpeed * Time.fixedDeltaTime);

            // Limb contacts may apply a large yaw impulse. The controlled root owns yaw,
            // so discard that impulse while keeping the requested turn responsive.
            Vector3 angularVelocity = body.angularVelocity;
            angularVelocity.y = 0f;
            body.angularVelocity = angularVelocity;

            Quaternion bodyHeading = Quaternion.Euler(0f, controlledYaw, 0f);
            Quaternion movementHeading = Quaternion.Euler(0f, view.Yaw, 0f);
            body.MoveRotation(bodyHeading);
            IsGrounded = CheckGround();

            Vector2 input = view.HasControl ? moveInput : Vector2.zero;
            Vector3 desiredVelocity = movementHeading * new Vector3(input.x, 0f, input.y) * moveSpeed;
            LastMoveInput = input;
            LastDesiredVelocity = desiredVelocity;
            TryStepUp(desiredVelocity);

            Vector3 planarVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
            float limit = input.sqrMagnitude > 0.001f ? acceleration : braking;
            limit *= IsGrounded ? 1f : airControl;

            // Limit the correction: impacts are resisted gradually, while gravity stays untouched.
            Vector3 correction = (desiredVelocity - planarVelocity) / Time.fixedDeltaTime;
            Vector3 appliedAcceleration = Vector3.ClampMagnitude(correction, limit);
            LastTargetAcceleration = correction;
            LastAppliedAcceleration = appliedAcceleration;
            ApplyMovementAcceleration(appliedAcceleration);
        }

        public void SynchronizeControlledYawToBody()
        {
            if (body == null)
                body = GetComponent<Rigidbody>();
            controlledYaw = body.rotation.eulerAngles.y; // Motor açıldığında karakter eski controlledYaw değerine dönüp ani bir rotasyon yapmaması için yaw hedefini dik pozla eşitliyoruz.
        }
        

        private void ApplyMovementAcceleration(Vector3 appliedAcceleration)
        {
            if (!accelerateConnectedBodies)
            {
                body.AddForce(appliedAcceleration, ForceMode.Acceleration);
                return;
            }

            // Move the complete connected rig with the same acceleration. Applying
            // locomotion only to the root makes every limb receive starts/stops late
            // through its joints, which creates a whip-like recovery motion.
            foreach (Rigidbody characterBody in characterBodies)
            {
                if (characterBody != null && !characterBody.isKinematic)
                    characterBody.AddForce(appliedAcceleration, ForceMode.Acceleration);
            }
        }

        private bool CheckGround()
        {
            Vector3 scale = transform.lossyScale;
            float radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float height = Mathf.Max(capsule.height * Mathf.Abs(scale.y), radius * 2f);
            Vector3 lowerSphere = transform.TransformPoint(capsule.center)
                - Vector3.up * (height * 0.5f - radius);
            const float lift = 0.05f;
            float probeRadius = radius * 0.9f;
            float probeDistance = lift + radius - probeRadius + groundCheckDistance;
            int count = Physics.SphereCastNonAlloc(lowerSphere + Vector3.up * lift,
                probeRadius, Vector3.down, groundHits, probeDistance, groundMask,
                QueryTriggerInteraction.Ignore);
            float minimumUp = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = groundHits[i];
                if (IsCharacterCollider(hit.collider))
                    continue;
                if (Vector3.Dot(hit.normal, Vector3.up) >= minimumUp)
                    return true;
            }

            return false;
        }

        private void TryStepUp(Vector3 desiredVelocity)
        {
            if (!IsGrounded || maxStepHeight <= 0f || desiredVelocity.sqrMagnitude < 0.01f)
                return;

            GetCapsuleWorldDimensions(out float radius, out float height);
            Vector3 lowerSphere = transform.TransformPoint(capsule.center)
                - Vector3.up * (height * 0.5f - radius);
            Vector3 foot = lowerSphere - Vector3.up * radius;
            Vector3 direction = Vector3.ProjectOnPlane(desiredVelocity, Vector3.up).normalized;
            float forwardDistance = radius + stepCheckDistance;

            Vector3 lowerOrigin = foot + Vector3.up * stepSkin;
            if (!TryRaycastIgnoringCharacter(
                    lowerOrigin,
                    direction,
                    forwardDistance,
                    out RaycastHit lowerHit))
                return;

            // A walkable surface is a ramp. Step handling is only for a near-vertical riser.
            if (Vector3.Dot(lowerHit.normal, Vector3.up) > 0.25f)
                return;

            Vector3 upperOrigin = foot + Vector3.up * (maxStepHeight + stepSkin);
            if (TryRaycastIgnoringCharacter(
                    upperOrigin,
                    direction,
                    forwardDistance,
                    out _))
                return;

            float distancePastFace = Mathf.Min(
                lowerHit.distance + stepCheckDistance * 0.5f,
                forwardDistance);
            Vector3 topProbe = foot
                + direction * distancePastFace
                + Vector3.up * (maxStepHeight + stepSkin);

            if (!TryRaycastIgnoringCharacter(
                    topProbe,
                    Vector3.down,
                    maxStepHeight + stepSkin * 2f,
                    out RaycastHit topHit))
                return;

            float minimumUp = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
            if (Vector3.Dot(topHit.normal, Vector3.up) < minimumUp)
                return;

            float stepHeight = topHit.point.y - foot.y;
            if (stepHeight <= stepSkin || stepHeight > maxStepHeight + stepSkin)
                return;

            float rise = Mathf.Min(
                stepHeight + stepSkin,
                stepUpSpeed * Time.fixedDeltaTime);
            MoveCharacterBodiesUp(rise);
        }

        private void MoveCharacterBodiesUp(float rise)
        {
            SteppedThisFixedUpdate = true;
            LastStepRise = rise;
            Vector3 displacement = Vector3.up * rise;

            // Move the complete connected rig by the same amount. Moving only the
            // root stretches the leg joints against a tall riser and causes sticking.
            foreach (Rigidbody characterBody in characterBodies)
            {
                if (characterBody == null)
                    continue;

                characterBody.MovePosition(characterBody.position + displacement);

                if (characterBody.linearVelocity.y < 0f)
                {
                    Vector3 velocity = characterBody.linearVelocity;
                    velocity.y = 0f;
                    characterBody.linearVelocity = velocity;
                }
            }
        }

        private bool TryRaycastIgnoringCharacter(
            Vector3 origin,
            Vector3 direction,
            float distance,
            out RaycastHit closestHit)
        {
            int count = Physics.RaycastNonAlloc(
                origin,
                direction,
                stepHits,
                distance,
                groundMask,
                QueryTriggerInteraction.Ignore);

            closestHit = default;
            float closestDistance = float.PositiveInfinity;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = stepHits[i];
                if (IsCharacterCollider(hit.collider) || hit.distance >= closestDistance)
                    continue;

                closestHit = hit;
                closestDistance = hit.distance;
                found = true;
            }

            return found;
        }

        private bool IsCharacterCollider(Collider other)
        {
            Rigidbody attachedBody = other.attachedRigidbody;
            return attachedBody != null && characterBodies.Contains(attachedBody);
        }

        private void CacheCharacterBodies()
        {
            characterBodies.Clear();
            characterBodies.Add(body);

            ConfigurableJoint[] joints = Object.FindObjectsByType<ConfigurableJoint>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            bool addedBody;
            do
            {
                addedBody = false;
                foreach (ConfigurableJoint joint in joints)
                {
                    Rigidbody jointBody = joint.GetComponent<Rigidbody>();
                    if (jointBody == null || joint.connectedBody == null
                        || !characterBodies.Contains(joint.connectedBody))
                        continue;

                    addedBody |= characterBodies.Add(jointBody);
                }
            }
            while (addedBody);
        }

        private void GetCapsuleWorldDimensions(out float radius, out float height)
        {
            Vector3 scale = transform.lossyScale;
            radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            height = Mathf.Max(capsule.height * Mathf.Abs(scale.y), radius * 2f);
        }
    }
}

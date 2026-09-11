using System;
using UnityEngine;

namespace RagdollDemo.Character
{
    [DefaultExecutionOrder(60)]
    [DisallowMultipleComponent]
    public sealed class ActiveRagdollPoseDriver : MonoBehaviour
    {
        [Serializable]
        private sealed class JointTarget
        {
            public ConfigurableJoint joint;

            [Tooltip("Desired rotation in the Configurable Joint's local joint space.")]
            public Vector3 targetRotation;
        }

        [SerializeField] private JointTarget[] jointTargets = Array.Empty<JointTarget>();

        [Header("Procedural Gait")]
        [SerializeField] private Rigidbody movementBody;
        [SerializeField] private ConfigurableJoint leftThighJoint;
        [SerializeField] private ConfigurableJoint rightThighJoint;
        [SerializeField] private ConfigurableJoint leftShinJoint;
        [SerializeField] private ConfigurableJoint rightShinJoint;
        [SerializeField] private ConfigurableJoint leftUpperArmJoint;
        [SerializeField] private ConfigurableJoint rightUpperArmJoint;

        [Header("Gait Tuning")]
        [SerializeField, Min(0.1f)] private float fullStrideSpeed = 4f;
        [Tooltip("World-space metres travelled during one complete left/right gait cycle.")]
        [SerializeField, Min(0.1f)] private float strideLength = 2.5f;
        [SerializeField, Range(0f, 45f)] private float thighSwing = 18f;
        [SerializeField, Range(0f, 60f)] private float kneeBend = 22f;
        [SerializeField, Range(0f, 30f)] private float armSwing = 8f;
        [SerializeField, Min(0f)] private float gaitBlendSpeed = 6f;

        [Header("Side Step Tuning")]
        [SerializeField, Min(0f)] private float sideStepFrequency = 1.35f;
        [SerializeField, Range(0f, 20f)] private float sideStepThighAngle = 7f;
        [SerializeField, Range(0f, 30f)] private float sideStepKneeBend = 10f;

        private float gaitPhase;
        private float gaitWeight;
        private float sideStepPhase;
        private float sideStepWeight;
        private float sideStepDirection = 1f;
        private PhysicsCharacterBalanceController balance;
        private ConfigurableJoint recoveryChestJoint;
        private Quaternion restingChestTarget;

        [Header("Get Up Pose")]
        [Tooltip("Kalkışın toplanma bölümünde sol kalçaya eklenecek maksimum öne bükülme açısı (derece).")]
        [SerializeField, Range(0f, 35f)] private float recoveryHipBend = 28f;
        [Tooltip("Kalkışın toplanma bölümünde sol dize eklenecek maksimum bükülme açısı (derece).")]
        [SerializeField, Range(0f, 85f)] private float recoveryKneeBend = 62f;
        [Tooltip("Kalkış sırasında iki üst kola eklenecek maksimum uzanma açısı (derece).")]
        [SerializeField, Range(0f, 35f)] private float recoveryArmReach = 25f;
        [Tooltip("Kalkışın toplanma bölümünde göğse eklenecek maksimum öne eğilme açısı (derece).")]
        [SerializeField, Range(0f, 28f)] private float recoveryTorsoBend = 24f;

        private void Awake()
        {
            balance = GetComponent<PhysicsCharacterBalanceController>();
            if (balance != null && balance.ChestTransform != null)
            {
                recoveryChestJoint = balance.ChestTransform.GetComponent<ConfigurableJoint>();
                if (recoveryChestJoint != null) restingChestTarget = recoveryChestJoint.targetRotation;
            }
        }

        private void FixedUpdate()
        {
            UpdateGait();

            float leftCycle = Mathf.Sin(gaitPhase) * gaitWeight;
            float rightCycle = -leftCycle;
            float sideCycle = Mathf.Sin(sideStepPhase) * sideStepWeight;
            float leftSideStep;
            float rightSideStep;

            if (sideStepDirection > 0f)
            {
                rightSideStep = Mathf.Max(0f, sideCycle);
                leftSideStep = Mathf.Max(0f, -sideCycle);
            }
            else
            {
                leftSideStep = Mathf.Max(0f, sideCycle);
                rightSideStep = Mathf.Max(0f, -sideCycle);
            }

            foreach (JointTarget target in jointTargets)
            {
                if (target.joint == null)
                    continue;

                Vector3 rotation = target.targetRotation;

                if (target.joint == leftThighJoint)
                {
                    rotation.x += leftCycle * thighSwing;
                    rotation.z += sideStepDirection * leftSideStep * sideStepThighAngle;
                }
                else if (target.joint == rightThighJoint)
                {
                    rotation.x += rightCycle * thighSwing;
                    rotation.z += sideStepDirection * rightSideStep * sideStepThighAngle;
                }
                else if (target.joint == leftShinJoint)
                    rotation.x -= Mathf.Max(0f, leftCycle) * kneeBend
                        + leftSideStep * sideStepKneeBend;
                else if (target.joint == rightShinJoint)
                    rotation.x -= Mathf.Max(0f, rightCycle) * kneeBend
                        + rightSideStep * sideStepKneeBend;
                else if (target.joint == leftUpperArmJoint)
                    rotation.x -= leftCycle * armSwing;
                else if (target.joint == rightUpperArmJoint)
                    rotation.x -= rightCycle * armSwing;

                float recoveryWeight = balance != null ? balance.RecoveryPoseWeight : 0f;
                if (recoveryWeight > 0f)
                {
                    // Joint-space signs match the existing gait: hips forward, knees flexed.
                    Vector3 gathered = target.targetRotation;
                    if (target.joint == leftThighJoint) gathered.x += recoveryHipBend;
                    else if (target.joint == rightThighJoint) gathered.x += recoveryHipBend * 0.75f;
                    else if (target.joint == leftShinJoint) gathered.x -= recoveryKneeBend;
                    else if (target.joint == rightShinJoint) gathered.x -= recoveryKneeBend * 0.8f;
                    else if (target.joint == leftUpperArmJoint || target.joint == rightUpperArmJoint)
                        gathered.x += recoveryArmReach;
                    rotation = Vector3.Lerp(rotation, gathered, recoveryWeight);
                }
                target.joint.targetRotation = Quaternion.Euler(rotation);
            }
            if (recoveryChestJoint != null)
                recoveryChestJoint.targetRotation = restingChestTarget * Quaternion.Euler(
                    -recoveryTorsoBend * (balance != null ? balance.RecoveryPoseWeight : 0f), 0f, 0f);
        }

        private void OnDisable()
        {
            if (recoveryChestJoint != null) recoveryChestJoint.targetRotation = restingChestTarget;
        }

        private void UpdateGait()
        {
            if (balance != null && balance.IsPhysicalFall)
            {
                // Ragdoll sliding speed is not walking input. Resume from a neutral gait.
                gaitWeight = Mathf.MoveTowards(gaitWeight, 0f, gaitBlendSpeed * Time.fixedDeltaTime);
                sideStepWeight = Mathf.MoveTowards(sideStepWeight, 0f, gaitBlendSpeed * Time.fixedDeltaTime);
                gaitPhase = sideStepPhase = 0f;
                return;
            }
            if (movementBody == null)
            {
                gaitWeight = sideStepWeight = 0f;
                return;
            }

            Vector3 planarVelocity = Vector3.ProjectOnPlane(movementBody.linearVelocity, Vector3.up);
            Vector3 localVelocity = movementBody.transform.InverseTransformDirection(planarVelocity);
            float forwardSpeed = Mathf.Abs(localVelocity.z);
            float sideSpeed = Mathf.Abs(localVelocity.x);
            float speedWeight = Mathf.Clamp01(forwardSpeed / fullStrideSpeed);
            float sideSpeedWeight = Mathf.Clamp01(sideSpeed / fullStrideSpeed)
                * (1f - speedWeight * 0.5f);

            gaitWeight = Mathf.MoveTowards(
                gaitWeight,
                speedWeight,
                gaitBlendSpeed * Time.fixedDeltaTime);
            sideStepWeight = Mathf.MoveTowards(
                sideStepWeight,
                sideSpeedWeight,
                gaitBlendSpeed * Time.fixedDeltaTime);

            if (gaitWeight > 0.001f && forwardSpeed > 0.001f)
            {
                float travelDirection = Mathf.Sign(localVelocity.z);
                // Derive phase from distance travelled so cadence remains consistent
                // when move speed changes. fullStrideSpeed controls blend weight only.
                float cyclesPerSecond = forwardSpeed / strideLength;
                gaitPhase = Mathf.Repeat(
                    gaitPhase
                    + travelDirection
                    * cyclesPerSecond
                    * Mathf.PI
                    * 2f
                    * Time.fixedDeltaTime,
                    Mathf.PI * 2f
                );
            }

            if (sideSpeed > 0.05f)
            {
                float newDirection = Mathf.Sign(localVelocity.x);
                if (newDirection != sideStepDirection)
                    sideStepPhase = 0f;

                sideStepDirection = newDirection;
            }

            if (sideStepWeight > 0.001f)
            {
                float frequencyScale = Mathf.Lerp(0.75f, 1f, sideSpeedWeight);
                sideStepPhase = Mathf.Repeat(
                    sideStepPhase + Mathf.PI * 2f * sideStepFrequency
                        * frequencyScale * Time.fixedDeltaTime,
                    Mathf.PI * 2f);
            }
        }
    }
}

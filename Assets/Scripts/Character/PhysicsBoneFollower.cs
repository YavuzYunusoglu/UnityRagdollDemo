using UnityEngine;
using UnityEngine.Serialization;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class PhysicsBoneFollower : MonoBehaviour
{
    [Header("Chest (applied first)")]
    [SerializeField] private Rigidbody chestPhysicsBody;
    [SerializeField] private Transform chestTargetBone;

    [Header("Head (applied last)")]
    [FormerlySerializedAs("physicsBody")]
    [SerializeField] private Rigidbody headPhysicsBody;

    [FormerlySerializedAs("targetBone")]
    [SerializeField] private Transform headTargetBone;

    [Header("Left Arm")]
    [SerializeField] private Rigidbody leftUpperArmPhysicsBody;
    [SerializeField] private Transform leftUpperArmTargetBone;
    [SerializeField] private Rigidbody leftForearmPhysicsBody;
    [SerializeField] private Transform leftForearmTargetBone;

    [Header("Right Arm")]
    [SerializeField] private Rigidbody rightUpperArmPhysicsBody;
    [SerializeField] private Transform rightUpperArmTargetBone;
    [SerializeField] private Rigidbody rightForearmPhysicsBody;
    [SerializeField] private Transform rightForearmTargetBone;

    [Header("Left Leg")]
    [SerializeField] private Rigidbody leftThighPhysicsBody;
    [SerializeField] private Transform leftThighTargetBone;
    [SerializeField] private Rigidbody leftShinPhysicsBody;
    [SerializeField] private Transform leftShinTargetBone;
    [SerializeField] private Rigidbody leftFootPhysicsBody;
    [SerializeField] private Transform leftFootTargetBone;

    [Header("Right Leg")]
    [SerializeField] private Rigidbody rightThighPhysicsBody;
    [SerializeField] private Transform rightThighTargetBone;
    [SerializeField] private Rigidbody rightShinPhysicsBody;
    [SerializeField] private Transform rightShinTargetBone;
    [SerializeField] private Rigidbody rightFootPhysicsBody;
    [SerializeField] private Transform rightFootTargetBone;

    private Quaternion chestRotationOffset;
    private Quaternion leftUpperArmRotationOffset;
    private Quaternion leftForearmRotationOffset;
    private Quaternion rightUpperArmRotationOffset;
    private Quaternion rightForearmRotationOffset;
    private Quaternion leftThighRotationOffset;
    private Quaternion leftShinRotationOffset;
    private Quaternion leftFootRotationOffset;
    private Quaternion rightThighRotationOffset;
    private Quaternion rightShinRotationOffset;
    private Quaternion rightFootRotationOffset;
    private Quaternion headRotationOffset;

    private void Awake()
    {
        if (headPhysicsBody == null || headTargetBone == null)
        {
            Debug.LogError("PhysicsBoneFollower needs a Head Physics Body and Head Target Bone.", this);
            enabled = false;
            return;
        }

        if (!TryCalculateOptionalOffset(chestPhysicsBody, chestTargetBone,
                "Chest", out chestRotationOffset)
            || !TryCalculateOptionalOffset(leftUpperArmPhysicsBody, leftUpperArmTargetBone,
                "Left Upper Arm", out leftUpperArmRotationOffset)
            || !TryCalculateOptionalOffset(leftForearmPhysicsBody, leftForearmTargetBone,
                "Left Forearm", out leftForearmRotationOffset)
            || !TryCalculateOptionalOffset(rightUpperArmPhysicsBody, rightUpperArmTargetBone,
                "Right Upper Arm", out rightUpperArmRotationOffset)
            || !TryCalculateOptionalOffset(rightForearmPhysicsBody, rightForearmTargetBone,
                "Right Forearm", out rightForearmRotationOffset)
            || !TryCalculateOptionalOffset(leftThighPhysicsBody, leftThighTargetBone,
                "Left Thigh", out leftThighRotationOffset)
            || !TryCalculateOptionalOffset(leftShinPhysicsBody, leftShinTargetBone,
                "Left Shin", out leftShinRotationOffset)
            || !TryCalculateOptionalOffset(leftFootPhysicsBody, leftFootTargetBone,
                "Left Foot", out leftFootRotationOffset)
            || !TryCalculateOptionalOffset(rightThighPhysicsBody, rightThighTargetBone,
                "Right Thigh", out rightThighRotationOffset)
            || !TryCalculateOptionalOffset(rightShinPhysicsBody, rightShinTargetBone,
                "Right Shin", out rightShinRotationOffset)
            || !TryCalculateOptionalOffset(rightFootPhysicsBody, rightFootTargetBone,
                "Right Foot", out rightFootRotationOffset))
            return;

        headRotationOffset =
            Quaternion.Inverse(headPhysicsBody.transform.rotation) * headTargetBone.rotation;
    }

    private void LateUpdate()
    {
        // Parent bone must be written before its child in the same render frame.
        if (chestPhysicsBody != null)
        {
            chestTargetBone.rotation =
                chestPhysicsBody.transform.rotation * chestRotationOffset;
        }

        if (leftUpperArmPhysicsBody != null)
        {
            leftUpperArmTargetBone.rotation =
                leftUpperArmPhysicsBody.transform.rotation * leftUpperArmRotationOffset;
        }

        if (leftForearmPhysicsBody != null)
        {
            leftForearmTargetBone.rotation =
                leftForearmPhysicsBody.transform.rotation * leftForearmRotationOffset;
        }

        if (rightUpperArmPhysicsBody != null)
        {
            rightUpperArmTargetBone.rotation =
                rightUpperArmPhysicsBody.transform.rotation * rightUpperArmRotationOffset;
        }

        if (rightForearmPhysicsBody != null)
        {
            rightForearmTargetBone.rotation =
                rightForearmPhysicsBody.transform.rotation * rightForearmRotationOffset;
        }

        if (leftThighPhysicsBody != null)
        {
            leftThighTargetBone.rotation =
                leftThighPhysicsBody.transform.rotation * leftThighRotationOffset;
        }

        if (leftShinPhysicsBody != null)
        {
            leftShinTargetBone.rotation =
                leftShinPhysicsBody.transform.rotation * leftShinRotationOffset;
        }

        if (leftFootPhysicsBody != null)
        {
            leftFootTargetBone.rotation =
                leftFootPhysicsBody.transform.rotation * leftFootRotationOffset;
        }

        if (rightThighPhysicsBody != null)
        {
            rightThighTargetBone.rotation =
                rightThighPhysicsBody.transform.rotation * rightThighRotationOffset;
        }

        if (rightShinPhysicsBody != null)
        {
            rightShinTargetBone.rotation =
                rightShinPhysicsBody.transform.rotation * rightShinRotationOffset;
        }

        if (rightFootPhysicsBody != null)
        {
            rightFootTargetBone.rotation =
                rightFootPhysicsBody.transform.rotation * rightFootRotationOffset;
        }

        headTargetBone.rotation =
            headPhysicsBody.transform.rotation * headRotationOffset;
    }

    private bool TryCalculateOptionalOffset(
        Rigidbody physicsBody,
        Transform targetBone,
        string label,
        out Quaternion offset)
    {
        offset = Quaternion.identity;
        bool hasBody = physicsBody != null;
        bool hasBone = targetBone != null;

        if (hasBody != hasBone)
        {
            Debug.LogError($"Assign both {label} fields, or leave both empty.", this);
            enabled = false;
            return false;
        }

        if (hasBody)
        {
            offset = Quaternion.Inverse(physicsBody.transform.rotation) * targetBone.rotation;
        }

        return true;
    }
}

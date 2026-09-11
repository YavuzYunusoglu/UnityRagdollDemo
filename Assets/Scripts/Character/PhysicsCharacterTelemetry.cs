using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace RagdollDemo.Character
{
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class PhysicsCharacterTelemetry : MonoBehaviour
    {
        private static readonly string[] TrackedBodyNames =
        {
            "ChestPhysics",
            "ThighPhysics_L",
            "ThighPhysics_R",
            "ShinPhysics_L",
            "ShinPhysics_R",
            "FootPhysics_L",
            "FootPhysics_R"
        };

        private readonly List<Rigidbody> trackedBodies = new List<Rigidbody>();
        private readonly StringBuilder row = new StringBuilder(2048);
        private PhysicsCharacterMotor motor;
        private Rigidbody playerBody;
        private ShoulderCameraController view;
        private StreamWriter writer;
        private int fixedStep;
        private int samplesSinceFlush;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            PhysicsCharacterMotor activeMotor = FindFirstObjectByType<PhysicsCharacterMotor>();
            if (activeMotor != null
                && activeMotor.GetComponent<PhysicsCharacterTelemetry>() == null)
            {
                activeMotor.gameObject.AddComponent<PhysicsCharacterTelemetry>();
            }
        }

        private void Awake()
        {
            motor = GetComponent<PhysicsCharacterMotor>();
            playerBody = GetComponent<Rigidbody>();
            view = FindFirstObjectByType<ShoulderCameraController>();

            Rigidbody[] sceneBodies = FindObjectsByType<Rigidbody>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            foreach (string bodyName in TrackedBodyNames)
            {
                Rigidbody match = Array.Find(
                    sceneBodies,
                    candidate => candidate.name == bodyName);

                if (match != null)
                    trackedBodies.Add(match);
                else
                    Debug.LogWarning($"Physics telemetry could not find {bodyName}.", this);
            }

            string diagnosticsDirectory = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "Diagnostics"));
            Directory.CreateDirectory(diagnosticsDirectory);
            string outputPath = Path.Combine(
                diagnosticsDirectory,
                $"physics-telemetry-{DateTime.Now:yyyyMMdd-HHmmss}.csv");

            FileStream stream = new FileStream(
                outputPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.ReadWrite);
            writer = new StreamWriter(stream, new UTF8Encoding(false));
            WriteHeader();
            writer.Flush();
            Debug.Log($"Physics telemetry recording to: {outputPath}", this);
        }

        private void FixedUpdate()
        {
            if (writer == null || motor == null || playerBody == null)
                return;

            row.Clear();
            Append(fixedStep++);
            Append(Time.fixedTimeAsDouble);
            Append(Time.frameCount);
            Append(motor.LastMoveInput);
            Append(motor.LastDesiredVelocity);
            Append(motor.LastTargetAcceleration);
            Append(motor.LastAppliedAcceleration);
            Append(motor.IsGrounded ? 1 : 0);
            Append(motor.SteppedThisFixedUpdate ? 1 : 0);
            Append(motor.LastStepRise);
            Append(motor.AcceleratesConnectedBodies ? 1 : 0);
            Append(playerBody.linearVelocity);
            Append(playerBody.angularVelocity);

            foreach (Rigidbody trackedBody in trackedBodies)
            {
                Append(trackedBody.linearVelocity);

                Quaternion relativeRotation = Quaternion.Inverse(playerBody.rotation)
                    * trackedBody.rotation;
                Vector3 relativeEuler = relativeRotation.eulerAngles;
                relativeEuler.x = Mathf.DeltaAngle(0f, relativeEuler.x);
                relativeEuler.y = Mathf.DeltaAngle(0f, relativeEuler.y);
                relativeEuler.z = Mathf.DeltaAngle(0f, relativeEuler.z);
                Append(relativeEuler);

                Vector3 relativeAngularVelocity = playerBody.transform.InverseTransformDirection(
                    trackedBody.angularVelocity - playerBody.angularVelocity);
                Append(relativeAngularVelocity);
            }

            Vector3 cameraOffset = view != null
                ? view.transform.position - playerBody.position
                : Vector3.zero;
            Append(cameraOffset);
            Append(cameraOffset.magnitude, false);
            writer.WriteLine(row.ToString());

            if (++samplesSinceFlush >= 10)
            {
                writer.Flush();
                samplesSinceFlush = 0;
            }
        }

        private void OnDestroy()
        {
            writer?.Flush();
            writer?.Dispose();
            writer = null;
        }

        private void WriteHeader()
        {
            row.Clear();
            row.Append("step,fixed_time,frame,input_x,input_y,");
            row.Append("desired_vx,desired_vy,desired_vz,");
            row.Append("target_ax,target_ay,target_az,");
            row.Append("applied_ax,applied_ay,applied_az,grounded,stepped,step_rise,full_rig_acceleration,");
            row.Append("Player_vx,Player_vy,Player_vz,Player_avx,Player_avy,Player_avz");

            foreach (string bodyName in TrackedBodyNames)
            {
                row.Append(',').Append(bodyName).Append("_vx,");
                row.Append(bodyName).Append("_vy,");
                row.Append(bodyName).Append("_vz,");
                row.Append(bodyName).Append("_angle_x,");
                row.Append(bodyName).Append("_angle_y,");
                row.Append(bodyName).Append("_angle_z,");
                row.Append(bodyName).Append("_rel_avx,");
                row.Append(bodyName).Append("_rel_avy,");
                row.Append(bodyName).Append("_rel_avz");
            }

            row.Append(",camera_offset_x,camera_offset_y,camera_offset_z,camera_distance");
            writer.WriteLine(row.ToString());
        }

        private void Append(int value)
        {
            row.Append(value).Append(',');
        }

        private void Append(double value)
        {
            row.Append(value.ToString("R", CultureInfo.InvariantCulture)).Append(',');
        }

        private void Append(float value, bool trailingComma = true)
        {
            row.Append(value.ToString("R", CultureInfo.InvariantCulture));
            if (trailingComma)
                row.Append(',');
        }

        private void Append(Vector2 value)
        {
            Append(value.x);
            Append(value.y);
        }

        private void Append(Vector3 value)
        {
            Append(value.x);
            Append(value.y);
            Append(value.z);
        }
    }
}

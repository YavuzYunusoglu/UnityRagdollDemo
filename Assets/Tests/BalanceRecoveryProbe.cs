#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RagdollDemo.Character
{
    // Explicitly attached by the review command in Play mode; never installed in a build.
    [DefaultExecutionOrder(1100)]
    public sealed class BalanceRecoveryProbe : MonoBehaviour
    {
        [Tooltip("Diagnostics/RecoveryReview altında oluşacak kayıt dosyalarının ortak adı.")]
        public string RunLabel = "baseline";
        private MonoBehaviour balance;
        private Rigidbody root;
        private Rigidbody chest;
        private ConfigurableJoint[] joints;
        private StreamWriter writer;
        private float started;
        private bool kicked;
        private Quaternion previousRotation;
        private Vector3 previousVelocity;
        private Vector3 previousCamera;
        private float previousCameraSpeed;
        private StreamWriter cameraWriter;
        private Camera reviewCamera;
        private int screenshotIndex;
        private float nextScreenshot;
        [Tooltip("Açıkken recovery sırasında oyun ve sabit yan kamera PNG kareleri kaydeder.")]
        public bool CaptureFrames;
        [Tooltip("Test düşüşünü başlatmak için Player'a uygulanacak açısal hız değişimi (rad/s).")]
        public Vector3 Kick = new Vector3(3.5f, 0f, 0f);
        [Tooltip("Açı darbesi yerine yalnız sendeleme state'ini ve göğüs darbesini test eder.")]
        public bool StaggerOnly;
        [Tooltip("Kayıt süresi (saniye). Süre sonunda Play modunu kapatır.")]
        public float Duration = 10f;
        private void Start()
        {
            root = GetComponent<Rigidbody>();
            balance = GetComponents<MonoBehaviour>().First(m => m.GetType().Name == "PhysicsCharacterBalanceController");
            chest = GameObject.Find("ChestPhysics").GetComponent<Rigidbody>();
            joints = GameObject.Find("PhysicsRig").GetComponentsInChildren<ConfigurableJoint>();
            started = Time.time;
            previousRotation = root.rotation;
            previousVelocity = root.linearVelocity;
            writer = new StreamWriter("Diagnostics/RecoveryReview/" + RunLabel + ".csv");
            writer.WriteLine("time,state,rootY,chestY,tilt,rotationStep,speed,acceleration,maxAnchorError,drive,relativeSpeed,relativeAngularSpeed,cameraY,cameraSpeed,cameraAcceleration");
            cameraWriter = new StreamWriter("Diagnostics/RecoveryReview/" + RunLabel + "-camera.csv");
            cameraWriter.WriteLine("time,state,cameraY,chestY,pivotY,cameraSpeed,cameraAcceleration,dt");
            previousCamera = Camera.main.transform.position;
            if (CaptureFrames)
            {
                reviewCamera = new GameObject("Recovery Review Camera").AddComponent<Camera>();
                reviewCamera.enabled = false;
                reviewCamera.transform.position = root.position + new Vector3(4f, 2f, -4f);
                reviewCamera.transform.LookAt(root.position + new Vector3(0f, 0.8f, -0.6f));
                reviewCamera.fieldOfView = 45f;
            }
        }
        private float Field(string name) => (float)balance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(balance);
        private void FixedUpdate()
        {
            if (writer == null) return;
            float t = Time.time - started;
            if (!kicked && t >= 2f)
            {
                kicked = true;
                if (StaggerOnly) balance.SendMessage("DebugForceStagger");
                else
                {
                    balance.GetType().GetMethod("EnterFallen", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(balance, null);
                    root.AddTorque(root.rotation * Kick, ForceMode.VelocityChange);
                }
            }
            float error = 0f;
            foreach (var j in joints)
                if (j.connectedBody != null) error = Mathf.Max(error, Vector3.Distance(j.transform.TransformPoint(j.anchor), j.connectedBody.transform.TransformPoint(j.connectedAnchor)));
            var camera = Camera.main;
            float cameraSpeed = camera == null ? 0f : Vector3.Distance(previousCamera, camera.transform.position) / Time.fixedDeltaTime;
            object state = balance.GetType().GetProperty("CurrentState").GetValue(balance);
            var numbers = new[] { t, root.position.y, chest.position.y, Vector3.Angle(root.rotation * Vector3.up, Vector3.up), Quaternion.Angle(previousRotation, root.rotation), root.linearVelocity.magnitude, (root.linearVelocity - previousVelocity).magnitude / Time.fixedDeltaTime, error, Field("currentDriveScale"), Field("relativeLinearSpeed"), Field("relativeAngularSpeed"), camera == null ? 0f : camera.transform.position.y, cameraSpeed, Mathf.Abs(cameraSpeed - previousCameraSpeed) / Time.fixedDeltaTime };
            writer.WriteLine(numbers[0].ToString("F5", CultureInfo.InvariantCulture) + "," + state + "," + string.Join(",", numbers.Skip(1).Select(n => n.ToString("F5", CultureInfo.InvariantCulture))));
            previousRotation = root.rotation;
            previousVelocity = root.linearVelocity;
            if (t >= Duration)
            {
                writer.Dispose(); writer = null;
                Debug.Log("Recovery probe complete: " + RunLabel);
                UnityEditor.EditorApplication.isPlaying = false;
            }
        }
        private void LateUpdate()
        {
            if (cameraWriter == null || Camera.main == null || Time.deltaTime <= 0f) return;
            var camera = Camera.main;
            float speed = Vector3.Distance(previousCamera, camera.transform.position) / Time.deltaTime;
            var view = camera.GetComponent<ShoulderCameraController>();
            object state = balance.GetType().GetProperty("CurrentState").GetValue(balance);
            cameraWriter.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0:F5},{1},{2:F5},{3:F5},{4:F5},{5:F5},{6:F5},{7:F5}",
                Time.time-started,state,camera.transform.position.y,chest.transform.position.y,view.CurrentPivot.y,speed,Mathf.Abs(speed-previousCameraSpeed)/Time.deltaTime,Time.deltaTime));
            previousCamera = camera.transform.position;
            previousCameraSpeed = speed;
            if (reviewCamera != null && Time.time >= nextScreenshot && state.ToString() == "Recovering" && screenshotIndex < 12)
            {
                nextScreenshot = Time.time + 0.25f;
                SaveFrame(reviewCamera, "side");
                SaveFrame(camera, "game");
                screenshotIndex++;
            }
        }
        private void SaveFrame(Camera camera, string suffix)
        {
            var texture = RenderTexture.GetTemporary(800, 600, 24);
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var image = new Texture2D(800, 600, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = texture;
                camera.Render();
                RenderTexture.active = texture;
                image.ReadPixels(new Rect(0, 0, 800, 600), 0, 0);
                image.Apply();
                File.WriteAllBytes("Diagnostics/RecoveryReview/" + RunLabel + "-" + suffix + "-" + screenshotIndex.ToString("D2") + ".png", image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                RenderTexture.ReleaseTemporary(texture);
                Destroy(image);
            }
        }
        private void OnDestroy()
        {
            writer?.Dispose(); writer = null;
            cameraWriter?.Dispose(); cameraWriter = null;
            if (reviewCamera != null) Destroy(reviewCamera.gameObject);
        }
    }
}
#endif

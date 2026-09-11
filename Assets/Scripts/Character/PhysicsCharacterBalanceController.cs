using UnityEngine;

namespace RagdollDemo.Character
{
    [DefaultExecutionOrder(50)]
    [DisallowMultipleComponent]
    public class PhysicsCharacterBalanceController : MonoBehaviour
    {
        public enum BalanceState
        {
            Balanced,
            Staggering,
            Fallen,
            Recovering
        }

        private struct JointDriveSnapshot
        {
            public JointDrive angularX;
            public JointDrive angularYZ;
            public JointDrive slerp;
            public RotationDriveMode mode;

            public JointDriveSnapshot(ConfigurableJoint joint)
            {
                angularX = joint.angularXDrive;
                angularYZ = joint.angularYZDrive;
                slerp = joint.slerpDrive;
                mode = joint.rotationDriveMode;
            }
        }
        [Header("References")]
        [Tooltip("Player GameObject'inin ana Rigidbody'si. Denge açısı, kök hızı, düşme rotasyonu ve toparlanma konumu bu gövde üzerinden yönetilir.")]
        [SerializeField] private Rigidbody rootBody;

        [Tooltip("PhysicsRig altındaki ChestPhysics Rigidbody'si. Köke göre hız, açısal hız ve poz sapması darbe şiddetini ölçmek için kullanılır.")]
        [SerializeField] private Rigidbody chestBody;

        [Tooltip("Sahne kökündeki PhysicsRig Transform'u. Altındaki ConfigurableJoint ve Rigidbody'ler başlangıçta bulunup drive ölçekleme ve tüm rig'i kaldırma işlemlerinde kullanılır.")]
        [SerializeField] private Transform physicsRig;

        [Tooltip("Player üzerindeki PhysicsCharacterMotor. Düşüşte hareket kontrolünü geçici kapatır; toparlanma bitince yaw hedefini gövdenin yeni yönüyle eşitleyip kontrolü geri verir.")]
        [SerializeField] private PhysicsCharacterMotor motor;

        [Header("Impact Detection")]
        [Tooltip("Play başladıktan sonra darbe algılamasının devreye girmeden bekleyeceği süre (saniye). Başlangıç yerleşmesindeki fizik hareketlerinin yanlış sendeleme veya düşme tetiklemesini önler.")]
        [SerializeField, Min(0f)] private float startupGraceTime = 1f;

        [Tooltip("ChestPhysics ile Player arasındaki doğrusal hız farkı bu değere ulaştığında sendeleme başlar (m/s). Düşme eşiği önce kontrol edilir.")]
        [SerializeField, Min(0f)]
        private float staggerRelativeSpeed = 1f;

        [Tooltip("ChestPhysics ile Player arasındaki doğrusal hız farkı bu değere ulaştığında karakter doğrudan düşer (m/s).")]
        [SerializeField, Min(0f)]
        private float fallRelativeSpeed = 3f;

        [Tooltip("ChestPhysics ile Player arasındaki açısal hız farkı bu değere ulaştığında sendeleme başlar (rad/s).")]
        [SerializeField, Min(0f)]
        private float staggerRelativeAngularSpeed = 3f;

        [Tooltip("ChestPhysics ile Player arasındaki açısal hız farkı bu değere ulaştığında karakter doğrudan düşer (rad/s).")]
        [SerializeField, Min(0f)]
        private float fallRelativeAngularSpeed = 7f;

        [Tooltip("Göğsün başlangıçtaki köke göre pozundan sapması bu açıya ulaştığında sendeleme başlar (derece).")]
        [SerializeField, Range(0f, 180f)]
        private float staggerChestDeviation = 12f;

        [Tooltip("Göğsün başlangıçtaki köke göre pozundan sapması bu açıya ulaştığında karakter doğrudan düşer (derece).")]
        [SerializeField, Range(0f, 180f)]
        private float fallChestDeviation = 30f;

        [Header("Stagger")]
        [Tooltip("Sendeleme sırasında bütün joint drive değerlerine uygulanacak katsayı. 0 tamamen gevşek, 1 başlangıçtaki tam drive gücüdür.")]
        [SerializeField, Range(0f, 1f)]
        private float staggerDriveScale = 0.35f;

        [Tooltip("Daha büyük bir düşme eşiği aşılmazsa sendeleme durumunun recovery başlamadan önce süreceği zaman (saniye).")]
        [SerializeField, Min(0f)]
        private float staggerDuration = 0.35f;

        [Header("Fallen")]
        [Tooltip("Düşmüş durumda bütün joint drive değerlerine uygulanacak katsayı. Çok düşük değer daha gevşek ragdoll, yüksek değer pozu daha sıkı koruyan düşüş üretir.")]
        [SerializeField, Range(0f, 1f)]
        private float fallenDriveScale = 0.03f;

        [Tooltip("Karakter sakinleşmiş olsa bile recovery başlamadan önce yerde kalacağı en kısa süre (saniye).")]
        [SerializeField, Min(0f)]
        private float minimumFallenDuration = 1f;

        [Tooltip("Sakinleşme beklemesinin üst süresi (saniye). Bu süre dolsa bile yakın ve uygun bir zemin olmadan kalkış başlamaz.")]
        [SerializeField, Min(0f)]
        private float maximumFallenDuration = 3f;

        [Tooltip("Player ve bütün PhysicsRig gövdelerindeki en yüksek doğrusal hız bunun altındaysa doğrusal hareket sakinleşmiş sayılır (m/s).")]
        [SerializeField, Min(0f)]
        private float settledLinearSpeed = 0.8f;

        [Tooltip("Player ve bütün PhysicsRig gövdelerindeki en yüksek açısal hız bunun altındaysa dönüş hareketi sakinleşmiş sayılır (rad/s).")]
        [SerializeField, Min(0f)]
        private float settledAngularSpeed = 2.5f;

        [Header("Recovery")]
        [Tooltip("Toplanma ve doğrulma pozlarının hedef süresi (saniye). Sonunda fizik sakinleşene kadar kontrol geri verilmez.")]
        [SerializeField, Min(0.1f)]
        private float recoveryDuration = 1.65f;

        [Tooltip("Bu süre içinde kalkış tamamlanamazsa Fallen durumuna dönülür ve yeniden denenir. Zorla dikleştirme yapılmaz.")]
        [SerializeField, Min(0.1f)]
        private float maximumRecoveryDuration = 4f;

        [Tooltip("Düşüşten kalkarken Player kökünün son kararlı yöne doğru dönebileceği en yüksek hız (derece/saniye).")]
        [SerializeField, Min(0f)]
        private float recoveryRotationSpeed = 220f;

        [Tooltip("Recovery açısal hız kontrolünün sönümleme tepkisi (1/s). Rotasyonu doğrudan yazmaz; sınırlı tork uygular.")]
        [SerializeField, Min(0f)]
        private float recoveryAngularDamping = 12f;

        [Tooltip("Kalkış sırasında zeminden yardım alınabilecek en büyük açıklık (metre). Havada kalkmayı ve uzaktan yukarı çekilmeyi engeller.")]
        [SerializeField, Min(0f)]
        private float recoveryLiftDistance = 0.35f;

        [Tooltip("Kontrol geri verilmeden önce kökün dünya yukarı eksenine izin verilen eğimi (derece). Diğer hız ve sakinleşme koşulları da gerekir.")]
        [SerializeField, Range(0f, 45f)]
        private float uprightTolerance = 0.5f;

        [Header("Recovery Motion")]
        [Tooltip("Kalkışın ilk yarısında kökün ileri eğim açısı (derece). Değer büyüdükçe karakter daha belirgin şekilde öne ağırlık verir.")]
        [SerializeField, Range(0f, 35f)] private float recoveryForwardLean = 18f;
        [Tooltip("Kalkışta dizler toplanırken kapsülü yumuşakça kısaltır; ayaklar yerden kopmadan kalça alçalabilir.")]
        [SerializeField, Range(0f, 0.25f)] private float recoveryCrouchDepth = 0.14f;
        [Tooltip("Kökün hedef dikliğe ulaşmak için uygulayabileceği en yüksek açısal ivme (rad/s²). Değer büyüdükçe kalkış daha hızlı ve sert olur.")]
        [SerializeField, Min(1f)] private float recoveryAngularAcceleration = 45f;
        [Tooltip("Zemin desteğinin hedef yüksekliğe ve dikey hıza tepki verme süresi (saniye). Küçük değer daha sıkı, büyük değer daha yumuşak destek verir.")]
        [SerializeField, Min(0.01f)] private float supportResponseTime = 0.2f;
        [Tooltip("Zemin desteğinin bütün rig'e uygulayabileceği en yüksek ivme (m/s²). Kalkışta ani yukarı sıçramayı sınırlar.")]
        [SerializeField, Min(1f)] private float maximumSupportAcceleration = 35f;
        [Tooltip("Kalkış pozu tamamlandıktan sonra kök ve göğsün sakin kalması gereken süre (saniye). Bu süre dolmadan motor geri verilmez.")]
        [SerializeField, Min(0f)] private float recoverySettleTime = 0.18f;
        [Tooltip("Kalkıştan sonra kendi fizik hareketinin yeni bir düşüş olarak algılanmasını önleyen koruma süresi (saniye).")]
        [SerializeField, Min(0f)] private float postRecoveryGraceTime = 0.45f;
        [Tooltip("Fallen durumunda hız eşikleri ilk kez sağlandıktan sonra recovery başlamadan önce gerekli kesintisiz sakinlik süresi (saniye).")]
        [SerializeField, Min(0f)] private float fallenSettleTime = 0.18f;
        [Tooltip("Kalkışın destek aramasında kullanılacak zemin katmanları. Karakterin kendi PhysicsRig collider'ları otomatik olarak dışarıda bırakılır.")]
        [SerializeField] private LayerMask recoveryGroundMask = ~0;

        [Header("Debug Kicks")]
        [Tooltip("Debug/Force Stagger komutunda ChestPhysics'e son kararlı ileri yönde verilecek kütleden bağımsız hız değişimi (m/s).")]
        [SerializeField, Min(0f)]
        private float debugStaggerKick = 0.75f;

        [Tooltip("Debug/Force Fall komutunda Player'a sağ ekseni çevresinde verilecek kütleden bağımsız açısal hız değişimi (rad/s).")]
        [SerializeField, Min(0f)]
        private float debugFallAngularKick = 3.5f;

        [Header("Live Readout")]
        [Tooltip("State machine'in Play sırasında bulunduğu güncel durum. İzleme değeridir; çalışma anında controller tarafından yazılır.")]
        [SerializeField] private BalanceState currentState;

        [Tooltip("ChestPhysics'in başlangıçtaki köke göre rotasyonundan güncel sapması (derece). İzleme değeridir.")]
        [SerializeField] private float chestDeviation;

        [Tooltip("ChestPhysics ile Player arasındaki güncel doğrusal hız farkının büyüklüğü (m/s). İzleme değeridir.")]
        [SerializeField] private float relativeLinearSpeed;

        [Tooltip("ChestPhysics ile Player arasındaki güncel açısal hız farkının büyüklüğü (rad/s). İzleme değeridir.")]
        [SerializeField] private float relativeAngularSpeed;

        [Tooltip("Player'ın yukarı ekseni ile dünya yukarı ekseni arasındaki güncel açı (derece). Darbe eşiği değildir; recovery sonunda diklik ayrıca kontrol edilir.")]
        [SerializeField] private float rootTilt;

        [Tooltip("Başlangıç joint drive değerlerine uygulanan güncel katsayı. 1 tam güç, 0 tamamen kapalı drive anlamına gelir. İzleme değeridir.")]
        [SerializeField] private float currentDriveScale = 1f;

        private ConfigurableJoint[] joints;
        private Rigidbody[] rigBodies;
        private JointDriveSnapshot[] defaultDrives;

        private RigidbodyConstraints defaultRootConstraints;
        private Quaternion restChestRelativeRotation;
        private Quaternion recoveryTargetRotation;

        private Vector3 lastStableForward = Vector3.forward;

        private float runtime;
        private float stateTime;
        private float recoveryStartDrive;
        private float settledTime;
        private float impactGraceRemaining;
        private Quaternion recoveryStartRotation;
        private CapsuleCollider rootCapsule;
        private readonly RaycastHit[] supportHits = new RaycastHit[32];
        private readonly System.Collections.Generic.HashSet<Rigidbody> characterBodies = new System.Collections.Generic.HashSet<Rigidbody>();
        private bool ownsMotor;
        private float standingCapsuleHeight;
        private float crouchAmount;
        private ConfigurableJoint chestJoint;
        private SoftJointLimit standingChestLowLimit;
        private SoftJointLimit standingChestHighLimit;

        private bool initialized;
        private bool recoveringFromFall;
        private bool motorWasEnabledBeforeFall;

        public BalanceState CurrentState => currentState;
        public Transform ChestTransform => chestBody != null ? chestBody.transform : null;
        public bool IsPhysicalFall => initialized && isActiveAndEnabled &&
            (currentState == BalanceState.Fallen || (currentState == BalanceState.Recovering && recoveringFromFall));
        public float RecoveryProgress => currentState == BalanceState.Recovering
            ? Mathf.Clamp01(stateTime / Mathf.Max(0.1f, recoveryDuration)) : 0f;
        public float RecoveryPoseWeight => IsPhysicalFall && currentState == BalanceState.Recovering
            ? SmoothRange(RecoveryProgress, 0f, 0.3f) * (1f - SmoothRange(RecoveryProgress, 0.65f, 1f)) : 0f;
        public bool OwnsBody(Rigidbody body) => body != null && characterBodies.Contains(body);

        private void Awake()
        {
            if (rootBody == null) rootBody = GetComponent<Rigidbody>();
            if (motor == null) motor = GetComponent<PhysicsCharacterMotor>();

            if (rootBody == null || chestBody == null || physicsRig == null || motor == null)
            {
                Debug.LogError("Balance Controller needs Root Body, Chest Body, Physics Rig and motor.", this);
                enabled = false;
                return;
            }

            joints = physicsRig.GetComponentsInChildren<ConfigurableJoint>(true);
            rigBodies = physicsRig.GetComponentsInChildren<Rigidbody>(true);
            rootCapsule = rootBody.GetComponent<CapsuleCollider>();
            if (rootCapsule != null) standingCapsuleHeight = rootCapsule.height;
            chestJoint = chestBody.GetComponent<ConfigurableJoint>();
            if (chestJoint != null)
            {
                standingChestLowLimit = chestJoint.lowAngularXLimit;
                standingChestHighLimit = chestJoint.highAngularXLimit;
            }
            characterBodies.Add(rootBody);
            foreach (Rigidbody body in rigBodies) characterBodies.Add(body);

            if (joints.Length == 0)
            {
                Debug.LogError("No configurable joints were found under physics rig.", this);
                enabled = false;
                return;
            }

            defaultDrives = new JointDriveSnapshot[joints.Length];
            for (int i = 0; i < joints.Length; i++)
                defaultDrives[i] = new JointDriveSnapshot(joints[i]);

            defaultRootConstraints = rootBody.constraints;

            restChestRelativeRotation = Quaternion.Inverse(rootBody.rotation) * chestBody.rotation;
            UpdateLastStableForward();
            ApplyDriveScale(1f);

            currentState = BalanceState.Balanced;
            initialized = true;
        }

        private void FixedUpdate()
        {
            if (!initialized)
                return;

            float deltatime = Time.fixedDeltaTime;

            runtime += deltatime;
            stateTime += deltatime;
            impactGraceRemaining = Mathf.Max(0f, impactGraceRemaining - deltatime);

            UpdateMeasurements();

            switch (CurrentState)
            {
                case BalanceState.Balanced:
                    TickBalanced(); break;
                case BalanceState.Staggering:
                    TickStaggering(); break;
                case BalanceState.Fallen:
                    TickFallen(); break;
                case BalanceState.Recovering:
                    TickRecovering(deltatime); break;
            }
            UpdateRecoveryCapsule(deltatime);
        }

        private void TickBalanced()
        {
            UpdateLastStableForward();
            if (runtime < startupGraceTime || impactGraceRemaining > 0f)
                return;

            if (IsFallImpact())
            {
                EnterFallen();
                return;
            }
            if (IsStaggerImpact())
                EnterStaggering();
        }

        private void TickStaggering()
        {
            if (IsFallImpact())
            {
                EnterFallen();
                return;
            }
            if (stateTime >= staggerDuration)
                BeginRecovery(false);
        }

        private void TickFallen()
        {
            bool minimumTimePassed = stateTime >= minimumFallenDuration;

            bool settled = GetMaximumLinearSpeed() <= settledLinearSpeed && GetMaximumAngularSpeed() <= settledAngularSpeed;

            bool timedOut = stateTime >= maximumFallenDuration;

            settledTime = settled ? settledTime + Time.fixedDeltaTime : 0f;
            // A timeout must never turn a mid-air ragdoll into a flying upright character.
            if (minimumTimePassed && (settledTime >= fallenSettleTime || timedOut)
                && TryGetSupport(out _, out float gap) && gap <= recoveryLiftDistance)
                BeginRecovery(true);
        }

        private void TickRecovering(float deltatime)
        {
            float progress = RecoveryProgress;
            ApplyDriveScale(Mathf.Lerp(recoveryStartDrive, 1f, SmoothRange(progress, 0.1f, 1f)));

            if (!recoveringFromFall)
            {
                if (currentDriveScale >= 0.999f)
                    FinishRecovery();

                return;
            }

            if (!TryGetSupport(out RaycastHit support, out float gap) || gap > recoveryLiftDistance + 0.15f)
            {
                EnterFallen();
                return;
            }

            // Gather the limbs first, bring the torso over them, then extend to standing.
            Quaternion crouched = recoveryTargetRotation * Quaternion.Euler(recoveryForwardLean, 0f, 0f);
            Quaternion gatherTarget = Quaternion.Slerp(recoveryStartRotation, crouched, SmoothRange(progress, 0.06f, 0.65f));
            Quaternion target = Quaternion.Slerp(gatherTarget, recoveryTargetRotation, SmoothRange(progress, 0.55f, 1f));
            float roll = 16f * SmoothRange(progress, 0f, 0.3f) * (1f - SmoothRange(progress, 0.35f, 0.75f));
            target = Quaternion.AngleAxis(roll, lastStableForward) * target;
            ApplyRecoveryTorque(target, deltatime);
            ApplyGroundSupport(support, gap);

            // Yaw is intentionally constrained while fallen. Pitch/roll recovery must not
            // wait for a heading error that only the locomotion motor can resolve.
            float uprightError = Vector3.Angle(rootBody.rotation * Vector3.up, Vector3.up);

            bool recoveredNormally = progress >= 1f && uprightError <= uprightTolerance
                && rootBody.angularVelocity.magnitude < 0.4f && relativeLinearSpeed < settledLinearSpeed
                && chestDeviation < Mathf.Min(staggerChestDeviation, 5f) && rootBody.linearVelocity.magnitude < 0.3f;
            settledTime = recoveredNormally ? settledTime + deltatime : 0f;

            bool recoveryTimedOut = stateTime >= maximumRecoveryDuration;

            if (settledTime >= recoverySettleTime && recoveredNormally)
                FinishRecovery();
            else if (recoveryTimedOut)
                EnterFallen(); // Blocked recovery retries from physics; never teleport upright.
        }

        private void EnterStaggering()
        {
            if (IsPhysicalFall) return;
            SetState(BalanceState.Staggering);
            ApplyDriveScale(staggerDriveScale);
        }

        private void EnterFallen()
        {
            if (currentState == BalanceState.Fallen)
                return;

            if (!ownsMotor)
            {
                motorWasEnabledBeforeFall = motor.enabled;
                ownsMotor = true;
                motor.enabled = false;
            }
            recoveringFromFall = false;

            RigidbodyConstraints fallConstraints = defaultRootConstraints;

            fallConstraints &= ~(RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ);

            fallConstraints |= RigidbodyConstraints.FreezeRotationY;

            rootBody.constraints = fallConstraints;

            SetState(BalanceState.Fallen);
            settledTime = 0f;
            ApplyDriveScale(fallenDriveScale);
        }

        private void BeginRecovery(bool fromFall)
        {
            recoveringFromFall = fromFall;
            recoveryStartDrive = currentDriveScale;
            settledTime = 0f;

            if (fromFall)
            {
                recoveryTargetRotation = Quaternion.LookRotation(lastStableForward, Vector3.up);

                recoveryStartRotation = rootBody.rotation;
            }
            SetState(BalanceState.Recovering);
        }

        private void FinishRecovery()
        {
            bool wasRecoveringFromFall = recoveringFromFall;

            if (wasRecoveringFromFall)
            {
                rootBody.constraints = defaultRootConstraints;

                motor.SynchronizeControlledYawToBody();

                if (motorWasEnabledBeforeFall)
                    motor.enabled = true;
                ownsMotor = false;
            }
            recoveringFromFall = false;
            impactGraceRemaining = postRecoveryGraceTime;

            ApplyDriveScale(1f);
            SetState(BalanceState.Balanced);
        }
        private void UpdateMeasurements()
        {
            relativeLinearSpeed = (chestBody.linearVelocity - rootBody.linearVelocity).magnitude;

            relativeAngularSpeed = (chestBody.angularVelocity - rootBody.angularVelocity).magnitude;

            Quaternion currentChestRelativeRotation = Quaternion.Inverse(rootBody.rotation) * chestBody.rotation;

            chestDeviation = Quaternion.Angle(restChestRelativeRotation, currentChestRelativeRotation);

            rootTilt = Vector3.Angle(rootBody.transform.up, Vector3.up);

        }

        private bool IsStaggerImpact()
        {
            return relativeLinearSpeed >= staggerRelativeSpeed || relativeAngularSpeed >= staggerRelativeAngularSpeed || chestDeviation >= staggerChestDeviation;
        }

        private bool IsFallImpact()
        {
            return relativeLinearSpeed >= fallRelativeSpeed || relativeAngularSpeed >= fallRelativeAngularSpeed || chestDeviation >= fallChestDeviation;
        }

        private void UpdateLastStableForward()
        {
            Vector3 planarForward = Vector3.ProjectOnPlane(rootBody.transform.forward, Vector3.up);

            if (planarForward.sqrMagnitude > 0.001f)
                lastStableForward = planarForward.normalized;
        }

        private void ApplyDriveScale(float scale)
        {
            currentDriveScale = Mathf.Clamp01(scale);

            for (int i = 0; i < joints.Length; i++)
            {
                ConfigurableJoint joint = joints[i];
                if (joint == null) continue;

                JointDriveSnapshot source = defaultDrives[i];

                bool recoveryDrive = currentState == BalanceState.Recovering && recoveringFromFall;
                bool needsAxisDrive = recoveryDrive && source.mode == RotationDriveMode.Slerp
                    && (joint.angularXMotion == ConfigurableJointMotion.Locked
                        || joint.angularYMotion == ConfigurableJointMotion.Locked
                        || joint.angularZMotion == ConfigurableJointMotion.Locked);
                joint.rotationDriveMode = needsAxisDrive ? RotationDriveMode.XYAndZ : source.mode;
                float scaleForJoint = recoveryDrive
                    ? Mathf.Max(currentDriveScale, Mathf.Lerp(fallenDriveScale, 0.65f, SmoothRange(RecoveryProgress, 0f, 0.25f)))
                    : currentDriveScale;

                joint.angularXDrive = ScaleDrive(needsAxisDrive ? source.slerp : source.angularX, scaleForJoint);

                joint.angularYZDrive = ScaleDrive(needsAxisDrive ? source.slerp : source.angularYZ, scaleForJoint);

                joint.slerpDrive = ScaleDrive(source.slerp, scaleForJoint);
            }

        }
        private static JointDrive ScaleDrive(JointDrive source, float scale)
        {
            source.positionSpring *= scale;
            // Spring strength scales linearly; critical damping scales with sqrt(k).
            // Scaling both linearly made the weak-drive recovery severely underdamped.
            source.positionDamper *= Mathf.Sqrt(scale);
            source.maximumForce *= scale;
            return source;
        }

        private float GetMaximumLinearSpeed()
        {
            float maximum = rootBody.linearVelocity.magnitude;

            foreach (Rigidbody body in rigBodies)
            {
                if (body != null)
                    maximum = Mathf.Max(maximum, body.linearVelocity.magnitude);
            }
            return maximum;
        }

        private float GetMaximumAngularSpeed()
        {
            float maximum = rootBody.angularVelocity.magnitude;

            foreach (Rigidbody body in rigBodies)
            {
                if (body != null)
                    maximum = Mathf.Max(maximum, body.angularVelocity.magnitude);
            }
            return maximum;
        }

        private static float SmoothRange(float value, float start, float end)
        {
            float t = Mathf.InverseLerp(start, end, value);
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        private void ApplyRecoveryTorque(Quaternion target, float dt)
        {
            Quaternion error = target * Quaternion.Inverse(rootBody.rotation);
            if (error.w < 0f) error = new Quaternion(-error.x, -error.y, -error.z, -error.w);
            error.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle < 0.01f || axis.sqrMagnitude < 0.001f) axis = Vector3.zero;
            Vector3 desiredVelocity = axis * Mathf.Min(angle * Mathf.Deg2Rad * 6f, recoveryRotationSpeed * Mathf.Deg2Rad);
            Vector3 acceleration = (desiredVelocity - rootBody.angularVelocity)
                * Mathf.Min(recoveryAngularDamping, 1f / dt);
            acceleration = Vector3.ClampMagnitude(acceleration, recoveryAngularAcceleration);
            // Convert desired angular acceleration through the body's world inertia tensor.
            // The tall capsule has very different inertia on its long and short axes.
            Quaternion inertiaRotation = rootBody.rotation * rootBody.inertiaTensorRotation;
            Vector3 torque = inertiaRotation * Vector3.Scale(rootBody.inertiaTensor,
                Quaternion.Inverse(inertiaRotation) * acceleration);
            rootBody.AddTorque(torque, ForceMode.Force);
        }

        private bool TryGetSupport(out RaycastHit support, out float gap)
        {
            support = default;
            gap = float.PositiveInfinity;
            if (rootCapsule == null) return false;
            Vector3 scale = rootCapsule.transform.lossyScale;
            float radius = rootCapsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float halfLine = Mathf.Max(0f, rootCapsule.height * Mathf.Abs(scale.y) * 0.5f - radius);
            Vector3 center = rootBody.position + rootBody.rotation * Vector3.Scale(rootCapsule.center, scale);
            float bottom = center.y - radius - halfLine * Mathf.Abs((rootBody.rotation * Vector3.up).y);
            int count = Physics.RaycastNonAlloc(center + Vector3.up * 0.25f, Vector3.down,
                supportHits, halfLine + radius + recoveryLiftDistance + 0.65f,
                recoveryGroundMask, QueryTriggerInteraction.Ignore);
            float closest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = supportHits[i];
                if (OwnsBody(hit.rigidbody) || hit.normal.y < Mathf.Cos(55f * Mathf.Deg2Rad) || hit.distance >= closest) continue;
                closest = hit.distance;
                support = hit;
            }
            if (float.IsPositiveInfinity(closest)) return false;
            gap = bottom - support.point.y;
            return true;
        }

        private void ApplyGroundSupport(RaycastHit support, float gap)
        {
            // Accelerate every connected body together. Forces preserve contacts and interpolation.
            // Only compensate gravity / small contact gaps; never translate the rig by a fixed lift.
            float response = Mathf.Max(0.08f, supportResponseTime);
            float platformSpeed = support.rigidbody != null ? support.rigidbody.GetPointVelocity(support.point).y : 0f;
            float acceleration = (0.025f - gap) / (response * response)
                - 2f * (rootBody.GetPointVelocity(rootBody.worldCenterOfMass).y - platformSpeed) / response
                - Physics.gravity.y;
            Vector3 force = Vector3.up * Mathf.Clamp(acceleration, -maximumSupportAcceleration, maximumSupportAcceleration);
            // Settle horizontal momentum before handing back to the normal 35 m/s² brake.
            Vector3 platformVelocity = support.rigidbody != null ? support.rigidbody.GetPointVelocity(support.point) : Vector3.zero;
            Vector3 planarVelocity = Vector3.ProjectOnPlane(rootBody.linearVelocity - platformVelocity, Vector3.up);
            force -= Vector3.ClampMagnitude(planarVelocity * (5f * SmoothRange(RecoveryProgress, 0.65f, 1f)), 10f);
            foreach (Rigidbody body in characterBodies)
                if (body != null && !body.isKinematic) body.AddForce(force, ForceMode.Acceleration);
        }

        private void UpdateRecoveryCapsule(float dt)
        {
            if (chestJoint != null)
            {
                bool recovering = currentState == BalanceState.Recovering && recoveringFromFall;
                var low = standingChestLowLimit;
                var high = standingChestHighLimit;
                if (recovering)
                {
                    low.limit = Mathf.Min(low.limit, -32f);
                    high.limit = Mathf.Max(high.limit, 32f);
                }
                chestJoint.lowAngularXLimit = low;
                chestJoint.highAngularXLimit = high;
            }
            if (rootCapsule == null) return;
            crouchAmount = Mathf.MoveTowards(crouchAmount, RecoveryPoseWeight * recoveryCrouchDepth, dt * 0.5f);
            float scaleY = Mathf.Max(0.001f, Mathf.Abs(rootCapsule.transform.lossyScale.y));
            rootCapsule.height = Mathf.Max(rootCapsule.radius * 2f, standingCapsuleHeight - 2f * crouchAmount / scaleY);
        }

        private void OnDisable()
        {
            if (!initialized) return;
            recoveringFromFall = false;
            ApplyDriveScale(1f);
            if (rootCapsule != null) rootCapsule.height = standingCapsuleHeight;
            if (chestJoint != null)
            {
                chestJoint.lowAngularXLimit = standingChestLowLimit;
                chestJoint.highAngularXLimit = standingChestHighLimit;
            }
            crouchAmount = 0f;
            if (ownsMotor)
            {
                rootBody.constraints = defaultRootConstraints;
                motor.SynchronizeControlledYawToBody();
                motor.enabled = motorWasEnabledBeforeFall;
                ownsMotor = false;
            }
            recoveringFromFall = false;
            currentState = BalanceState.Balanced;
            impactGraceRemaining = postRecoveryGraceTime;
        }

        private void SetState(BalanceState nextState)
        {
            if (CurrentState == nextState)
                return;

            currentState = nextState;
            stateTime = 0f;

            Debug.Log($"Balance State changed to {nextState}", this);
        }

        [ContextMenu("Debug/Force Stagger")]
        private void DebugForceStagger()
        {
            if (!Application.isPlaying || !initialized) return;

            EnterStaggering();

            chestBody.AddForce(lastStableForward * debugStaggerKick, ForceMode.VelocityChange);
        }

        [ContextMenu("Debug/Force Fall")]
        private void DebugForceFall()
        {
            if (!Application.isPlaying || !initialized) return;

            EnterFallen();

            rootBody.AddTorque(rootBody.transform.right * debugFallAngularKick, ForceMode.VelocityChange);
        }
    }


}


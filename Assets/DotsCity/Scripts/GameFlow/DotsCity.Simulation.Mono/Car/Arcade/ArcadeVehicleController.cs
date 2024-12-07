using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Mono
{
    // Code based on https://github.com/mrgarcialuigi/Arcade-Vehicle-Controller
    public class ArcadeVehicleController : MonoBehaviour
    {
        private const float AlmostStoppingSpeed = 2.0f;
        private const float AlmostStoppingBrakingRatio = 1.0f;
        private const float NoAccelerationBrakingRatio = 0.1f;

        [Serializable]
        public class WheelData
        {
            public Transform WheelTransform;
            public bool Driving = true;
            public bool Braking = true;

            public Vector3 SpringOriginLocalPos { get; set; }
            public bool IsSteering { get; set; }
            public float CurrentSuspensionLength { get; set; }
            public float CurrentSpringVelocity { get; set; }
            public Quaternion RollRot { get; set; }
            public int InversionValue { get; set; }
        }

        [Header("Body")]
        [SerializeField] private float m_ChassiMass = 60;

        [SerializeField] private float m_TireMass = 3;

        [SerializeField] private float wheelRadius = 0.4f;
        [SerializeField] private LayerMask raycastLayer = 0;

        [Header("Suspension")]
        [SerializeField] private float m_SpringRestLength = 0.8f;
        [SerializeField] private float m_SpringStrength = 1200;
        [SerializeField] private float m_SpringDamper = 75;

        [Header("Power")]
        [SerializeField] private float m_AcceleratePower = 300;

        [Tooltip("Braking power")]
        [SerializeField] private float m_BrakesPower = 0.1f;

        [Tooltip("Hand brake power")]
        [SerializeField] private float m_HandBrakesPower = 1;

        [Tooltip("Additional brake power when the car almost stopped")]
        [SerializeField] private float m_StoppedBrakesPower = 5;

        [Tooltip("Max speed in km/h")]
        [SerializeField] private float m_MaxSpeed = 80;

        [Tooltip("Max reverse speed in km/h")]
        [SerializeField] private float m_MaxReverseSpeed = 45;

        [SerializeField] private float antiRollForce = 100;

        [Header("Handling")]
        [SerializeField][Range(0.0f, 60.0f)] private float m_SteerAngle = 30f;

        [Tooltip("Auxiliary variable for the increase of the turning power")]
        [SerializeField][Range(1f, 2f)] private float steeringPowerFactor = 1;

        [SerializeField][Range(0.0f, 10.0f)] private float m_FrontWheelsGripFactor = 1.8f;
        [SerializeField][Range(0.0f, 10.0f)] private float m_RearWheelsGripFactor = 2f;

        [Header("Other")]
        [SerializeField] private float m_AirResistance = 5;
        [SerializeField] private List<Transform> steeringWheels = new List<Transform>();
        [SerializeField] private List<WheelData> allWheels = new List<WheelData>();

        private Transform m_Transform;
        private BoxCollider m_BoxCollider;
        private Rigidbody m_Rigidbody;

        private List<WheelData> accWheels = new List<WheelData>();
        private List<WheelData> brakeWheels = new List<WheelData>();
        private float colliderMagnitude;
        private IVehicleInput vehicleInput;
        private bool hasInput;

        public float WheelRadius { get => wheelRadius; set => wheelRadius = value; }

        public float SteerAngle { get => m_SteerAngle; set => m_SteerAngle = value; }

        public float SpringRestLength { get => m_SpringRestLength; set => m_SpringRestLength = value; }

        public float MaxSpeed { get; private set; }

        public float MaxReverseSpeed { get; private set; }

        public float Speed { get; private set; }

        public int Direction { get; private set; }

        public Vector3 Forward => m_Transform.forward;

#if UNITY_6000_0_OR_NEWER
        public Vector3 Velocity => m_Rigidbody.linearVelocity;
#else
        public Vector3 Velocity => m_Rigidbody.velocity;
#endif

        private float Throttle => vehicleInput.Throttle;
        private float Steering => vehicleInput.Steering;
        private bool Handbrake => vehicleInput.Handbrake;

        private void Awake()
        {
            vehicleInput = GetComponent<IVehicleInput>();
            m_Transform = transform;
            InitializeCollider();
            InitializeBody();
            InitializeWheels();
            InitMisc();
        }

        private void FixedUpdate()
        {
            UpdateSuspension();

            UpdateSteering();

            UpdateAccelerate();

            UpdateBrakes();

            UpdateAirResistance();
        }

        private void Update()
        {
            CalcData();
            UpdateWheels();
        }

        public void AddWheel(Transform wheel, bool steering = false, bool driving = true, bool braking = true)
        {
            if (!wheel)
                throw new NullReferenceException();

            if (steering)
            {
                steeringWheels.TryToAdd(wheel);
            }

            if (!allWheels.Any(a => a.WheelTransform == wheel))
            {
                allWheels.Add(new WheelData()
                {
                    WheelTransform = wheel,
                    Driving = driving,
                    Braking = braking
                });
            }
        }

        public void InitializeBody()
        {
            if (!TryGetComponent(out m_Rigidbody))
            {
                m_Rigidbody = gameObject.AddComponent<Rigidbody>();
                m_Rigidbody.isKinematic = false;
                m_Rigidbody.useGravity = true;
                m_Rigidbody.automaticCenterOfMass = false;

                var collider = GetComponent<BoxCollider>();

                if (collider != null)
                {
                    m_Rigidbody.centerOfMass = new Vector3(0, 0, collider.center.z);
                }

#if UNITY_6000_0_OR_NEWER
                m_Rigidbody.linearDamping = 0.0f;
                m_Rigidbody.angularDamping = 0.0f;
#else
                m_Rigidbody.drag = 0.0f;
                m_Rigidbody.angularDrag = 0.0f;
#endif

                m_Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                m_Rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                m_Rigidbody.constraints = RigidbodyConstraints.None;
                EditorSaver.SetObjectDirty(m_Rigidbody);
            }

            m_Rigidbody.mass = m_ChassiMass + m_TireMass * allWheels.Count;
        }

        public void InitializeCollider()
        {
            if (!TryGetComponent(out m_BoxCollider))
            {
                m_BoxCollider = gameObject.AddComponent<BoxCollider>();
            }

            colliderMagnitude = m_BoxCollider.size.magnitude;
        }

        public void InitializeCollider(MeshRenderer meshRenderer)
        {
            if (!TryGetComponent(out m_BoxCollider))
            {
                m_BoxCollider = gameObject.AddComponent<BoxCollider>();
            }

            m_BoxCollider.size = meshRenderer.bounds.size;
            m_BoxCollider.center = meshRenderer.bounds.center;
            colliderMagnitude = m_BoxCollider.size.magnitude;
        }

        private void InitializeWheels()
        {
            if (m_SpringRestLength == 0)
            {
                Debug.LogError($"Vehicle '{name}' spring rest length is 0");
            }

            if (wheelRadius == 0)
            {
                Debug.LogError($"Vehicle '{name}' wheel radius is 0");
            }

            foreach (var wheel in allWheels)
            {
                wheel.SpringOriginLocalPos = GetInitialSpringPoint(wheel);
                wheel.InversionValue = Mathf.Abs(wheel.WheelTransform.localRotation.eulerAngles.y) < 1f ? 1 : -1;
                wheel.CurrentSuspensionLength = m_SpringRestLength;
                wheel.RollRot = wheel.WheelTransform.localRotation;

                if (steeringWheels.Contains(wheel.WheelTransform))
                {
                    wheel.IsSteering = true;
                }

                if (wheel.Driving)
                {
                    accWheels.Add(wheel);
                }

                if (wheel.Braking)
                {
                    brakeWheels.Add(wheel);
                }
            }
        }

        private void InitMisc()
        {
            MaxSpeed = m_MaxSpeed / ProjectConstants.KmhToMs_RATE;
            MaxReverseSpeed = m_MaxReverseSpeed / ProjectConstants.KmhToMs_RATE;
        }

        private Vector3 GetInitialSpringPoint(WheelData wheel)
        {
            return transform.InverseTransformPoint(wheel.WheelTransform.position) + new Vector3(0, m_SpringRestLength);
        }

        private void CastSpring(WheelData wheel)
        {
            Vector3 position = GetSpringPosition(wheel);

            float previousLength = wheel.CurrentSuspensionLength;

            float currentLength;

            if (Physics.Raycast(position, -m_Transform.up, out var hit, m_SpringRestLength + wheelRadius, raycastLayer.value, QueryTriggerInteraction.Ignore))
            {
                currentLength = hit.distance - wheelRadius;
            }
            else
            {
                currentLength = m_SpringRestLength;
            }

            wheel.CurrentSpringVelocity = (currentLength - previousLength) / Time.fixedDeltaTime;
            wheel.CurrentSuspensionLength = currentLength;
        }

        private Vector3 GetSpringPosition(WheelData wheel)
        {
            return transform.TransformPoint(wheel.SpringOriginLocalPos);
        }

        private Vector3 GetSpringHitPosition(WheelData wheel)
        {
            Vector3 vehicleDown = -m_Transform.up;
            return GetSpringPosition(wheel) + wheel.CurrentSuspensionLength * vehicleDown;
        }

        private Vector3 GetWheelWorldPosition(WheelData wheel)
        {
            return GetSpringHitPosition(wheel);
        }

        private Vector3 GetWheelRollDirection(WheelData wheel, bool steeringForce = false)
        {
            if (wheel.IsSteering)
            {
                float factor = 1;

                if (steeringForce)
                {
                    factor = steeringPowerFactor;
                }

                var steerQuaternion = Quaternion.AngleAxis(Steering * m_SteerAngle * factor, Vector3.up);
                return steerQuaternion * m_Transform.forward;
            }
            else
            {
                return m_Transform.forward;
            }
        }

        private Vector3 GetWheelSlideDirection(WheelData wheel)
        {
            Vector3 forward = GetWheelRollDirection(wheel, true);
            return Vector3.Cross(m_Transform.up, forward);
        }

        private Vector3 GetWheelTorquePosition(WheelData wheel)
        {
            return wheel.WheelTransform.position;
        }

        private float GetWheelGripFactor(WheelData wheel)
        {
            return wheel.IsSteering ? m_FrontWheelsGripFactor : m_RearWheelsGripFactor;
        }

        private bool IsGrounded(WheelData wheel)
        {
            return wheel.CurrentSuspensionLength < m_SpringRestLength;
        }

        private void CalcData()
        {
            hasInput = !Mathf.Approximately(Throttle, 0.0f);

            var speed = Vector3.Dot(transform.forward, Velocity);
            Direction = speed >= 0 ? 1 : -1;
            Speed = Mathf.Abs(speed);
        }

        private void UpdateSuspension()
        {
            for (int i = 0; i < allWheels.Count; i++)
            {
                WheelData wheel = allWheels[i];
                CastSpring(wheel);
                float currentLength = wheel.CurrentSuspensionLength;
                float currentVelocity = wheel.CurrentSpringVelocity;

                float antiRoll = 0;

                if (antiRollForce > 0)
                {
                    float travelL = 1.0f;
                    float travelR = 1.0f;

                    int nextAxisWheelIndex = i % 2 == 0 ? i + 1 : i - 1;

                    if (IsGrounded(allWheels[i]))
                    {
                        travelL = (allWheels[i].CurrentSuspensionLength - m_SpringRestLength) / m_SpringRestLength;
                    }

                    if (IsGrounded(allWheels[nextAxisWheelIndex]))
                    {
                        travelR = (allWheels[nextAxisWheelIndex].CurrentSuspensionLength - m_SpringRestLength) / m_SpringRestLength;
                    }

                    antiRoll = (travelR - travelL) * antiRollForce;
                }

                float force = SpringMath.CalculateForceDamped(currentLength, currentVelocity,
                    m_SpringRestLength, m_SpringStrength,
                    m_SpringDamper) + antiRoll;

                m_Rigidbody.AddForceAtPosition(force * m_Transform.up, GetSpringPosition(wheel));
            }
        }

        private void UpdateSteering()
        {
            for (int i = 0; i < allWheels.Count; i++)
            {
                WheelData wheel = allWheels[i];

                if (!IsGrounded(wheel))
                    continue;

                Vector3 point = GetSpringPosition(wheel);
                Vector3 slideDirection = GetWheelSlideDirection(wheel);

                float slideVelocity = Vector3.Dot(slideDirection, m_Rigidbody.GetPointVelocity(point));

                float desiredVelocityChange = -slideVelocity * GetWheelGripFactor(wheel);
                var desiredAcceleration = desiredVelocityChange * Time.fixedDeltaTime;

                Vector3 force = desiredAcceleration * slideDirection;
                m_Rigidbody.AddForceAtPosition(force, point, ForceMode.VelocityChange);
            }
        }

        private void UpdateAccelerate()
        {
            if (!hasInput)
                return;

            float forwardSpeed = Vector3.Dot(m_Transform.forward, Velocity);
            bool movingForward = forwardSpeed > 0.0f;
            float speed = Mathf.Abs(forwardSpeed);

            if (movingForward && speed > MaxSpeed)
            {
                return;
            }
            else if (!movingForward && speed > MaxReverseSpeed)
            {
                return;
            }

            for (int i = 0; i < accWheels.Count; i++)
            {
                WheelData wheel = accWheels[i];

                if (!IsGrounded(wheel))
                    continue;

                Vector3 position = GetWheelTorquePosition(wheel);
                Vector3 wheelForward = GetWheelRollDirection(wheel);
                m_Rigidbody.AddForceAtPosition(Throttle * m_AcceleratePower * wheelForward, position);
            }
        }

        private void UpdateBrakes()
        {
            float forwardSpeed = Vector3.Dot(m_Transform.forward, Velocity);
            float speed = Mathf.Abs(forwardSpeed);

            float handBrakeRatio = Handbrake ? 1 : 0;
            float brakesRatio = 0;
            float additiveBrakePower = 0;

            bool braking = !hasInput;

            bool almostStopping = speed < AlmostStoppingSpeed;

            if (almostStopping)
            {
                if (braking)
                {
                    brakesRatio = AlmostStoppingBrakingRatio;
                    additiveBrakePower = m_StoppedBrakesPower * brakesRatio;
                }
            }
            else
            {
                bool accelerateContrary =
                    hasInput &&
                    Vector3.Dot(Throttle * m_Transform.forward, Velocity) < 0.0f;

                if (accelerateContrary)
                {
                    brakesRatio = MathF.Abs(Throttle);
                }
                else if (!hasInput) // No accelerate input
                {
                    brakesRatio = NoAccelerationBrakingRatio;
                }
            }

            if (handBrakeRatio == 0 && brakesRatio == 0)
                return;

            for (int i = 0; i < brakeWheels.Count; i++)
            {
                WheelData wheel = brakeWheels[i];

                if (!IsGrounded(wheel))
                    continue;

                Vector3 point = GetWheelTorquePosition(wheel);
                Vector3 rollDirection = GetWheelRollDirection(wheel);

                var rollVelocityLocal = Vector3.Dot(rollDirection, m_Rigidbody.GetPointVelocity(point));

                float desiredVelocityChange = -rollVelocityLocal * (m_BrakesPower * brakesRatio + m_HandBrakesPower * handBrakeRatio + additiveBrakePower);

                float desiredAcceleration = desiredVelocityChange * Time.fixedDeltaTime;

                Vector3 force = desiredAcceleration * rollDirection;
                m_Rigidbody.AddForceAtPosition(force, point, ForceMode.VelocityChange);
            }
        }

        private void UpdateAirResistance()
        {
            if (m_AirResistance > 0)
                m_Rigidbody.AddForce(colliderMagnitude * m_AirResistance * -Velocity);
        }

        private void UpdateWheels()
        {
            for (int i = 0; i < allWheels.Count; i++)
            {
                WheelData wheel = allWheels[i];

                wheel.WheelTransform.position = GetWheelWorldPosition(wheel);

                var rotationSpeed = Speed * Time.deltaTime * Mathf.Rad2Deg / wheelRadius;

                var rollRotation = Quaternion.AngleAxis(rotationSpeed, Vector3.right * Direction * wheel.InversionValue);

                if (!wheel.IsSteering)
                {
                    wheel.WheelTransform.localRotation *= rollRotation;
                }
                else
                {
                    wheel.RollRot *= rollRotation;
                    wheel.WheelTransform.localRotation = Quaternion.AngleAxis(Steering * m_SteerAngle, Vector3.up) * wheel.RollRot;
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var matrix = Gizmos.matrix;
            foreach (var wheel in allWheels)
            {
                Gizmos.matrix = Matrix4x4.TRS(wheel.WheelTransform.position, Quaternion.LookRotation(wheel.WheelTransform.right), Vector3.one);
                Gizmos.DrawWireSphere(default, wheelRadius);
            }

            Gizmos.matrix = matrix;

            if (Application.isPlaying)
            {
                Vector3 vehicleDown = -transform.up;

                for (int i = 0; i < allWheels.Count; i++)
                {
                    WheelData wheel = allWheels[i];
                    // Spring
                    Vector3 position = GetSpringPosition(wheel);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(position, position + vehicleDown * m_SpringRestLength);
                    Gizmos.color = Color.red;
                    Gizmos.DrawCube(GetSpringHitPosition(wheel), Vector3.one * 0.08f);

                    // WheelData
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(position, GetWheelRollDirection(wheel));
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(position, GetWheelSlideDirection(wheel));
                }
            }
            else
            {
                for (int i = 0; i < allWheels.Count; i++)
                {
                    WheelData wheel = allWheels[i];

                    var s = GetInitialSpringPoint(wheel);
                    var p1 = transform.TransformPoint(s);
                    var p2 = wheel.WheelTransform.position;

                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(p1, p2);
                }
            }
        }
#endif
    }
}
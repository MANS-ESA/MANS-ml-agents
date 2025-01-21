using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.MLAgents.Sensors
{
    /// <summary>
    /// A base class to support sensor components for Lidar-based sensors.
    /// </summary>
    public abstract class LidarPerceptionSensorComponentBase : SensorComponent
    {
        [HideInInspector, SerializeField, FormerlySerializedAs("sensorName")]
        string m_SensorName = "LidarPerceptionSensor";

        /// <summary>
        /// The name of the Sensor that this component wraps.
        /// Note that changing this at runtime does not affect how the Agent sorts the sensors.
        /// </summary>
        public string SensorName
        {
            get { return m_SensorName; }
            set { m_SensorName = value; }
        }

        [SerializeField, FormerlySerializedAs("detectableTags")]
        [Tooltip("List of tags in the scene to compare against.")]
        List<string> m_DetectableTags;

        public List<string> DetectableTags
        {
            get { return m_DetectableTags; }
            set { m_DetectableTags = value; }
        }

        [HideInInspector, SerializeField, FormerlySerializedAs("raysPerDirection")]
        [Range(0, 1000000)]
        [Tooltip("Number of rays to cast.")]
        int m_RaysPerDirection = 10;

        public int RaysPerDirection
        {
            get { return m_RaysPerDirection; }
            set { m_RaysPerDirection = value; }
        }

        [HideInInspector, SerializeField, FormerlySerializedAs("maxRayDegrees")]
        [Range(0, 1000000)]
        [Tooltip("Cone size for rays.")]
        float m_MaxRayDegrees = 70;

        public float MaxRayDegrees
        {
            get => m_MaxRayDegrees;
            set { m_MaxRayDegrees = value; UpdateSensor(); }
        }

        [HideInInspector, SerializeField, FormerlySerializedAs("sphereCastRadius")]
        [Range(0f, 10f)]
        [Tooltip("Radius of sphere to cast.")]
        float m_SphereCastRadius = 0.5f;

        public float SphereCastRadius
        {
            get => m_SphereCastRadius;
            set { m_SphereCastRadius = value; UpdateSensor(); }
        }

        [HideInInspector, SerializeField, FormerlySerializedAs("rayLength")]
        [Range(1, 1000000)]
        [Tooltip("Length of the rays to cast.")]
        float m_RayLength = 20f;

        public float RayLength
        {
            get => m_RayLength;
            set { m_RayLength = value; UpdateSensor(); }
        }

        const int k_PhysicsDefaultLayers = -5;
        [HideInInspector, SerializeField, FormerlySerializedAs("rayLayerMask")]
        [Tooltip("Controls which layers the rays can hit.")]
        LayerMask m_RayLayerMask = k_PhysicsDefaultLayers;

        public LayerMask RayLayerMask
        {
            get => m_RayLayerMask;
            set { m_RayLayerMask = value; UpdateSensor(); }
        }

        [HideInInspector, SerializeField, FormerlySerializedAs("observationStacks")]
        [Range(1, 1000000)]
        [Tooltip("Number of stacked observations.")]
        int m_ObservationStacks = 1;

        public int ObservationStacks
        {
            get { return m_ObservationStacks; }
            set { m_ObservationStacks = value; }
        }

        [HideInInspector, SerializeField]
        [Tooltip("Enable to use batched raycasts.")]
        public bool m_UseBatchedRaycasts = false;

        public bool UseBatchedRaycasts
        {
            get { return m_UseBatchedRaycasts; }
            set { m_UseBatchedRaycasts = value; }
        }

        [HideInInspector]
        [SerializeField]
        [Header("Debug Gizmos", order = 999)]
        internal Color rayHitColor = Color.red;

        [HideInInspector]
        [SerializeField]
        internal Color rayMissColor = Color.white;

        [NonSerialized]
        LidarPerceptionSensor m_LidarSensor;

        /// <summary>
        /// Get the LidarPerceptionSensor that was created.
        /// </summary>
        public LidarPerceptionSensor LidarSensor
        {
            get => m_LidarSensor;
        }

        public abstract LidarPerceptionCastType GetCastType();

        public virtual float GetStartVerticalOffset() => 0f;
        public virtual float GetEndVerticalOffset() => 0f;

        /// <summary>
        /// Returns an initialized LidarPerceptionSensor.
        /// </summary>
        public override ISensor[] CreateSensors()
        {
            var lidarInput = GetLidarPerceptionInput();
            m_LidarSensor = new LidarPerceptionSensor(m_SensorName, lidarInput);

            if (ObservationStacks != 1)
            {
                var stackingSensor = new StackingSensor(m_LidarSensor, ObservationStacks);
                return new ISensor[] { stackingSensor };
            }

            return new ISensor[] { m_LidarSensor };
        }

        
        internal static float[] GetLidarRayAngles(int raysPerDirection, float maxRayDegrees)
        {
            // Example:
            // { 90 - 3*delta, 90 - 2*delta, ..., 90, 90 + delta, ..., 90 + 3*delta }
            var anglesOut = new float[2 * raysPerDirection + 1];
            var delta = maxRayDegrees / raysPerDirection;

            for (var i = 0; i < 2 * raysPerDirection + 1; i++)
            {
                anglesOut[i] = 90 + (i - raysPerDirection) * delta;
            }

            return anglesOut;
        }

        public LidarPerceptionInput GetLidarPerceptionInput()
        {
            var lidarAngles = GetLidarRayAngles(m_RaysPerDirection, m_MaxRayDegrees);

            return new LidarPerceptionInput
            {
                RayLength = RayLength,
                Angles = lidarAngles,
                Transform = transform,
                CastType = GetCastType(),
                LayerMask = RayLayerMask,
                UseBatchedRaycasts = UseBatchedRaycasts
            };
        }

        internal void UpdateSensor()
        {
            if (m_LidarSensor != null)
            {
                var lidarInput = GetLidarPerceptionInput();
                m_LidarSensor.SetLidarPerceptionInput(lidarInput);
            }
        }

        void OnDrawGizmosSelected()
        {
            // if (m_LidarSensor?.LidarPerceptionOutput?.RayOutputs != null)
            // {
            //     var alpha = Mathf.Pow(.5f, SensorObservationAge());

            //     foreach (var rayInfo in m_LidarSensor.LidarPerceptionOutput.RayOutputs)
            //     {
            //         DrawLidarGizmos(rayInfo, alpha);
            //     }
            // }
        }

        void DrawLidarGizmos(LidarPerceptionOutput.RayOutput rayOutput, float alpha = 1.0f)
        {
            // var startPositionWorld = rayOutput.StartPositionWorld;
            // var endPositionWorld = rayOutput.EndPositionWorld;
            // var rayDirection = endPositionWorld - startPositionWorld;
            // rayDirection *= rayOutput.HitFraction;

            // var lerpT = rayOutput.HitFraction * rayOutput.HitFraction;
            // var color = Color.Lerp(rayHitColor, rayMissColor, lerpT);
            // color.a *= alpha;
            // Gizmos.color = color;
            // Gizmos.DrawRay(startPositionWorld, rayDirection);

            // if (rayOutput.HasHit)
            // {
            //     var hitRadius = Mathf.Max(rayOutput.ScaledCastRadius, .05f);
            //     Gizmos.DrawWireSphere(startPositionWorld + rayDirection, hitRadius);
            // }
        }
    }
}

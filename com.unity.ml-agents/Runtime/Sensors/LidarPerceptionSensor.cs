using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Unity.MLAgents.Sensors
{
    public enum LidarPerceptionCastType
    {
        Cast2D,
        Cast3D,
    }

    public struct LidarPerceptionInput
    {
        public float RayLength;
        public IReadOnlyList<float> Angles;
        public Transform Transform;
        public LidarPerceptionCastType CastType;
        public int LayerMask;
        public bool UseBatchedRaycasts;

        public int OutputSize() => Angles?.Count ?? 0;

        public (Vector3 StartPositionWorld, Vector3 EndPositionWorld) RayExtents(int rayIndex)
        {
            var angle = Angles[rayIndex];
            Vector3 endPositionLocal;

            if (CastType == LidarPerceptionCastType.Cast3D)
            {
                endPositionLocal = PolarToCartesian3D(RayLength, angle);
            }
            else
            {
                endPositionLocal = PolarToCartesian2D(RayLength, angle);
            }

            return (
                Transform.position,
                Transform.TransformPoint(endPositionLocal)
            );
        }

        static internal Vector3 PolarToCartesian3D(float radius, float angleDegrees)
        {
            float x = radius * Mathf.Cos(Mathf.Deg2Rad * angleDegrees);
            float z = radius * Mathf.Sin(Mathf.Deg2Rad * angleDegrees);
            return new Vector3(x, 0f, z);
        }

        static internal Vector2 PolarToCartesian2D(float radius, float angleDegrees)
        {
            float x = radius * Mathf.Cos(Mathf.Deg2Rad * angleDegrees);
            float y = radius * Mathf.Sin(Mathf.Deg2Rad * angleDegrees);
            return new Vector2(x, y);
        }
    }

    public class LidarPerceptionOutput
    {
        public struct RayOutput
        {
            public float HitFraction;

            public void ToFloatArray(int rayIndex, float[] buffer)
            {
                buffer[rayIndex] = HitFraction;
            }
        }

        public RayOutput[] RayOutputs;
    }

    public class LidarPerceptionSensor : ISensor, IBuiltInSensor
    {
        float[] m_Observations;
        ObservationSpec m_ObservationSpec;
        string m_Name;

        LidarPerceptionInput m_LidarPerceptionInput;
        LidarPerceptionOutput m_LidarPerceptionOutput;

        bool m_UseBatchedRaycasts;

        public LidarPerceptionSensor(string name, LidarPerceptionInput lidarInput)
        {
            m_Name = name;
            m_LidarPerceptionInput = lidarInput;
            m_UseBatchedRaycasts = lidarInput.UseBatchedRaycasts;

            SetNumObservations(lidarInput.OutputSize());
            m_LidarPerceptionOutput = new LidarPerceptionOutput();
        }

        void SetNumObservations(int numObservations)
        {
            m_ObservationSpec = ObservationSpec.Vector(numObservations);
            m_Observations = new float[numObservations];
        }

        public int Write(ObservationWriter writer)
        {
            Array.Clear(m_Observations, 0, m_Observations.Length);
            var numRays = m_LidarPerceptionInput.Angles.Count;

            for (var rayIndex = 0; rayIndex < numRays; rayIndex++)
            {
                m_LidarPerceptionOutput.RayOutputs[rayIndex].ToFloatArray(rayIndex, m_Observations);
            }

            writer.AddList(m_Observations);
            Debug.Log("LidarPerceptionSensor.Write : " + m_Observations);

            return m_Observations.Length;
        }

        public void Update()
        {
            var numRays = m_LidarPerceptionInput.Angles.Count;

            if (m_LidarPerceptionOutput.RayOutputs == null || m_LidarPerceptionOutput.RayOutputs.Length != numRays)
            {
                m_LidarPerceptionOutput.RayOutputs = new LidarPerceptionOutput.RayOutput[numRays];
            }

            if (m_UseBatchedRaycasts && m_LidarPerceptionInput.CastType == LidarPerceptionCastType.Cast3D)
            {
                PerceiveBatchedRays(ref m_LidarPerceptionOutput.RayOutputs, m_LidarPerceptionInput);
            }
            else
            {
                for (var rayIndex = 0; rayIndex < numRays; rayIndex++)
                {
                    m_LidarPerceptionOutput.RayOutputs[rayIndex] = PerceiveSingleRay(m_LidarPerceptionInput, rayIndex);
                }
            }
        }

        public static void PerceiveBatchedRays(ref LidarPerceptionOutput.RayOutput[] batchedRaycastOutputs, LidarPerceptionInput input)
        {
            var numRays = input.Angles.Count;
            var results = new NativeArray<RaycastHit>(numRays, Allocator.TempJob);

            var raycastCommands = new NativeArray<RaycastCommand>(numRays, Allocator.TempJob);

            for (int i = 0; i < numRays; i++)
            {
                var extents = input.RayExtents(i);
                var rayDirection = (extents.EndPositionWorld - extents.StartPositionWorld).normalized;

                raycastCommands[i] = new RaycastCommand(extents.StartPositionWorld, rayDirection, input.RayLength, input.LayerMask);

                batchedRaycastOutputs[i] = new LidarPerceptionOutput.RayOutput { HitFraction = 1.0f };
            }

            JobHandle handle = RaycastCommand.ScheduleBatch(raycastCommands, results, 1, 1, default(JobHandle));
            handle.Complete();

            for (int i = 0; i < results.Length; i++)
            {
                batchedRaycastOutputs[i].HitFraction = results[i].collider != null ? results[i].distance / input.RayLength : 1.0f;
            }

            results.Dispose();
            raycastCommands.Dispose();
        }

        internal static LidarPerceptionOutput.RayOutput PerceiveSingleRay(LidarPerceptionInput input, int rayIndex)
        {
            var extents = input.RayExtents(rayIndex);
            var rayDirection = (extents.EndPositionWorld - extents.StartPositionWorld).normalized;

            RaycastHit rayHit;
            bool castHit = Physics.Raycast(extents.StartPositionWorld, rayDirection, out rayHit, input.RayLength, input.LayerMask);
            float hitFraction = castHit ? rayHit.distance / input.RayLength : 1.0f;

            return new LidarPerceptionOutput.RayOutput { HitFraction = hitFraction };
        }

        public void Reset() { }
        public ObservationSpec GetObservationSpec() => m_ObservationSpec;
        public string GetName() => m_Name;
        public byte[] GetCompressedObservation() => null;
        public CompressionSpec GetCompressionSpec() => CompressionSpec.Default();
        public BuiltInSensorType GetBuiltInSensorType() => BuiltInSensorType.RayPerceptionSensor;
    }
}

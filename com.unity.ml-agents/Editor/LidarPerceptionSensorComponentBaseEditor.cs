using UnityEditor;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Unity.MLAgents.Editor
{
    internal class LidarPerceptionSensorComponentBaseEditor : UnityEditor.Editor
    {
        bool m_RequireSensorUpdate;

        protected void OnLidarPerceptionInspectorGUI(bool is3d)
        {
            var so = serializedObject;
            so.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUI.indentLevel++;

            // Empêcher les modifications pendant l'exécution
            EditorGUI.BeginDisabledGroup(!EditorUtilities.CanUpdateModelProperties());
            {
                EditorGUILayout.PropertyField(so.FindProperty("m_SensorName"), true);
                EditorGUILayout.PropertyField(so.FindProperty("m_DetectableTags"), true);
                EditorGUILayout.PropertyField(so.FindProperty("m_RaysPerDirection"), true);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(so.FindProperty("m_MaxRayDegrees"), true);
            EditorGUILayout.PropertyField(so.FindProperty("m_SphereCastRadius"), true);
            EditorGUILayout.PropertyField(so.FindProperty("m_RayLength"), true);
            EditorGUILayout.PropertyField(so.FindProperty("m_RayLayerMask"), true);

            EditorGUI.BeginDisabledGroup(!EditorUtilities.CanUpdateModelProperties());
            {
                EditorGUILayout.PropertyField(so.FindProperty("m_ObservationStacks"), new GUIContent("Stacked Raycasts"), true);
            }
            EditorGUI.EndDisabledGroup();

            if (is3d)
            {
                EditorGUILayout.PropertyField(so.FindProperty("m_StartVerticalOffset"), true);
                EditorGUILayout.PropertyField(so.FindProperty("m_EndVerticalOffset"), true);
            }

            EditorGUILayout.PropertyField(so.FindProperty("m_AlternatingRayOrder"), true);
            if (is3d)
            {
                EditorGUILayout.PropertyField(so.FindProperty("m_UseBatchedRaycasts"), true);
            }

            EditorGUILayout.PropertyField(so.FindProperty("rayHitColor"), true);
            EditorGUILayout.PropertyField(so.FindProperty("rayMissColor"), true);

            EditorGUI.indentLevel--;
            if (EditorGUI.EndChangeCheck())
            {
                m_RequireSensorUpdate = true;
            }

            so.ApplyModifiedProperties();
            UpdateSensorIfDirty();
        }

        void UpdateSensorIfDirty()
        {
            if (m_RequireSensorUpdate)
            {
                var sensorComponent = serializedObject.targetObject as LidarPerceptionSensorComponentBase;
                sensorComponent?.UpdateSensor();
                m_RequireSensorUpdate = false;
            }
        }
    }

    [CustomEditor(typeof(LidarPerceptionSensorComponent3D), editorForChildClasses: true)]
    [CanEditMultipleObjects]
    internal class LidarPerceptionSensorComponent3DEditor : LidarPerceptionSensorComponentBaseEditor
    {
        public override void OnInspectorGUI()
        {
            OnLidarPerceptionInspectorGUI(true);
        }
    }
}

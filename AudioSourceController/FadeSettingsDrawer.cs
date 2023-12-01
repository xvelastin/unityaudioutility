using UnityEngine;
using UnityEditor;
using static XV.AudioSourceController.FadeSettings;

namespace XV
{
    [CustomPropertyDrawer(typeof(AudioSourceController.FadeSettings))]
    public class FadeSettingsDrawer : PropertyDrawer
    {
        private readonly float CurveFieldHeight = EditorGUIUtility.singleLineHeight * 4;
        private Color curveColour = Color.cyan;
        private const float maxDuration = 45;
        
        private bool _isOpen = true;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var durationProperty = property.FindPropertyRelative(nameof(AudioSourceController.FadeSettings.Duration));
            var curveProperty = property.FindPropertyRelative(nameof(AudioSourceController.FadeSettings.Curve));
            var curveShapeProperty = property.FindPropertyRelative(nameof(AudioSourceController.FadeSettings.CurveShape));
            var direction = property.FindPropertyRelative(nameof(AudioSourceController.FadeSettings.Direction));
            var directionAsEnum = (ECurveDirection)direction.enumValueIndex;

            position.height = EditorGUIUtility.singleLineHeight;

            _isOpen = EditorGUI.BeginFoldoutHeaderGroup(position, _isOpen, property.displayName);
            if (_isOpen)
            {
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                durationProperty.floatValue = EditorGUI.Slider(position, durationProperty.displayName, durationProperty.floatValue, 0, maxDuration);
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.BeginChangeCheck();

                curveShapeProperty.floatValue = EditorGUI.Slider(position, new GUIContent(curveShapeProperty.displayName, curveShapeProperty.tooltip), curveShapeProperty.floatValue, 0, 1);
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                position.height = CurveFieldHeight;

                if (EditorGUI.EndChangeCheck())
                {
                    curveProperty.animationCurveValue = Draw(curveShapeProperty.floatValue, directionAsEnum);
                }
                else
                {
                    if (curveProperty.animationCurveValue.keys.Length < 2)
                    {
                        curveProperty.animationCurveValue = Draw(0.5f, directionAsEnum);
                    }
                }

                position.x += EditorGUIUtility.labelWidth;
                position.width -= EditorGUIUtility.labelWidth;

                curveProperty.animationCurveValue = EditorGUI.CurveField(position, curveProperty.animationCurveValue, curveColour, new Rect());

                position.y += CurveFieldHeight + EditorGUIUtility.standardVerticalSpacing;
                position.height = EditorGUIUtility.singleLineHeight;
            }
            EditorGUI.EndFoldoutHeaderGroup();

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var baseHeight = (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 3;
            return _isOpen ? baseHeight + CurveFieldHeight : EditorGUIUtility.singleLineHeight;
        }

        private static AnimationCurve Draw(float curveShape, ECurveDirection directionType)
        {
            var keys = new Keyframe[2];
            const float curveBendFactor = 3.0f;
            
            if (directionType == ECurveDirection.In)
            {
                curveShape = 1 - curveShape;
                if (curveShape < 0.5f)
                {
                    keys[0] = new Keyframe(0, 0, 0, Mathf.Cos(curveShape * Mathf.PI) * curveBendFactor);
                    keys[1] = new Keyframe(1, 1);
                }
                else
                {
                    keys[0] = new Keyframe(0, 0);
                    keys[1] = new Keyframe(1, 1, -Mathf.Cos(curveShape * Mathf.PI) * curveBendFactor, 0);
                }
            }
            else
            {
                if (curveShape < 0.5f)
                {
                    keys[0] = new Keyframe(0, 1, 0, -Mathf.Cos(curveShape * Mathf.PI) * curveBendFactor);
                    keys[1] = new Keyframe(1, 0);
                }
                else
                {
                    keys[0] = new Keyframe(0, 1);
                    keys[1] = new Keyframe(1, 0, Mathf.Cos(curveShape * Mathf.PI) * curveBendFactor, 0);
                }
            }
            return new AnimationCurve(keys);
        }
    }
}
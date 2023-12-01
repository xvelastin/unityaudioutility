using UnityEditor;
using UnityEngine;

namespace XV
{
    [CustomPropertyDrawer(typeof(AudioSourceController.Parameter))]
    public class ParameterDrawer : PropertyDrawer
    {
        private float _propertyHeightInLines = 1;
        private bool _isOpen = true;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            #region Properties

            var valueProperty = property.FindPropertyRelative("_value");
            var randomRangeProperty =
                property.FindPropertyRelative(nameof(AudioSourceController.Parameter.RandomRange));
            var minValueProperty = property.FindPropertyRelative(nameof(AudioSourceController.Parameter.MinValue));
            var maxValueProperty = property.FindPropertyRelative(nameof(AudioSourceController.Parameter.MaxValue));

            #endregion

            position.height = EditorGUIUtility.singleLineHeight;

            _isOpen = EditorGUI.BeginFoldoutHeaderGroup(position, _isOpen, new GUIContent(property.displayName, property.tooltip));
            
            if (_isOpen)
            {
                _propertyHeightInLines = 3;
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                if (minValueProperty.floatValue > maxValueProperty.floatValue)
                {
                    valueProperty.floatValue = EditorGUI.FloatField(position, property.displayName, Mathf.Max(minValueProperty.floatValue, valueProperty.floatValue));
                }
                else
                {
                    valueProperty.floatValue = EditorGUI.Slider(position, property.displayName, valueProperty.floatValue, minValueProperty.floatValue, maxValueProperty.floatValue);
                }

                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                randomRangeProperty.floatValue = EditorGUI.FloatField(position, randomRangeProperty.displayName + " (+/-)", Mathf.Max(0, randomRangeProperty.floatValue));

            }
            else
            {
                _propertyHeightInLines = 1;
            }
            EditorGUI.EndFoldoutHeaderGroup();


            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return _propertyHeightInLines * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
        }
    }
}
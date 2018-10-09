using UnityEngine;
using UnityEngine.VFXToolbox;

namespace UnityEditor.VFXToolbox
{
    [CustomPropertyDrawer(typeof(FloatSliderAttribute))]
    internal class FloatSliderPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            FloatSliderAttribute floatSliderAttribute = attribute as FloatSliderAttribute;

            if(property.propertyType == SerializedPropertyType.Float)
            {
                EditorGUI.Slider(position, property, floatSliderAttribute.m_ValueMin, floatSliderAttribute.m_ValueMax);
            }
            else
                EditorGUI.LabelField(position, label, "(FloatSliderProperty can only be used with float attributes)");
        }
    }
}

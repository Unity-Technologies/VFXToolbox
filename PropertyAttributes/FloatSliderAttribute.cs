using UnityEngine;

namespace UnityEngine.VFXToolbox
{
    public class FloatSliderAttribute : PropertyAttribute
    {
        public float m_ValueMin;
        public float m_ValueMax;

        public FloatSliderAttribute(float min, float max) 
        {
            // Check inversions : value
            if(min < max)
            {
                m_ValueMin = min;
                m_ValueMax = max;
            }
            else
            {
                m_ValueMin = max;
                m_ValueMax = min;
            }
        }
    }
}


using UnityEngine;
using System;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ProcessorAttribute : Attribute
    {
        public readonly string category;
        public readonly string name;

        public ProcessorAttribute(string category, string name)
        {
            this.category = category;
            this.name = name;
        }
    }
}

using UnityEngine;
using System;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ProcessorAttribute : Attribute
    {
        public readonly string category;
        public readonly string name;
        public readonly Type processorType;

        public ProcessorAttribute(string category, string name, Type processorType)
        {
            this.category = category;
            this.name = name;
            this.processorType = processorType;
        }
    }
}

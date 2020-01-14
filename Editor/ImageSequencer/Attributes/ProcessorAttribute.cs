using UnityEngine;
using System;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    /// <summary>
    /// Attribute for Class derived from ProcessorBase. 
    /// Determines the ImageSequencer menu name and category for the processor when adding new processors to the asset.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ProcessorAttribute : Attribute
    {
        /// <summary>
        /// Menu Category where the Processor will be stored into
        /// </summary>
        public readonly string category;

        /// <summary>
        /// Processor name used to display in the menu
        /// </summary>
        public readonly string name;

        /// <summary>
        /// Defines a Processor Entry in the ImageSequencer add Processor Menu.
        /// </summary>
        /// <param name="category">Menu Category where the Processor will be stored into</param>
        /// <param name="name">Processor name used to display in the menu</param>
        public ProcessorAttribute(string category, string name)
        {
            this.category = category;
            this.name = name;
        }
    }
}

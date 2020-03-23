using UnityEngine;
using System;

namespace UnityEditor.Experimental.VFX.Toolbox.ImageSequencer
{
    internal class ProcessorInfo : ScriptableObject
    {
        public bool Enabled;
        public ProcessorBase Settings;

        public static ProcessorInfo CreateDefault(string name, bool enabled, Type type)
        {
            ProcessorInfo p = ScriptableObject.CreateInstance<ProcessorInfo>();
            p.Enabled = enabled;
            p.Settings = ScriptableObject.CreateInstance(type) as ProcessorBase;
            p.Settings.Default();
            return p;
        }

        public override string ToString()
        {
            return Settings.label + (Enabled ? "" : "Disabled") ;
        }

    }
}


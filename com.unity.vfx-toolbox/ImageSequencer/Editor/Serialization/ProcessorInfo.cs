using UnityEngine;
using System;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    public class ProcessorInfo : ScriptableObject
    {
        public string ProcessorName;
        public bool Enabled;
        public ProcessorSettingsBase Settings;

        public static ProcessorInfo CreateDefault(string name, bool enabled, Type type)
        {
            ProcessorInfo p = ScriptableObject.CreateInstance<ProcessorInfo>();
            p.ProcessorName = name;
            p.Enabled = enabled;
            p.Settings = ScriptableObject.CreateInstance(type) as ProcessorSettingsBase;
            p.Settings.Default();
            return p;
        }

        public override string ToString()
        {
            return ProcessorName + (Enabled ? "" : "Disabled") ;
        }

    }
}


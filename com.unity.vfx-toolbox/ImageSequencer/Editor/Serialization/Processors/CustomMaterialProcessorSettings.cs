using UnityEngine;

namespace UnityEditor.VFXToolbox.ImageSequencer
{
    [Processor("","Custom Material", typeof(CustomMaterialProcessor))]
    public class CustomMaterialProcessorSettings : ProcessorSettingsBase
    {
        public Material material;

        public override void Default()
        {
            material = null;
        }
    }
}

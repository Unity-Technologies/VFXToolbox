using UnityEngine;
using System.IO;

namespace UnityEditor.VFXToolbox.VolumeEditor
{
    public class VFFileImporter
    {
        public static Texture3D LoadVFFile(string filename, TextureFormat textureformat)
        {
            BinaryReader br = new BinaryReader(File.OpenRead(filename));
            string fourcc = new string(br.ReadChars(4));
            ushort size_x = br.ReadUInt16();
            ushort size_y = br.ReadUInt16();
            ushort size_z = br.ReadUInt16();

            int mode = -1;
            if (fourcc == "VF_F")
                mode = 0;
            else if (fourcc == "VF_V")
                mode = 1;
            else
                throw new System.Exception("Invalid VF FourCC");

            if(mode != -1)
            {
                Texture3D outputFile = new Texture3D(size_x, size_y, size_z, textureformat, false);
                ulong length = (ulong)size_x * (ulong)size_y * (ulong)size_z;
                Color[] colors = new Color[length];

                for(ulong i = 0; i < length; i++)
                {
                    if(mode == 0)
                    {
                        float val = br.ReadSingle();
                        colors[i] = new Color(val, val, val);
                    }
                    else
                    {
                        float r = br.ReadSingle();
                        float g = br.ReadSingle();
                        float b = br.ReadSingle();
                        colors[i] = new Color(r,g,b);
                    }
                }
                outputFile.SetPixels(colors);
                br.Close();
                return outputFile;
            }

            return null;

        }
    }
}

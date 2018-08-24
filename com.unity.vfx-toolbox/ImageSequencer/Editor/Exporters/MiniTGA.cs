using System;
using UnityEngine;
using UnityEditor.VFXToolbox;

namespace MiniTGA
{
    public static class MiniTGA
    {
        // Writes TGA into a memory buffer.
        // Input:
        //   - (width) x (height) image,
        //   - channels=4: RGBA 32bit
        //   - channels=3: RGB 24bit
        // Returns memory buffer with uncompressed, unpalettized Targa contents and buffer size in outSize. free() the buffer when done with it.
 
        public static void MiniTGAWrite (string _filePath, ushort _width, ushort _height, bool _exportalpha, Color[] _colorArray) {

            byte[] bytes = MiniTGAWrite(_width, _height, _exportalpha, _colorArray);
            System.IO.File.WriteAllBytes(_filePath, bytes);
        }

        public static byte[] MiniTGAWrite (ushort _width, ushort _height, bool _exportalpha, Color[] _colorArray)
        {
            byte[] kHeader =                                    // TRUEVISION TARGA HEADER          18 bytes
            {
                0,                                              // No ID Field                      1
                0,                                              // No ColorMap                      1
                2,                                              // Uncompressed                     1
                0,0,0,0,0,                                      // Null Dummies for Color Map       5
                0,0,                                            // X Origin = 0                     2
                0,0,                                            // Y Origin = 0                     2
                (byte)(_width % 256), (byte)(_width >> 8),      // Width                            2
                (byte)(_height % 256), (byte)(_height >> 8),    // Height                           2
                (byte)(_exportalpha ? 32 : 24),                 // Bit depth                        1
                0                                               // End Descriptor                   1
            };

            byte stride = (byte)(_exportalpha ? 4 : 3);
            int size = kHeader.Length + (_width * _height * stride);

            byte[] buffer = new byte[size];

            // Copy Header into buffer
            Buffer.BlockCopy(kHeader, 0, buffer, 0, kHeader.Length);

            // Image Positionning
            int pos = kHeader.Length;

            int count = _colorArray.Length;
            int i = 0;
            foreach(Color c in _colorArray)
            {
                buffer[pos] = (byte)(Mathf.Clamp01(c.b) * 255);
                buffer[pos+1] = (byte)(Mathf.Clamp01(c.g) * 255);
                buffer[pos+2] = (byte)(Mathf.Clamp01(c.r) * 255);
                if(_exportalpha)
                    buffer[pos+3] = (byte)(Mathf.Clamp01(c.a) * 255);
                pos += stride;
                i++;
            }

            return buffer;
        }
    }
}

using UnityEngine;
using UnityEditor.VFXToolbox;

// MiniEXR 2013 by Aras Pranckevicius / Unity Technologies.
//
// C# conversion by Ilya Suzdalnitski.
// Slightly Modified by Thomas Ich�
//
// Writes OpenEXR RGB files out of half-precision RGBA or RGB data.
//
 
namespace MiniEXR {
      //Based on source-forge project: http://sourceforge.net/projects/csharp-half/
    internal static class HalfHelper
    {
        private static uint[] mantissaTable = GenerateMantissaTable();
        private static uint[] exponentTable = GenerateExponentTable();
        private static ushort[] offsetTable = GenerateOffsetTable();
        private static ushort[] baseTable = GenerateBaseTable();
        private static sbyte[] shiftTable = GenerateShiftTable();
 
        // Transforms the subnormal representation to a normalized one. 
        private static uint ConvertMantissa(int i)
        {
            uint m = (uint)(i << 13); // Zero pad mantissa bits
            uint e = 0; // Zero exponent
 
            // While not normalized
            while ((m & 0x00800000) == 0)
            {
                e -= 0x00800000; // Decrement exponent (1<<23)
                m <<= 1; // Shift mantissa                
            }
            m &= unchecked((uint)~0x00800000); // Clear leading 1 bit
            e += 0x38800000; // Adjust bias ((127-14)<<23)
            return m | e; // Return combined number
        }
 
        private static uint[] GenerateMantissaTable()
        {
            uint[] mantissaTable = new uint[2048];
            mantissaTable[0] = 0;
            for (int i = 1; i < 1024; i++)
            {
                mantissaTable[i] = ConvertMantissa(i);
            }
            for (int i = 1024; i < 2048; i++)
            {
                mantissaTable[i] = (uint)(0x38000000 + ((i - 1024) << 13));
            }
 
            return mantissaTable;
        }
        private static uint[] GenerateExponentTable()
        {
            uint[] exponentTable = new uint[64];
            exponentTable[0] = 0;
            for (int i = 1; i < 31; i++)
            {
                exponentTable[i] = (uint)(i << 23);
            }
            exponentTable[31] = 0x47800000;
            exponentTable[32] = 0x80000000;
            for (int i = 33; i < 63; i++)
            {
                exponentTable[i] = (uint)(0x80000000 + ((i - 32) << 23));
            }
            exponentTable[63] = 0xc7800000;
 
            return exponentTable;
        }
        private static ushort[] GenerateOffsetTable()
        {
            ushort[] offsetTable = new ushort[64];
            offsetTable[0] = 0;
            for (int i = 1; i < 32; i++)
            {
                offsetTable[i] = 1024;
            }
            offsetTable[32] = 0;
            for (int i = 33; i < 64; i++)
            {
                offsetTable[i] = 1024;
            }
 
            return offsetTable;
        }
        private static ushort[] GenerateBaseTable()
        {
            ushort[] baseTable = new ushort[512];
            for (int i = 0; i < 256; ++i)
            {
                sbyte e = (sbyte)(127 - i);
                if (e > 24)
                { // Very small numbers map to zero
                    baseTable[i | 0x000] = 0x0000;
                    baseTable[i | 0x100] = 0x8000;
                }
                else if (e > 14)
                { // Small numbers map to denorms
                    baseTable[i | 0x000] = (ushort)(0x0400 >> (18 + e));
                    baseTable[i | 0x100] = (ushort)((0x0400 >> (18 + e)) | 0x8000);
                }
                else if (e >= -15)
                { // Normal numbers just lose precision
                    baseTable[i | 0x000] = (ushort)((15 - e) << 10);
                    baseTable[i | 0x100] = (ushort)(((15 - e) << 10) | 0x8000);
                }
                else if (e > -128)
                { // Large numbers map to Infinity
                    baseTable[i | 0x000] = 0x7c00;
                    baseTable[i | 0x100] = 0xfc00;
                }
                else
                { // Infinity and NaN's stay Infinity and NaN's
                    baseTable[i | 0x000] = 0x7c00;
                    baseTable[i | 0x100] = 0xfc00;
                }
            }
 
            return baseTable;
        }
        private static sbyte[] GenerateShiftTable()
        {
            sbyte[] shiftTable = new sbyte[512];
            for (int i = 0; i < 256; ++i)
            {
                sbyte e = (sbyte)(127 - i);
                if (e > 24)
                { // Very small numbers map to zero
                    shiftTable[i | 0x000] = 24;
                    shiftTable[i | 0x100] = 24;
                }
                else if (e > 14)
                { // Small numbers map to denorms
                    shiftTable[i | 0x000] = (sbyte)(e - 1);
                    shiftTable[i | 0x100] = (sbyte)(e - 1);
                }
                else if (e >= -15)
                { // Normal numbers just lose precision
                    shiftTable[i | 0x000] = 13;
                    shiftTable[i | 0x100] = 13;
                }
                else if (e > -128)
                { // Large numbers map to Infinity
                    shiftTable[i | 0x000] = 24;
                    shiftTable[i | 0x100] = 24;
                }
                else
                { // Infinity and NaN's stay Infinity and NaN's
                    shiftTable[i | 0x000] = 13;
                    shiftTable[i | 0x100] = 13;
                }
            }
 
            return shiftTable;
        }
 
        public static float HalfToSingle(ushort half)
        {
            uint result = mantissaTable[offsetTable[half >> 10] + (half & 0x3ff)] + exponentTable[half >> 10];
 
            return System.BitConverter.ToSingle( System.BitConverter.GetBytes( result ), 0 );
 
            //return *((float*)&result);
        }
        public static ushort SingleToHalf(float single)
        {
            //uint value = *((uint*)&single);
 
            uint value = System.BitConverter.ToUInt32( System.BitConverter.GetBytes( single ), 0 );
 
            ushort result = (ushort)(baseTable[(value >> 23) & 0x1ff] + ((value & 0x007fffff) >> shiftTable[value >> 23]));
            return result;
        }
    }
 
    public static class MiniEXR {
 
        // Writes EXR into a memory buffer.
        // Input:
        //   - (width) x (height) image,
        //   - channels=4: 8 bytes per pixel (R,G,B,A order, 16 bit float per channel; alpha ignored), or
        //   - channels=3: 6 bytes per pixel (R,G,B order, 16 bit float per channel).
        // Returns memory buffer with .EXR contents and buffer size in outSize. free() the buffer when done with it.

        public static void MiniEXRWrite (string _filePath, uint _width, uint _height, bool _ExportAlpha, Color[] _colorArray, bool bFlipVertical) {

            byte[] bytes = MiniEXRWrite(_width, _height, _ExportAlpha, _colorArray, bFlipVertical);

            System.IO.File.WriteAllBytes(_filePath, bytes );
        }
 
        public static byte[] MiniEXRWrite (uint _width, uint _height, bool _ExportAlpha, Color[] _colorArray, bool bFlipVertical = false)
        {
            if (bFlipVertical)
                _colorArray = FlipVertical(_colorArray, _width, _height);

            byte stride = (byte)(_ExportAlpha ? 4 : 3);
            float[] rgbaArray = new float[ _colorArray.Length * stride ];
 

            for (int i = 0; i < _colorArray.Length; i++)
            {
                rgbaArray[i * stride] = _colorArray[i].r;
                rgbaArray[i * stride + 1] = _colorArray[i].g;
                rgbaArray[i * stride + 2] = _colorArray[i].b;
                if(_ExportAlpha)
                    rgbaArray[i * stride + 3] = _colorArray[i].a;
            }

            return MiniEXRWrite(_width, _height, stride, rgbaArray);
        }

        private static Color[] FlipVertical(Color[] input, uint _width, uint _height)
        {
            Color[] output = new Color[input.Length];

            uint k = 0;
            for(int j = (int)_height-1; j >= 0; j--)
            {
                for (int i = 0; i < _width; i++)
                {
                    int idx = i + (j * (int)_width);
                    output[k] = input[idx];
                    k++;
                }
            }

            return output;
        }
 
        public static byte[] MiniEXRWrite (uint _width, uint _height, uint _channels, float[] _rgbaArray)
        {
            //const void* rgba16f
            uint ww = _width-1;
            uint hh = _height-1;
            byte[] kHeader;
            if(_channels == 3)
            {
                    kHeader = new byte[] {
                    0x76, 0x2f, 0x31, 0x01, // magic
                    2, 0, 0, 0, // version, scanline
                    // channels
                    (byte)'c',(byte)'h',(byte)'a',(byte)'n',(byte)'n',(byte)'e',(byte)'l',(byte)'s',0,
                    (byte)'c',(byte)'h',(byte)'l',(byte)'i',(byte)'s',(byte)'t',0,
                    55,0,0,0,
                    (byte)'B',0, 1,0,0,0, 0, 0,0,0,1,0,0,0,1,0,0,0, // R, half
                    (byte)'G',0, 1,0,0,0, 0, 0,0,0,1,0,0,0,1,0,0,0, // G, half
                    (byte)'R',0, 1,0,0,0, 0, 0,0,0,1,0,0,0,1,0,0,0, // B, half
                    0,
                    // compression
                    (byte)'c',(byte)'o',(byte)'m',(byte)'p',(byte)'r',(byte)'e',(byte)'s',(byte)'s',(byte)'i',(byte)'o',(byte)'n',0,
                    (byte)'c',(byte)'o',(byte)'m',(byte)'p',(byte)'r',(byte)'e',(byte)'s',(byte)'s',(byte)'i',(byte)'o',(byte)'n',0,
                    1,0,0,0,
                    0, // no compression
                    // dataWindow
                    (byte)'d',(byte)'a',(byte)'t',(byte)'a',(byte)'W',(byte)'i',(byte)'n',(byte)'d',(byte)'o',(byte)'w',0,
                    (byte)'b',(byte)'o',(byte)'x',(byte)'2',(byte)'i',0,
                    16,0,0,0,
                    0,0,0,0,0,0,0,0,
                    (byte)(ww&0xFF), (byte)((ww>>8)&0xFF), (byte)((ww>>16)&0xFF), (byte)((ww>>24)&0xFF),
                    (byte)(hh&0xFF), (byte)((hh>>8)&0xFF), (byte)((hh>>16)&0xFF), (byte)((hh>>24)&0xFF),
                    // displayWindow
                    (byte)'d',(byte)'i',(byte)'s',(byte)'p',(byte)'l',(byte)'a',(byte)'y',(byte)'W',(byte)'i',(byte)'n',(byte)'d',(byte)'o',(byte)'w',0,
                    (byte)'b',(byte)'o',(byte)'x',(byte)'2',(byte)'i',0,
                    16,0,0,0,
                    0,0,0,0,0,0,0,0,
                    (byte)(ww&0xFF), (byte)((ww>>8)&0xFF), (byte)((ww>>16)&0xFF), (byte)((ww>>24)&0xFF),
                    (byte)(hh&0xFF), (byte)((hh>>8)&0xFF), (byte)((hh>>16)&0xFF), (byte)((hh>>24)&0xFF),
                    // lineOrder
                    (byte)'l',(byte)'i',(byte)'n',(byte)'e',(byte)'O',(byte)'r',(byte)'d',(byte)'e',(byte)'r',0,
                    (byte)'l',(byte)'i',(byte)'n',(byte)'e',(byte)'O',(byte)'r',(byte)'d',(byte)'e',(byte)'r',0,
                    1,0,0,0,
                    0, // increasing Y
                    // pixelAspectRatio
                    (byte)'p',(byte)'i',(byte)'x',(byte)'e',(byte)'l',(byte)'A',(byte)'s',(byte)'p',(byte)'e',(byte)'c',(byte)'t',(byte)'R',(byte)'a',(byte)'t',(byte)'i',(byte)'o',0,
                    (byte)'f',(byte)'l',(byte)'o',(byte)'a',(byte)'t',0,
                    4,0,0,0,
                    0,0,0x80,0x3f, // 1.0f
                    // screenWindowCenter
                    (byte)'s',(byte)'c',(byte)'r',(byte)'e',(byte)'e',(byte)'n',(byte)'W',(byte)'i',(byte)'n',(byte)'d',(byte)'o',(byte)'w',(byte)'C',(byte)'e',(byte)'n',(byte)'t',(byte)'e',(byte)'r',0,
                    (byte)'v',(byte)'2',(byte)'f',0,
                    8,0,0,0,
                    0,0,0,0, 0,0,0,0,
                    // screenWindowWidth
                    (byte)'s',(byte)'c',(byte)'r',(byte)'e',(byte)'e',(byte)'n',(byte)'W',(byte)'i',(byte)'n',(byte)'d',(byte)'o',(byte)'w',(byte)'W',(byte)'i',(byte)'d',(byte)'t',(byte)'h',0,
                    (byte)'f',(byte)'l',(byte)'o',(byte)'a',(byte)'t',0,
                    4,0,0,0,
                    0,0,0x80,0x3f, // 1.0f
                    // end of header
                    0,
                    };
            }
            else
            {
                    kHeader = new byte[] {
                    0x76, 0x2f, 0x31, 0x01, // magic
                    2, 0, 0, 0, // version, scanline
                    // channels
                    (byte)'c',(byte)'h',(byte)'a',(byte)'n',(byte)'n',(byte)'e',(byte)'l',(byte)'s',0,
                    (byte)'c',(byte)'h',(byte)'l',(byte)'i',(byte)'s',(byte)'t',0,
                    55,0,0,0,
                    (byte)'A',0, 1,0,0,0, 0, 0,0,0,1,0,0,0,1,0,0,0, // A, half
                    (byte)'B',0, 1,0,0,0, 0, 0,0,0,1,0,0,0,1,0,0,0, // R, half
                    (byte)'G',0, 1,0,0,0, 0, 0,0,0,1,0,0,0,1,0,0,0, // G, half
                    (byte)'R',0, 1,0,0,0, 0, 0,0,0,1,0,0,0,1,0,0,0, // B, half
                    0,
                    // compression
                    (byte)'c',(byte)'o',(byte)'m',(byte)'p',(byte)'r',(byte)'e',(byte)'s',(byte)'s',(byte)'i',(byte)'o',(byte)'n',0,
                    (byte)'c',(byte)'o',(byte)'m',(byte)'p',(byte)'r',(byte)'e',(byte)'s',(byte)'s',(byte)'i',(byte)'o',(byte)'n',0,
                    1,0,0,0,
                    0, // no compression
                    // dataWindow
                    (byte)'d',(byte)'a',(byte)'t',(byte)'a',(byte)'W',(byte)'i',(byte)'n',(byte)'d',(byte)'o',(byte)'w',0,
                    (byte)'b',(byte)'o',(byte)'x',(byte)'2',(byte)'i',0,
                    16,0,0,0,
                    0,0,0,0,0,0,0,0,
                    (byte)(ww&0xFF), (byte)((ww>>8)&0xFF), (byte)((ww>>16)&0xFF), (byte)((ww>>24)&0xFF),
                    (byte)(hh&0xFF), (byte)((hh>>8)&0xFF), (byte)((hh>>16)&0xFF), (byte)((hh>>24)&0xFF),
                    // displayWindow
                    (byte)'d',(byte)'i',(byte)'s',(byte)'p',(byte)'l',(byte)'a',(byte)'y',(byte)'W',(byte)'i',(byte)'n',(byte)'d',(byte)'o',(byte)'w',0,
                    (byte)'b',(byte)'o',(byte)'x',(byte)'2',(byte)'i',0,
                    16,0,0,0,
                    0,0,0,0,0,0,0,0,
                    (byte)(ww&0xFF), (byte)((ww>>8)&0xFF), (byte)((ww>>16)&0xFF), (byte)((ww>>24)&0xFF),
                    (byte)(hh&0xFF), (byte)((hh>>8)&0xFF), (byte)((hh>>16)&0xFF), (byte)((hh>>24)&0xFF),
                    // lineOrder
                    (byte)'l',(byte)'i',(byte)'n',(byte)'e',(byte)'O',(byte)'r',(byte)'d',(byte)'e',(byte)'r',0,
                    (byte)'l',(byte)'i',(byte)'n',(byte)'e',(byte)'O',(byte)'r',(byte)'d',(byte)'e',(byte)'r',0,
                    1,0,0,0,
                    0, // increasing Y
                    // pixelAspectRatio
                    (byte)'p',(byte)'i',(byte)'x',(byte)'e',(byte)'l',(byte)'A',(byte)'s',(byte)'p',(byte)'e',(byte)'c',(byte)'t',(byte)'R',(byte)'a',(byte)'t',(byte)'i',(byte)'o',0,
                    (byte)'f',(byte)'l',(byte)'o',(byte)'a',(byte)'t',0,
                    4,0,0,0,
                    0,0,0x80,0x3f, // 1.0f
                    // screenWindowCenter
                    (byte)'s',(byte)'c',(byte)'r',(byte)'e',(byte)'e',(byte)'n',(byte)'W',(byte)'i',(byte)'n',(byte)'d',(byte)'o',(byte)'w',(byte)'C',(byte)'e',(byte)'n',(byte)'t',(byte)'e',(byte)'r',0,
                    (byte)'v',(byte)'2',(byte)'f',0,
                    8,0,0,0,
                    0,0,0,0, 0,0,0,0,
                    // screenWindowWidth
                    (byte)'s',(byte)'c',(byte)'r',(byte)'e',(byte)'e',(byte)'n',(byte)'W',(byte)'i',(byte)'n',(byte)'d',(byte)'o',(byte)'w',(byte)'W',(byte)'i',(byte)'d',(byte)'t',(byte)'h',0,
                    (byte)'f',(byte)'l',(byte)'o',(byte)'a',(byte)'t',0,
                    4,0,0,0,
                    0,0,0x80,0x3f, // 1.0f
                    // end of header
                    0,
                    };
            }

 
            uint kHeaderSize = (uint)kHeader.Length;
 
            uint kScanlineTableSize = 8 * _height;
            uint pixelRowSize = _width * _channels * 2;
            uint fullRowSize = pixelRowSize + 8;
 
            uint bufSize = kHeaderSize + kScanlineTableSize + _height * fullRowSize;
 
            byte[] buf = new byte[bufSize];
 
            // copy in header
 
            int bufI = 0;
 
            for (int i = 0; i < kHeaderSize; i++) {
                buf[ bufI ] = kHeader[i];
 
                bufI++;
            }
 
            // line offset table
            uint ofs = kHeaderSize + kScanlineTableSize;

            for (int y = 0; y < _height; ++y)
            {
                buf[ bufI++ ] = (byte)(ofs & 0xFF);
                buf[ bufI++ ] = (byte)((ofs >> 8) & 0xFF);
                buf[ bufI++ ] = (byte)((ofs >> 16) & 0xFF);
                buf[ bufI++ ] = (byte)((ofs >> 24) & 0xFF);
                buf[ bufI++ ] = 0;
                buf[ bufI++ ] = 0;
                buf[ bufI++ ] = 0;
                buf[ bufI++ ] = 0;
 
                ofs += fullRowSize;
            }
 
            //Convert float to half float
            ushort[] srcHalf = new ushort[_rgbaArray.Length];
 
            for (int i = 0; i < _rgbaArray.Length; i++) {
                //Gamma encode before converting : no
                //_rgbaArray[i] = Mathf.Pow(_rgbaArray[i], 2.2f);
                srcHalf[i] = HalfHelper.SingleToHalf( _rgbaArray[i] );
            }
 
            uint srcDataI = 0;

            for (int y = 0; y < _height; y++)
            {
                // coordinate
                buf[ bufI++ ] = (byte)(y & 0xFF);
                buf[ bufI++ ] = (byte)((y >> 8) & 0xFF);
                buf[ bufI++ ] = (byte)((y >> 16) & 0xFF);
                buf[ bufI++ ] = (byte)((y >> 24) & 0xFF);
                // data size
                buf[ bufI++ ] = (byte)(pixelRowSize & 0xFF);
                buf[ bufI++ ] = (byte)((pixelRowSize >> 8) & 0xFF);
                buf[ bufI++ ] = (byte)((pixelRowSize >> 16) & 0xFF);
                buf[ bufI++ ] = (byte)((pixelRowSize >> 24) & 0xFF);
                // B, G, R
                //memcpy (ptr, src, width*6);	//Copy first line - 6 bits, 2 bits per channel

                uint tempSrcI;

                //If _channels == 4, write Alpha
                if(_channels == 4)
                {
                    tempSrcI = srcDataI;
                    for (int x = 0; x < _width; ++x)
                    {	
                        //Blue
                        byte[] halfBytes = System.BitConverter.GetBytes( srcHalf[ tempSrcI + 3 ] );
                        buf[ bufI++ ] = halfBytes[0];
                        buf[ bufI++ ] = halfBytes[1];
 
                        tempSrcI += _channels;
                    }
                }


                //First copy a line of B
                tempSrcI = srcDataI;
                for (int x = 0; x < _width; ++x)
                {	
                    //Blue
                    byte[] halfBytes = System.BitConverter.GetBytes( srcHalf[ tempSrcI + 2 ] );
                    buf[ bufI++ ] = halfBytes[0];
                    buf[ bufI++ ] = halfBytes[1];
 
                    tempSrcI += _channels;
                }
 
                //Then copy a line of G
                tempSrcI = srcDataI;
                for (int x = 0; x < _width; ++x)
                {	
                    //Blue
                    byte[] halfBytes = System.BitConverter.GetBytes( srcHalf[ tempSrcI + 1 ] );
                    buf[ bufI++ ] = halfBytes[0];
                    buf[ bufI++ ] = halfBytes[1];
 
                    tempSrcI += _channels;
                }
 
                //Finally copy a line of R
                tempSrcI = srcDataI;
                for (int x = 0; x < _width; ++x)
                {	
                    //Blue
                    byte[] halfBytes = System.BitConverter.GetBytes( srcHalf[ tempSrcI ] );
                    buf[ bufI++ ] = halfBytes[0];
                    buf[ bufI++ ] = halfBytes[1];
 
                    tempSrcI += _channels;
                }
 
                srcDataI += _width * _channels;
            }
 
            return buf;
        }
    }
}

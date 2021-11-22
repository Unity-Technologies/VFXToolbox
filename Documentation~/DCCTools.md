# DCC Tools

VFX Toolbox comes bundled with a set of DCC Tools that help export data for unity.

## Houdini DCC Tools for Visual Effect Graph

Houdini DCC Tools are located inside the `DCC-Tools~/Houdini` folder of the VFX Toolbox Package. This folder is not visible in editor but it can be browsed by clicking the Packages/VFX Toolbox Folder in the Project View, then selecting Show in Explorer from the Context Menu.

The Houdini Folder contains the following files:

* **Examples** (Folder) : Contains a set of houdini example files (.hipnc)
* **Unity_VFX_Tools.hda** : The digital Asset file containing the Unity VFX Toolbox Digital assets for Houdini.
* **VAT-ROP.hda** : The digital Asset file containing the Vertex Animation Texture ROP Exporter.

<u>Compatibility:</u> Houdini 16.5 and newer.


## VAT-ROP Export and Import Options

The VAT-ROP exports point data from frames of an animated mesh to textures. It produces textures with point data that correspond to the values of position and normal of a given SOP. Each texture is composed of lines of data, each line corresponding to one frame of animation. Every pixel on one line holds the data for a particular vertex.

For convenience, we advise to facet (unique points) and derive normals to points so we end up in a 1:1 point:vertex values for export.

### Baking Modes
It can be used in two different modes:
* *Non-uniform topology* : This mode assumes that the geometry is re-written every frame and will bake the absolute positions of vertices by groups of 3. You have to determine a maximum point count so it will drive the width of the texture. In non-uniform topology mode, degenerate input geometry must be used. There are some bundled in the VertexAnimationTextures Sample of VFX Toolbox.
* *Uniform topology* : This mode

### Export File Formats:
The exporter can run in two different file formats: EXR and TGA. EXR holds 32bit floating point value data per channel, whereas TGA only stores data in 8 bit per channel. TGA Also stores the -1 to 1 range in unsigned normalized.

### Limitations:
* Maximum of 16384 points per frame baked (maximum width of a texture in unity)
* Only Position / Normals baked at the moment
* TGA Export clamps values from an unit cube (-1 .. 1) in each axis, and stores it as unsigned normalized.

### Import in Unity
By deploying the VertexAnimationTextures sample from the VFXToolbox package manager page, you install in the project some assets that you can start with, or use as example to use vertex animation textures. The sample also contains an `AssetPostprocessor` that will configure any texture of extension `tga` or `exr` with the following suffixes `-VATPOS` (Position) or `-VATNRM` (Normals).

A `VAT-Simple` Shadergraph uses a sample `Sample VertexAnimationTexture` Subgraph that loads the value directly from the VertexID. Its use is demonstrated through the `VAT-SingleParticle.vfx` and `VAT-Characters.vfx` VFXGraphs

### Troubleshooting
* __In Uniform topology, the geometry can become garbled__: as if the triangles were in correct place, but somehow rotated. This is related to the winding order of the mesh. Please check the `Optimize Mesh` in the import options and ensure `Vertex Order` is enabled.
* __The animation is played backwards__ : This can happen when your texture is baked top to bottom. You can use the Reverse Frame order option in the exporter, or play it backwards (see the ReverseAnimation property in the `VAT-SingleParticle` example)
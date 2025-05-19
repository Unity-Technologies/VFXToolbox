Unity VFX tools for Houdini
===========================

Requirements:
=============

 * Houdini 19.5 Apprentice, Indie, Core or FX


Install:
========

 * Cherry pick the needed HDA by going to :File > Import > Houdini Digital Asset> Select the desired HDA > Install either to your "current Hip File" or "Scanned Asset directory"
 
 * Copy/Paste the needed HDA into your Houdini "otls" folder. By default should be : C:\Users\xxx\Documents\houdini19.5\otls
 
 * Copy/Paste the VFXGraph19.5.json (you can find it in the packages folder) into your own Houdini packages folder > C:\Users\xxx\Documents\houdini19.5\packages.
   From there edit the VFXGraph19.5.json file so that the VFXG VAR point to your VFXG folder.
	    
	"env": [
        {
            "VFXG": "C:/VFXToolbox/DCC-Tools~/Houdini/houdini19.5/VFXG"   <-- Edit 
        }


Release Notes:
==============

    2021 - 09 - 06: 
    ---------------
    .Change the Folder Structure to provide "packages style" installation
    .Updated Volume Tiling Tool and Volume Previz tool and embedded them into the master Unity_VFX_Tools.hda
    .6W lightmap Smoke Render Exporter 1.0 (Karma XPU)
    .6W lightmap Smoke Render Exporter 1.0 (OGL)    
    

    2021 - 11 - 21: 
    ---------------
    .Added VAT ROP Exporter

    2021 - 11 - 02: 
    ---------------
    .Upgraded PCache/VF exporters to python3
    .Added X flip option for export


    2018 - 11 - 02: 
    ---------------
    .Initial Version
        - Volume Exporter
        - Point Cache Exporter
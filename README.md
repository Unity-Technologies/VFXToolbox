# VFXToolbox
Additional tools for Visual Effect Artists.

## Install VFX Toolbox Unity Package

#### Local Package Install : 

* Git Clone this repository (or download zip and unzip locally)
* Install Unity 2019.3 or newer and run it for your project
* Open Package Manager Window (Window/Package Manager)
* Use the + Button located at the top-left of the window and select "Add Package from Disk"
* Navigate to the root of the repository directory then open the `package.json` file.

#### Git Reference Install (Package will be available as read-only):

* *(Make sure you have Git CLI installed on your system and path correctly configured)*
* Install Unity 2019.3 or newer
* Open your project's `Packages/manifest.json` file with a text editor
* Add the following line to `dependencies` list :  `"com.unity.vfx-toolbox": "https://github.com/Unity-Technologies/VFXToolbox.git#2.0.0-preview",`

## Image Sequencer

This utility enables the authoring of Flipbook Texture Sheets in an easy way. Create Image Sequence assets, import your texture sequences and start retiming, loop, and assemble into a flipbook texture sheet. 

![](https://i.imgur.com/UNcwTHi.gif)

By using templates, you can go back, make adjustments then re-export with only one click.

* You can create Image Sequence Templates using the `Create/Visual Effects/Image Sequence` project window menu
* You can open the Image Sequencer using the `Window/Visual Effects/Image Sequencer` menu

## DCC Tools

DCC Tools enable the export of .pcache and .vf files to be used with Visual Effect Graph. They are available for the following DCCs at the moment:

#### Houdini
* Point Cache
* Volume Exporter

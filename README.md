</p>
<p align="center">
  <img src="https://user-images.githubusercontent.com/68167377/231150151-63ad8c0e-b684-4ce7-80c6-cde7c5b6c1a9.png"/>
</p>

# FnO-Model-visualisation
This branch is for data visualisation that are represented by 3D mesh/model using AR/VR headsets running on target devices without the PC. [OpenFracture](https://github.com/dgreenheck/OpenFracture) library from dgreenheck is used for reality mesh slicing providing similar functionality as cutout plane in [VolumeBranch](https://github.com/SitronX/FnO-Hololens2-visualisation/tree/VolumetricData). Solution is tested to work on Hololens 2 (AR) and Oculus Quest 2 (VR).

Downloadable builds are available in the [Release](https://github.com/SitronX/FnO-Hololens2-visualisation/releases) section.



<table>
<tr>
     <th colspan="2" style="text-align:center;">
      <b>Click on images for video showcase</b>
    </th>
</tr>
  <tr>  
<th>

  [![watch the video](https://img.youtube.com/vi/iHUxRmJwxO0/hqdefault.jpg)](https://youtu.be/iHUxRmJwxO0)

</th>
<th>  

[![watch the video](https://img.youtube.com/vi/ZV28z-6UJyM/hqdefault.jpg)](https://youtu.be/ZV28z-6UJyM)

</th>
  </tr>
  <tr>
    <th> Hololens 2</th>
    <th> PCVR </th>
  </tr>
  </table>

# Manual

## Model processing

Follow this video tutorial to correctly process and import the model from medical data into the Unity

[![Watch the video](https://img.youtube.com/vi/affo3I7i2-8/hqdefault.jpg)](https://www.youtube.com/watch?v=affo3I7i2-8)

Individual sections: 
- [Exporting the model from segmentation in Slicer3D](https://youtu.be/affo3I7i2-8)
- [Processing the model in Blender](https://youtu.be/affo3I7i2-8?t=33)
- [Importing model into Unity](https://youtu.be/affo3I7i2-8?t=192)
- [Editor showcase](https://youtu.be/affo3I7i2-8?t=227)

## App build

### Hololens 2

In the scene, make sure the Hololens2 is selected here as target platform

<img src="https://user-images.githubusercontent.com/68167377/220965466-def9d8e6-4548-4c2a-a499-f89210b64484.jpg" width=720>

Make sure the UWP is selected in build and all settings are same as on next picture. Building the project like this will export it to the Visual Studio project.

<img src="https://user-images.githubusercontent.com/68167377/220967860-7e2aabeb-c191-4f59-b7c7-d614db96489e.jpg" width=720>

Visual Studio building and deployment is described [here](https://learn.microsoft.com/en-us/windows/mixed-reality/develop/advanced-concepts/using-visual-studio?tabs=hl2). The application should start running automatically on device after Visual Studio finishes the building and deployment process.

### Quest 2 / Quest 1

In the scene, make sure the Quest is selected here as target platform

<img src="https://user-images.githubusercontent.com/68167377/220969351-728b47f9-d943-4f9c-998c-a39885270cbb.jpg" width=720>

Make sure the Android is selected in build and all settings are same as on next picture

<img src="https://user-images.githubusercontent.com/68167377/231197260-d15debd4-1f9b-47dc-91bb-c9b1ccdfb337.jpg" width=720>

Simply select <b>build and run</b> option and app should automatically install on connected Quest device.

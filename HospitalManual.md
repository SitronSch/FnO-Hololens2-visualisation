</p>
<p align="center">
 <img src="https://user-images.githubusercontent.com/68167377/235357838-d1168679-e3a9-4819-bf30-274770078271.jpg" width="512">
</p>

# FnO-Manual

Hospital manual for Quest 2 version of the app.

## Running the app

Connect the VR headset to the PC with AirLink. After sucesfull connection, app can be directly launched from the desktop. The app looks like this.

![appIcon](https://user-images.githubusercontent.com/68167377/226698519-8b72c5fa-d1a3-417f-977c-93b427c15e7d.png)

In case of connecting issues, make sure that Oculus OpenXR runtime is set as the default. If OpenXR is not set as the default, a prompt will appear in the Oculus app to assist you in correctly setting it. The prompt is shown below.

<img src="https://user-images.githubusercontent.com/68167377/220205065-01c349e3-70ac-4937-b07f-08f988869e65.jpg" width=720>

## Opening the menu

To open the control menu inside the app, hold the X button on <b>left</b> controller.

![quest2controllers](https://user-images.githubusercontent.com/68167377/221322807-1cbd76a1-9683-422a-95f5-278ebebd5908.png)

The menu can be grabbed and anchored in the space using the second controller.

<img src="https://user-images.githubusercontent.com/68167377/226731942-d9b4f7f3-bd60-4a2a-86eb-a4182198416e.gif" width=512>

## Working with new Datasets
To add a custom dataset, follow these steps:

1. On desktop there is folder named <b>Datasets</b>. In this folder, create new folder with appropriate name that will hold your new dataset.

<img src="https://user-images.githubusercontent.com/68167377/226700616-7e8bf396-c91c-4197-90b7-67177fcb5248.png" width=512>

2. Create additional folder in the previously created folder, named <b> Data </b>.
3. Paste the medical dataset into the <b>Data</b> folder. Supported file types are: <b>NRRD,NIFTI,DICOM and JPG sequence</b>. The files need to have corresponding suffix matching the file they represent (eg: DICOM data will have a .dcm suffix). If you have a lot of files without suffix, you can create suffixes with with my simple renaming tool, which is available [here](https://github.com/SitronX/FileRenamer). If you have the whole patient study, see [here](https://github.com/SitronX/FnO-Hololens2-visualisation/blob/VolumetricData/DatasetExtraction.md) how you can extract the specific dataset from study using Slicer3D.
4. (Optional-Recommended) Create a second folder named <b>Thumbnail</b> and paste some image into that folder (.jpg or .png) so that the dataset is recognizable in the spawn menu.
5. (Optional) Create a third folder named <b>Labels</b> for segmentation support. Paste the segmentation [label map](https://slicer.readthedocs.io/en/latest/user_guide/modules/segmentations.html#export-segmentation-to-labelmap-volume) to this folder. The manual for correctly extracting segmentation from Slicer3D is available [here](https://github.com/SitronX/FnO-Hololens2-visualisation/blob/VolumetricData/SegmentationExtraction.md). The supported file types are the same as mentioned in step 3. 

### Spawning dataset in app

1. Open the hand menu. Datasets can be scrolled horizontally.
2. Double-click the dataset you want to spawn. Datasets are differentiated by thumbnails and the folder name you previously set.

<img src="https://user-images.githubusercontent.com/68167377/226214129-0902cc3d-da77-4714-8229-eea4af8b02a4.gif" width=512>

### Managing spawned datasets

When the dataset is spawned, it can be reset by clicking the same previous button.

<img src="https://user-images.githubusercontent.com/68167377/226214137-bf5b4928-0567-40d2-80b5-824463ec1050.gif" width=512>

Dataset can be enabled/disabled after the spawn.

<img src="https://user-images.githubusercontent.com/68167377/227978363-84184c7c-3a20-4c1f-a056-44d193ac88d3.gif" width=512>

## Changing Transfer-function

Some datasets might have problem with the default Transfer-function. Transfer-function provides color to every particle based on its density. When dataset appears to be washed-out with a lot of same color, it is best to manually correct the color positions. Adjusting is shown here.

<img src="https://user-images.githubusercontent.com/68167377/235325145-a59606d4-0b8e-4f5f-b693-99d4cd0861b0.gif" width=512>

The color positions are directly connected to the density slider, so when you select specific density interval, you can also set up what colors will be inside the interval.

<img src="https://user-images.githubusercontent.com/68167377/235325395-a57d97d5-73f9-445c-9fed-205abdd6797a.png" width=512>

You can also reset color positions by clicking <b>Reset TF</b> button, this will reset the colors to default state.

<img src="https://user-images.githubusercontent.com/68167377/235325466-2e4da136-9661-477b-bee1-37b235aaec3a.png" width=512>

## Segmentation module

If you placed the correct label map in the corresponding <b>Labels</b> folder, the segmentation module is available for that dataset. 

After loading the dataset, you can open the segmentation module by checking the segmentation checkbox.

<img src="https://github.com/SitronX/FnO-Hololens2-visualisation/assets/68167377/6a13221e-8487-43b2-92cd-a81ed845437d" width=450>

List of segments will appear. Segments are differentiated via color. You can control segments opacity by alpha sliders as shown below.

![SegmentControl](https://user-images.githubusercontent.com/68167377/226215616-12a93ab8-6ed5-4337-8343-c359e7364432.gif)

You can also change segment color by pressing the color button.

![ColorChange](https://user-images.githubusercontent.com/68167377/226215618-b020f276-95f3-4aec-9d1f-a27dcb70b995.gif)


In case you have multi-layer label map, you can control which segments will be shown by changing the alpha value of the segments. The segment with highest alpha value will cover the others in that location. If there are two segments with the same alpha value, the segment that is higher on the list will be displayed.

<img src="https://github.com/SitronX/FnO-Hololens2-visualisation/assets/68167377/8de9cb81-4f2b-4627-89b0-69788ba46055" width=450>

<b>Note:</b> Up to 8 layer label maps are supported.

## Rescaling objects

You can rescale objects by stretching/contracting them with both hands.

<img src="https://user-images.githubusercontent.com/68167377/235326145-678edfc2-2554-4a34-a7ff-00e61d2ac90a.gif" width=450>

## Slice planes

You can activate the slice view from the hand menu. Once enabled, it offers a grayscale visualization of the dataset, comparable to conventional 2D software. You can also modify the radiologic window using the slider located in the configuration panel for each dataset, allowing you to achieve better contrast.

![Slices](https://user-images.githubusercontent.com/68167377/235325795-1850c06f-6fe6-4494-b335-4d4a88b840c9.gif)

## Cutout methods

Cutout methods are selectable in <b>Additional settings </b>.

![Cutouts](https://user-images.githubusercontent.com/68167377/226217175-80e0391c-f703-4be6-9d3f-07ee8a61e382.gif)

## Multiple density sliders

You can add additional density sliders to have more visibility intervals. With this, use can visualise several different parts of the body with different density, while your view is unobstructed by irrelevant parts.

![MultipleSlider](https://user-images.githubusercontent.com/68167377/229572369-52f0b983-fee9-4475-a239-45fc3a86b15c.gif)

## More dataset options

Dataset options are available in the <b>Additional settings</b>.

### Downsampling

Some very large datasets can bring even powerful computers to their knees. When this happens, it's best to downsample the dataset.

By downsampling very large datasets, the quality loss is usually negligible with a real boost in performance.

1. Grab the previously spawned dataset you want to downsample. The dataset will then become active, and you will see its name, thumbnail and dimensions.
2. Press the downscale button (the dataset must finish previous loading).

<img src="https://user-images.githubusercontent.com/68167377/229569339-0788a979-aa92-42be-a603-74723dd70f99.gif" width=420>

### Mirror correction

Datasets should display correctly, but some specific datasets might still be affected by the mirror issue. If you detect that body parts are mirror flipped, you can manually flip it to the correct state.

![MirrorFlip](https://user-images.githubusercontent.com/68167377/229569745-eb5dc52e-8721-4928-9c6b-e372d6e6a5c7.gif)

## Changing background

It is possible to change the background according to the user's preference.

<table>
  <tr>  
<th>Default backgroung</th>
<th>Dark background</th>
<th>Light background</th>
<tr>  
<th>
  <img src="https://user-images.githubusercontent.com/68167377/226741250-eae58191-67f5-4b5f-aa80-b72b26d995d7.jpg" width=512>
</th>
<th>

  <img src="https://user-images.githubusercontent.com/68167377/226741372-54374c71-0e28-4f0a-93c4-0005c1949d66.jpg" width=512>
</th>
<th>

  <img src="https://user-images.githubusercontent.com/68167377/226741461-834e394c-e9e5-480b-9c08-57614d7a6d94.jpg" width=512>
</th>
  </tr>
  <tr>
    <th>Press <b> F2 </b> to activate </th>
    <th>Press <b> F3 </b> to activate </th>
    <th>Press <b> F4 </b> to activate </th>
  </tr>
  </table>
  
  
Note: <b>F1</b> button opens developer console for additional commands that you can use.


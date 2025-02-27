﻿#if UVR_USE_SIMPLEITK
using UnityEngine;
using System;
using itk.simple;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Globalization;
using Unity.Collections;

namespace UnityVolumeRendering
{
    /// <summary>
    /// SimpleITK-based DICOM importer.
    /// Has support for JPEG2000 and more.
    /// </summary>
    public class SimpleITKImageSequenceImporter : IImageSequenceImporter
    {
        public class ImageSequenceSlice : IImageSequenceFile
        {
            public string filePath;

            public string GetFilePath()
            {
                return filePath;
            }
        }

        public class ImageSequenceSeries : IImageSequenceSeries
        {
            public List<ImageSequenceSlice> files = new List<ImageSequenceSlice>();

            public IEnumerable<IImageSequenceFile> GetFiles()
            {
                return files;
            }
        }

        public IEnumerable<IImageSequenceSeries> LoadSeries(IEnumerable<string> files)
        {
            HashSet<string>  directories = new HashSet<string>();

            foreach (string file in files)
            {
                string dir = Path.GetDirectoryName(file);
                if (!directories.Contains(dir))
                    directories.Add(dir);
            }

            List<ImageSequenceSeries> seriesList = new List<ImageSequenceSeries>();
            Dictionary<string, VectorString> directorySeries = new Dictionary<string, VectorString>();
            foreach (string directory in directories)
            {
                VectorString seriesIDs = ImageSeriesReader.GetGDCMSeriesIDs(directory);
                directorySeries.Add(directory, seriesIDs);

            }

            foreach(var dirSeries in directorySeries)
            {
                foreach(string seriesID in dirSeries.Value)
                {
                    VectorString dicom_names = ImageSeriesReader.GetGDCMSeriesFileNames(dirSeries.Key, seriesID);
                    ImageSequenceSeries series = new ImageSequenceSeries();
                    foreach(string file in dicom_names)
                    {
                        ImageSequenceSlice sliceFile = new ImageSequenceSlice();
                        sliceFile.filePath = file;
                        series.files.Add(sliceFile);
                    }
                    seriesList.Add(series);
                }
            }

            return seriesList;
        }
        public async Task<IEnumerable<IImageSequenceSeries>> LoadSeriesAsync(IEnumerable<string> files,ProgressHandler progressHandler,bool isSegmentation)
        {
            List<ImageSequenceSeries> seriesList = null;
            await Task.Run(() => {
                HashSet<string> directories = new HashSet<string>();

                int totalCount = files.Count();
                int onePercent=totalCount/100;
                int percentCounter=0;
                int overall = 0;

                foreach (string file in files)
                {
                    string dir = Path.GetDirectoryName(file);
                    if (!directories.Contains(dir))
                        directories.Add(dir);

                    if(percentCounter > onePercent)
                    {
                        progressHandler.ReportProgress(overall, totalCount, isSegmentation ? "Loading segmentation slices...": "Loading main slices...");
                        percentCounter = 0;
                    }
                    percentCounter++;
                    overall++;
                }

                seriesList = new List<ImageSequenceSeries>();
                Dictionary<string, VectorString> directorySeries = new Dictionary<string, VectorString>();
                foreach (string directory in directories)
                {
                    VectorString seriesIDs = ImageSeriesReader.GetGDCMSeriesIDs(directory);
                    directorySeries.Add(directory, seriesIDs);

                }

                foreach (var dirSeries in directorySeries)
                {
                    foreach (string seriesID in dirSeries.Value)
                    {
                        VectorString dicom_names = ImageSeriesReader.GetGDCMSeriesFileNames(dirSeries.Key, seriesID);
                        ImageSequenceSeries series = new ImageSequenceSeries();
                        foreach (string file in dicom_names)
                        {
                            ImageSequenceSlice sliceFile = new ImageSequenceSlice();
                            sliceFile.filePath = file;
                            series.files.Add(sliceFile);
                        }
                        seriesList.Add(series);
                    }
                }

                
            });

            return seriesList;
        }

        public VolumeDataset ImportSeries(IImageSequenceSeries series)
        {
            ImageSequenceSeries sequenceSeries = (ImageSequenceSeries)series;
            if (sequenceSeries.files.Count == 0)
            {
                Debug.LogError("Empty series. No files to load.");
                return null;
            }

            ImageSeriesReader reader = new ImageSeriesReader();

            VectorString dicomNames = new VectorString();
            foreach (var dicomFile in sequenceSeries.files)
                dicomNames.Add(dicomFile.filePath);
            reader.SetFileNames(dicomNames);

            Image image = reader.Execute();

            // Cast to 32-bit float
            image = SimpleITK.Cast(image, PixelIDValueEnum.sitkFloat32);

            VectorUInt32 size = image.GetSize();

            int numPixels = 1;
            for (int dim = 0; dim < image.GetDimension(); dim++)
                numPixels *= (int)size[dim];

            // Read pixel data
            float[] pixelData = new float[numPixels];
            IntPtr imgBuffer = image.GetBufferAsFloat();
            Marshal.Copy(imgBuffer, pixelData, 0, numPixels);

            for (int i = 0; i < pixelData.Length; i++)
                pixelData[i] = Mathf.Clamp(pixelData[i], -1024, 3071);

            VectorDouble spacing = image.GetSpacing();

            // Create dataset
            VolumeDataset volumeDataset = new VolumeDataset();
            volumeDataset.data = pixelData;
            volumeDataset.dimX = (int)size[0];
            volumeDataset.dimY = (int)size[1];
            volumeDataset.dimZ = (int)size[2];
            volumeDataset.datasetName = "test";
            volumeDataset.filePath = dicomNames[0];
            volumeDataset.scaleX = (float)(spacing[0] * size[0]);
            volumeDataset.scaleY = (float)(spacing[1] * size[1]);
            volumeDataset.scaleZ = (float)(spacing[2] * size[2]);

            volumeDataset.FixDimensions();

            return volumeDataset;
        }
        public async Task<VolumeDataset> ImportSeriesAsync(IImageSequenceSeries series,string datasetName)
        {
            Image image = null;
            float[] pixelData = null;
            VectorUInt32 size = null;
            VectorString dicomNames = null;

            // Create dataset
            VolumeDataset volumeDataset = new VolumeDataset();

            ImageSequenceSeries sequenceSeries = (ImageSequenceSeries)series;
            if (sequenceSeries.files.Count == 0)
            {
                Debug.LogError("Empty series. No files to load.");
                return (null);
            }

            await Task.Run(() => {
               
                ImageSeriesReader reader = new ImageSeriesReader();

                string first = sequenceSeries.files.First().filePath;
                string last = sequenceSeries.files.Last().filePath;

                dicomNames = new VectorString();
          
                foreach (var dicomFile in sequenceSeries.files)
                    dicomNames.Add(dicomFile.filePath);
                reader.SetFileNames(dicomNames);
            
                image = reader.Execute();

                // Cast to 32-bit float
                image = SimpleITK.Cast(image, PixelIDValueEnum.sitkFloat32);

                size = image.GetSize();

                int numPixels = 1;
                for (int dim = 0; dim < image.GetDimension(); dim++)
                    numPixels *= (int)size[dim];

                // Read pixel data
                pixelData = new float[numPixels];
                IntPtr imgBuffer = image.GetBufferAsFloat();
                Marshal.Copy(imgBuffer, pixelData, 0, numPixels);

                VectorDouble spacing = image.GetSpacing();


                volumeDataset.data = pixelData.Reverse().ToArray();
                volumeDataset.dimX = (int)size[0];
                volumeDataset.dimY = (int)size[1];
                volumeDataset.dimZ = (int)size[2];

                volumeDataset.datasetName = datasetName;
                volumeDataset.filePath = dicomNames[0];
                volumeDataset.scaleX = (float)(spacing[0] * size[0]);
                volumeDataset.scaleY = (float)(spacing[1] * size[1]);
                volumeDataset.scaleZ = (float)(spacing[2] * size[2]);

                volumeDataset.FixDimensions();
            });
            
            return volumeDataset;
        }
        public async Task ImportSeriesSegmentationAsync(IImageSequenceSeries series,VolumeDataset volumeDataset)
        {
            Image image = null;
            float[] pixelData = null;
            VectorUInt32 size = null;
            VectorString dicomNames = null;


            ImageSequenceSeries sequenceSeries = (ImageSequenceSeries)series;
            if (sequenceSeries.files.Count == 0)
            {
                Debug.LogError("Empty series. No files to load.");
                return;
            }

            await Task.Run(() => {

                ImageSeriesReader reader = new ImageSeriesReader();

                dicomNames = new VectorString();

                foreach (var dicomFile in sequenceSeries.files)
                    dicomNames.Add(dicomFile.filePath);
                reader.SetFileNames(dicomNames);

                image = reader.Execute();              

                // Cast to 32-bit float
                image = SimpleITK.Cast(image, PixelIDValueEnum.sitkFloat32);

                size = image.GetSize();

                if (size[0] != volumeDataset.dimX || size[1] != volumeDataset.dimY || size[2] != volumeDataset.dimZ)
                    ErrorNotifier.Instance.AddErrorMessageToUser($"Segmentation file in dataset named: {volumeDataset.datasetName} in folder Data has other dimensions than base dataset.");

                int numPixels = 1;
                for (int dim = 0; dim < image.GetDimension(); dim++)
                    numPixels *= (int)size[dim];

                // Read pixel data
                pixelData = new float[numPixels];
                IntPtr imgBuffer = image.GetBufferAsFloat();
                Marshal.Copy(imgBuffer, pixelData, 0, numPixels);

                for (int i = 0; i < pixelData.Length; i++)
                    pixelData[i] = Mathf.Clamp(pixelData[i], -1024, 3071);

                VectorDouble spacing = image.GetSpacing();

                NativeArray<float>[] labelData = new NativeArray<float>[1];     //Dicom segmentation map only supports 1 layer, only NRRD supports multiple layers

                labelData[0] = new NativeArray<float>(pixelData.Reverse().ToArray(), Allocator.Persistent);


                volumeDataset.LabelValues.Add(new Dictionary<float, float>());
                volumeDataset.LabelNames.Add(new Dictionary<float, string>());

                volumeDataset.nativeLabelData = labelData;
                volumeDataset.labelDimX = (int)size[0];
                volumeDataset.labelDimY = (int)size[1];
                volumeDataset.labelDimZ = (int)size[2];
                volumeDataset.HowManyLabelMapLayers = 1;
            });

        }
      

    }
}
#endif

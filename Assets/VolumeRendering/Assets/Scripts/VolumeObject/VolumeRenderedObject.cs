﻿using System.Collections.Generic;
using UnityEngine;

namespace UnityVolumeRendering
{
    [ExecuteInEditMode]
    public class VolumeRenderedObject : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        public TransferFunction transferFunction;

        [SerializeField, HideInInspector]
        public TransferFunction2D transferFunction2D;

        [SerializeField, HideInInspector]
        public VolumeDataset dataset;

        [SerializeField, HideInInspector]
        public MeshRenderer meshRenderer;


        [SerializeField, HideInInspector]
        private RenderMode renderMode;
        [SerializeField, HideInInspector]
        private TFRenderMode tfRenderMode;
        [SerializeField, HideInInspector]
        private bool lightingEnabled;
        [SerializeField, HideInInspector]
        private LightSource lightSource;

        private float[] _lowerVisibilityWindow = new float[500];
        private float[] _upperVisibilityWindow = new float[500];
        private int _visibilitySlidersCount = 1;

        [SerializeField, HideInInspector]
        private bool rayTerminationEnabled = true;
    
        [SerializeField, HideInInspector]
        private bool cubicInterpolationEnabled = false;

        private CrossSectionManager crossSectionManager;


        public SlicingPlane CreateSlicingPlane()
        {
            GameObject sliceRenderingPlane = GameObject.Instantiate(Resources.Load<GameObject>("SlicingPlane"));
            sliceRenderingPlane.transform.parent = transform;
            sliceRenderingPlane.transform.localPosition = Vector3.zero;
            sliceRenderingPlane.transform.localRotation = Quaternion.identity;
            sliceRenderingPlane.transform.localScale = Vector3.one * 0.1f; // TODO: Change the plane mesh instead and use Vector3.one
            MeshRenderer sliceMeshRend = sliceRenderingPlane.GetComponent<MeshRenderer>();
            sliceMeshRend.material = new Material(sliceMeshRend.sharedMaterial);
            Material sliceMat = sliceRenderingPlane.GetComponent<MeshRenderer>().sharedMaterial;
            sliceMat.SetTexture("_DataTex", dataset.GetDataTexture());
            sliceMat.SetTexture("_TFTex", transferFunction.GetTexture());
            sliceMat.SetMatrix("_parentInverseMat", transform.worldToLocalMatrix);
            sliceMat.SetMatrix("_planeMat", Matrix4x4.TRS(sliceRenderingPlane.transform.position, sliceRenderingPlane.transform.rotation, transform.lossyScale)); // TODO: allow changing scale

            SlicingPlane slicingPlaneComp = sliceRenderingPlane.GetComponent<SlicingPlane>();
            slicingPlaneComp.targetObject = this;
            return slicingPlaneComp;
        }
        public void FillSlicingPlaneWithData(GameObject sliceRenderingPlane)
        {
            //sliceRenderingPlane.transform.parent = transform;
            //sliceRenderingPlane.transform.localPosition = Vector3.zero;
            //sliceRenderingPlane.transform.localRotation = Quaternion.identity;
            //sliceRenderingPlane.transform.localScale = Vector3.one * 0.1f; // TODO: Change the plane mesh instead and use Vector3.one
            MeshRenderer sliceMeshRend = sliceRenderingPlane.GetComponent<MeshRenderer>();
            sliceMeshRend.material = new Material(sliceMeshRend.sharedMaterial);
            Material sliceMat = sliceRenderingPlane.GetComponent<MeshRenderer>().sharedMaterial;
            sliceMat.SetTexture("_DataTex", dataset.GetDataTexture());
            sliceMat.SetTexture("_TFTex", transferFunction.GetTexture());
            sliceMat.SetMatrix("_parentInverseMat", transform.worldToLocalMatrix);
            sliceMat.SetMatrix("_planeMat", Matrix4x4.TRS(sliceRenderingPlane.transform.position, sliceRenderingPlane.transform.rotation, transform.lossyScale)); // TODO: allow changing scale

            SlicingPlane slicingPlaneComp = sliceRenderingPlane.GetComponent<SlicingPlane>();
            slicingPlaneComp.targetObject = this;
        }

        public void SetRenderMode(RenderMode mode)
        {
            if (renderMode != mode)
            {
                renderMode = mode;
                //SetVisibilityWindow(0.0f, 1.0f,0,0); // reset visibility window
            }
            UpdateMaterialProperties();
        }

        public void SetTransferFunctionMode(TFRenderMode mode)
        {
            tfRenderMode = mode;
            if (tfRenderMode == TFRenderMode.TF1D && transferFunction != null)
                transferFunction.GenerateTexture();
            else if (transferFunction2D != null)
                transferFunction2D.GenerateTexture();
            UpdateMaterialProperties();
        }

        public TFRenderMode GetTransferFunctionMode()
        {
            return tfRenderMode;
        }

        public RenderMode GetRenderMode()
        {
            return renderMode;
        }

        public bool GetLightingEnabled()
        {
            return lightingEnabled;
        }

        public LightSource GetLightSource()
        {
            return lightSource;
        }

        public CrossSectionManager GetCrossSectionManager()
        {
            if (crossSectionManager == null)
                crossSectionManager = GetComponent<CrossSectionManager>();
            if (crossSectionManager == null)
                crossSectionManager = gameObject.AddComponent<CrossSectionManager>();
            return crossSectionManager;
        }

        public void SetLightingEnabled(bool enable)
        {
            if (enable != lightingEnabled)
            {
                lightingEnabled = enable;
                UpdateMaterialProperties();
            }
        }

        public void SetLightSource(LightSource source)
        {
            lightSource = source;
            UpdateMaterialProperties();
        }

        public void SetVisibilityWindow(float[] lowerVisibilityWindow, float[] upperVisibilityWindow,int visibilitySlidersCount)
        {
            _lowerVisibilityWindow = lowerVisibilityWindow;
            _upperVisibilityWindow=upperVisibilityWindow;
            _visibilitySlidersCount=visibilitySlidersCount;

            UpdateMaterialProperties();
        }
        public void InitVisiblityWindow()
        {
            _lowerVisibilityWindow[0]=0;
            _upperVisibilityWindow[0]=1;
            for (int i=1;i<500;i++)
            {
                _lowerVisibilityWindow[i]=0;
                _upperVisibilityWindow[i]=0;
            }
            
        }

        public void SetVisibilityWindow(Vector4 val)
        {
            //
        }
        public Vector4 GetVisibilityWindow()
        {
            return Vector4.zero;
        }

        public bool GetRayTerminationEnabled()
        {
            return rayTerminationEnabled;
        }

        public void SetRayTerminationEnabled(bool enable)
        {
            if (enable != rayTerminationEnabled)
            {
                rayTerminationEnabled = enable;
                UpdateMaterialProperties();
            }
        }

        [System.Obsolete("Back-to-front rendering no longer supported")]
        public bool GetDVRBackwardEnabled()
        {
            return false;
        }

        [System.Obsolete("Back-to-front rendering no longer supported")]
        public void SetDVRBackwardEnabled(bool enable)
        {
            Debug.LogWarning("Back-to-front rendering no longer supported");
        }

        public bool GetCubicInterpolationEnabled()
        {
            return cubicInterpolationEnabled;
        }

        public void SetCubicInterpolationEnabled(bool enable)
        {
            if (enable != cubicInterpolationEnabled)
            {
                cubicInterpolationEnabled = enable;
                UpdateMaterialProperties();
            }
        }

        public void SetTransferFunction(TransferFunction tf)
        {
            this.transferFunction = tf;
            UpdateMaterialProperties();
        }
        public void SetTransferFunction2D(TransferFunction2D tf)
        {
            this.transferFunction2D = tf;
            UpdateMaterialProperties();
        }

        private void UpdateMaterialProperties()
        {
            bool useGradientTexture = tfRenderMode == TFRenderMode.TF2D || renderMode == RenderMode.IsosurfaceRendering || lightingEnabled;
            meshRenderer.sharedMaterial.SetTexture("_GradientTex", useGradientTexture ? dataset.GetGradientTexture() : null);

            if (tfRenderMode == TFRenderMode.TF2D)
            {
                meshRenderer.sharedMaterial.SetTexture("_TFTex", transferFunction2D.GetTexture());
                meshRenderer.sharedMaterial.EnableKeyword("TF2D_ON");
            }
            else
            {
                meshRenderer.sharedMaterial.SetTexture("_TFTex", transferFunction.GetTexture());
                meshRenderer.sharedMaterial.DisableKeyword("TF2D_ON");
            }

            if (lightingEnabled)
                meshRenderer.sharedMaterial.EnableKeyword("LIGHTING_ON");
            else
                meshRenderer.sharedMaterial.DisableKeyword("LIGHTING_ON");

            if (lightSource == LightSource.SceneMainLight)
                meshRenderer.sharedMaterial.EnableKeyword("USE_MAIN_LIGHT");
            else
                meshRenderer.sharedMaterial.DisableKeyword("USE_MAIN_LIGHT");

            switch (renderMode)
            {
                case RenderMode.DirectVolumeRendering:
                    {
                        meshRenderer.sharedMaterial.EnableKeyword("MODE_DVR");
                        meshRenderer.sharedMaterial.DisableKeyword("MODE_MIP");
                        meshRenderer.sharedMaterial.DisableKeyword("MODE_SURF");
                        break;
                    }
                case RenderMode.MaximumIntensityProjectipon:
                    {
                        meshRenderer.sharedMaterial.DisableKeyword("MODE_DVR");
                        meshRenderer.sharedMaterial.EnableKeyword("MODE_MIP");
                        meshRenderer.sharedMaterial.DisableKeyword("MODE_SURF");
                        break;
                    }
                case RenderMode.IsosurfaceRendering:
                    {
                        meshRenderer.sharedMaterial.DisableKeyword("MODE_DVR");
                        meshRenderer.sharedMaterial.DisableKeyword("MODE_MIP");
                        meshRenderer.sharedMaterial.EnableKeyword("MODE_SURF");
                        break;
                    }
            }

            meshRenderer.material.SetFloatArray("_lowerVisibilityWindow", _lowerVisibilityWindow);
            meshRenderer.material.SetFloatArray("_upperVisibilityWindow", _upperVisibilityWindow);
            meshRenderer.material.SetFloat("_visibilitySlidersCount", _visibilitySlidersCount);

            meshRenderer.material.SetVector("_TextureSize", new Vector3(dataset.dimX, dataset.dimY, dataset.dimZ));

            if (rayTerminationEnabled)
                meshRenderer.sharedMaterial.EnableKeyword("RAY_TERMINATE_ON");
            else
                meshRenderer.sharedMaterial.DisableKeyword("RAY_TERMINATE_ON");

            if(cubicInterpolationEnabled)
                meshRenderer.sharedMaterial.EnableKeyword("CUBIC_INTERPOLATION_ON");
            else
                meshRenderer.sharedMaterial.DisableKeyword("CUBIC_INTERPOLATION_ON");

        }

    }
}

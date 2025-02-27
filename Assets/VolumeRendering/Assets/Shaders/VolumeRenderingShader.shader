﻿Shader "VolumeRendering/VolumeRenderingShader"
{
    Properties
    {
        _DataTex ("Data Texture (Generated)", 3D) = "" {}
        _LabelTex("Label Texture (Generated)",3D)=""{}
        _LabelTexSec("Label TextureSec (Generated)",3D) = ""{}
        _GradientTex("Gradient Texture (Generated)", 3D) = "" {}
        _NoiseTex("Noise Texture (Generated)", 2D) = "white" {}
        _TFTex("Transfer Function Texture (Generated)", 2D) = "" {}
        _stepNumber("Step number",int)=512
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100
        Cull Front
        ZTest LEqual
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
// Upgrade NOTE: excluded shader from DX11 because it uses wrong array syntax (type[size] name)
            #pragma multi_compile MODE_DVR MODE_MIP MODE_SURF
            #pragma multi_compile __ TF2D_ON
            #pragma multi_compile __ CROSS_SECTION_ON
            #pragma multi_compile __ LIGHTING_ON
            #pragma multi_compile DEPTHWRITE_ON DEPTHWRITE_OFF
            #pragma multi_compile __ RAY_TERMINATE_ON
            #pragma multi_compile __ USE_MAIN_LIGHT
            #pragma multi_compile __ CUBIC_INTERPOLATION_ON
            #pragma multi_compile __ LABELING_SUPPORT_ON
            #pragma multi_compile __ SECOND_LABEL_TEXTURE_ON
            #pragma multi_compile __ MODIFY_BRIGHTNESS_IN_LABELING
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "TricubicSampling.cginc"

            #define AMBIENT_LIGHTING_FACTOR 0.5
            #define JITTER_FACTOR 5.0

            struct vert_in
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex : POSITION;
                float4 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct frag_in
            {
                UNITY_VERTEX_OUTPUT_STEREO
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 vertexLocal : TEXCOORD1;
                float3 normal : NORMAL;
            };

            struct frag_out
            {
                float4 colour : SV_TARGET;
#if DEPTHWRITE_ON
                float depth : SV_DEPTH;
#endif
            };

            sampler3D _DataTex;
            sampler3D _LabelTex;
            sampler3D _LabelTexSec;
            sampler3D _GradientTex;
            sampler2D _NoiseTex;
            sampler2D _TFTex;

            float3 _TextureSize;
            int _stepNumber;
            float4 _SegmentsColors[255];                                       //Dynamic arrays are not possible, so here it is capped to 255 segments
            int _SegmentsColorsLayersStride[255];                               //Due to label map support of multiple layers, in each layer lets take its stride by index and only after that take correct color position
            int _HowManyLabelLayers;
          
            float _lowerVisibilityWindow[500];
            float _upperVisibilityWindow[500];
            int _visibilitySlidersCount = 1;

#if CROSS_SECTION_ON
#define CROSS_SECTION_TYPE_PLANE 1 
#define CROSS_SECTION_TYPE_BOX_INCL 2 
#define CROSS_SECTION_TYPE_BOX_EXCL 3
#define CROSS_SECTION_TYPE_SPHERE_INCL 4
#define CROSS_SECTION_TYPE_SPHERE_EXCL 5
            float4x4 _CrossSectionMatrices[8];
            float _CrossSectionTypes[8];
            int _NumCrossSections;
#endif

            struct RayInfo
            {
                float3 startPos;
                float3 endPos;
                float3 direction;
                float2 aabbInters;
            };

            struct RaymarchInfo
            {
                RayInfo ray;
                int numSteps;
                float numStepsRecip;
                float stepSize;
            };
            float3 RGBtoHSV(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 HSVtoRGB(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
            }


            float3 getViewRayDir(float3 vertexLocal)
            {
                if(unity_OrthoParams.w == 0)
                {
                    // Perspective
                    return normalize(ObjSpaceViewDir(float4(vertexLocal, 0.0f)));
                }
                else
                {
                    // Orthographic
                    float3 camfwd = mul((float3x3)unity_CameraToWorld, float3(0,0,-1));
                    float4 camfwdobjspace = mul(unity_WorldToObject, camfwd);
                    return normalize(camfwdobjspace);
                }
            }

            // Find ray intersection points with axis aligned bounding box
            float2 intersectAABB(float3 rayOrigin, float3 rayDir, float3 boxMin, float3 boxMax)
            {
                float3 tMin = (boxMin - rayOrigin) / rayDir;
                float3 tMax = (boxMax - rayOrigin) / rayDir;
                float3 t1 = min(tMin, tMax);
                float3 t2 = max(tMin, tMax);
                float tNear = max(max(t1.x, t1.y), t1.z);
                float tFar = min(min(t2.x, t2.y), t2.z);
                return float2(tNear, tFar);
            };

            // Get a ray for the specified fragment (back-to-front)
            RayInfo getRayBack2Front(float3 vertexLocal)
            {
                RayInfo ray;
                ray.direction = getViewRayDir(vertexLocal);
                ray.startPos = vertexLocal + float3(0.5f, 0.5f, 0.5f);
                // Find intersections with axis aligned boundinng box (the volume)
                ray.aabbInters = intersectAABB(ray.startPos, ray.direction, float3(0.0, 0.0, 0.0), float3(1.0f, 1.0f, 1.0));

                // Check if camera is inside AABB
                const float3 farPos = ray.startPos + ray.direction * ray.aabbInters.y - float3(0.5f, 0.5f, 0.5f);
                float4 clipPos = UnityObjectToClipPos(float4(farPos, 1.0f));
                ray.aabbInters += min(clipPos.w, 0.0);

                ray.endPos = ray.startPos + ray.direction * ray.aabbInters.y;
                return ray;
            }

            // Get a ray for the specified fragment (front-to-back)
            RayInfo getRayFront2Back(float3 vertexLocal)
            {
                RayInfo ray = getRayBack2Front(vertexLocal);
                ray.direction = -ray.direction;
                float3 tmp = ray.startPos;
                ray.startPos = ray.endPos;
                ray.endPos = tmp;
                return ray;
            }

            RaymarchInfo initRaymarch(RayInfo ray, int maxNumSteps)
            {
                RaymarchInfo raymarchInfo;
                raymarchInfo.stepSize = 1.732f/*greatest distance in box*/ / maxNumSteps;
                raymarchInfo.numSteps = (int)clamp(abs(ray.aabbInters.x - ray.aabbInters.y) / raymarchInfo.stepSize, 1, maxNumSteps);
                raymarchInfo.numStepsRecip = 1.0 / raymarchInfo.numSteps;
                return raymarchInfo;
            }

            // Gets the colour from a 1D Transfer Function (x = density)
            float4 getTF1DColour(float density)
            {
                return tex2Dlod(_TFTex, float4(density, 0.0f, 0.0f, 0.0f));
            }

            // Gets the colour from a 2D Transfer Function (x = density, y = gradient magnitude)
            float4 getTF2DColour(float density, float gradientMagnitude)
            {
                return tex2Dlod(_TFTex, float4(density, gradientMagnitude, 0.0f, 0.0f));
            }

            // Gets the density at the specified position
            float getDensity(float3 pos)
            {
#if CUBIC_INTERPOLATION_ON
                return interpolateTricubicFast(_DataTex, float3(pos.x, pos.y, pos.z), _TextureSize);
#else
                return tex3Dlod(_DataTex, float4(pos.x, pos.y, pos.z, 0.0f));
#endif
            }

            float4 getLabel(float3 pos)
            {   
                return tex3Dlod(_LabelTex, float4(pos.x, pos.y, pos.z, 0.0f));
            }
            float4 getLabelSec(float3 pos)
            {
                return tex3Dlod(_LabelTexSec, float4(pos.x, pos.y, pos.z, 0.0f));
            }

            // Gets the gradient at the specified position
            float3 getGradient(float3 pos)
            {
#if CUBIC_INTERPOLATION_ON
                return interpolateTricubicFast(_GradientTex, float3(pos.x, pos.y, pos.z), _TextureSize);
#else
                return tex3Dlod(_GradientTex, float4(pos.x, pos.y, pos.z, 0.0f)).rgb;
#endif
            }

            // Get the light direction (using main light or view direction, based on setting)
            float3 getLightDirection(float3 viewDir)
            {
#if defined(USE_MAIN_LIGHT)
                return normalize(mul(unity_WorldToObject, _WorldSpaceLightPos0.xyz));
#else
                return viewDir;
#endif
            }

            // Performs lighting calculations, and returns a modified colour.
            float3 calculateLighting(float3 col, float3 normal, float3 lightDir, float3 eyeDir, float specularIntensity)
            {
                // Invert normal if facing opposite direction of view direction.
                // Optimised version of: if(dot(normal, eyeDir) < 0.0) normal *= -1.0
                normal *= (step(0.0, dot(normal, eyeDir)) * 2.0 - 1.0);

                float ndotl = max(lerp(0.0f, 1.5f, dot(normal, lightDir)), AMBIENT_LIGHTING_FACTOR);
                float3 diffuse = ndotl * col;
                float3 v = eyeDir;
                float3 r = normalize(reflect(-lightDir, normal));
                float rdotv = max( dot( r, v ), 0.0 );
                float3 specular = pow(rdotv, 32.0f) * float3(1.0f, 1.0f, 1.0f) * specularIntensity;
                return diffuse + specular;
            }

            // Converts local position to depth value
            float localToDepth(float3 localPos)
            {
                float4 clipPos = UnityObjectToClipPos(float4(localPos, 1.0f));

#if defined(SHADER_API_GLCORE) || defined(SHADER_API_OPENGL) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
                return (clipPos.z / clipPos.w) * 0.5 + 0.5;
#else
                return clipPos.z / clipPos.w;
#endif
            }

            bool IsCutout(float3 currPos)
            {
#if CROSS_SECTION_ON
                // Move the reference in the middle of the mesh, like the pivot
                float4 pivotPos = float4(currPos - float3(0.5f, 0.5f, 0.5f), 1.0f);

                bool clipped = false;
                for (int i = 0; i < _NumCrossSections && !clipped; ++i)
                {
                    const int type = (int)_CrossSectionTypes[i];
                    const float4x4 mat = _CrossSectionMatrices[i];

                    // Convert from model space to plane's vector space
                    float3 planeSpacePos = mul(mat, pivotPos);
                    if (type == CROSS_SECTION_TYPE_PLANE)
                        clipped = planeSpacePos.z > 0.0f;
                    else if (type == CROSS_SECTION_TYPE_BOX_INCL)
                        clipped = !(planeSpacePos.x >= -0.5f && planeSpacePos.x <= 0.5f && planeSpacePos.y >= -0.5f && planeSpacePos.y <= 0.5f && planeSpacePos.z >= -0.5f && planeSpacePos.z <= 0.5f);
                    else if (type == CROSS_SECTION_TYPE_BOX_EXCL)
                        clipped = planeSpacePos.x >= -0.5f && planeSpacePos.x <= 0.5f && planeSpacePos.y >= -0.5f && planeSpacePos.y <= 0.5f && planeSpacePos.z >= -0.5f && planeSpacePos.z <= 0.5f;
                    else if (type == CROSS_SECTION_TYPE_SPHERE_INCL)
                        clipped = length(planeSpacePos) > 0.5;
                    else if (type == CROSS_SECTION_TYPE_SPHERE_EXCL)
                        clipped = length(planeSpacePos) < 0.5;
                }
                return clipped;
#else
                return false;
#endif
            }

            frag_in vert_main (vert_in v)
            {
                frag_in o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(frag_in, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.vertexLocal = v.vertex;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            // Direct Volume Rendering
            frag_out frag_dvr(frag_in i)
            {
                //#define MAX_NUM_STEPS 512
                #define OPACITY_THRESHOLD (1.0 - 1.0 / 255.0)

                RayInfo ray = getRayFront2Back(i.vertexLocal);
                RaymarchInfo raymarchInfo = initRaymarch(ray, _stepNumber);

                float3 lightDir = normalize(ObjSpaceViewDir(float4(float3(0.0f, 0.0f, 0.0f), 0.0f)));

                // Create a small random offset in order to remove artifacts
                ray.startPos += (JITTER_FACTOR * ray.direction * raymarchInfo.stepSize) * tex2D(_NoiseTex, float2(i.uv.x, i.uv.y)).r;

                float4 col = float4(0.0f, 0.0f, 0.0f, 0.0f);

                float tDepth = raymarchInfo.numStepsRecip * (raymarchInfo.numSteps - 1);
                for (int iStep = 0; iStep < raymarchInfo.numSteps; iStep++)
                {
                    const float t = iStep * raymarchInfo.numStepsRecip;
                    const float3 currPos = lerp(ray.startPos, ray.endPos, t);
   
                    // Perform slice culling (cross section plane)
#ifdef CROSS_SECTION_ON
                    if(IsCutout(currPos))
                    	continue;
#endif


                    // Get the dansity/sample value of the current position
                    const float density = getDensity(currPos);
                              
                    bool isInInterval = false;
                    // Apply visibility window
                    for (int i = 0; i < _visibilitySlidersCount; i++)
                    {
                        if ((density > _lowerVisibilityWindow[i]) && (density < _upperVisibilityWindow[i]))
                        {
							isInInterval =true;
							break;
						}
                    }
                    if (!isInInterval)
                        continue;

#ifdef LABELING_SUPPORT_ON

                    float4 label = getLabel(currPos)*255.0;

                    float4 src = float4(0, 0, 0, 0);

#ifdef SECOND_LABEL_TEXTURE_ON

                    for (int i = 0; i < 4; i++)
                    {
                        if (label[i] != 0)
                        {
                            float4 tmp = _SegmentsColors[_SegmentsColorsLayersStride[i] + label[i]-1 ];
                            if (tmp.a > src.a)
                                src = tmp;
                        }
                    }
                    float4 labelSec = getLabelSec(currPos) * 255.0;

                    for (int i = 0; i < _HowManyLabelLayers - 4; i++)
                    {
                        if (labelSec[i] != 0)
                        {
                            float4 tmp = _SegmentsColors[_SegmentsColorsLayersStride[4+i] + labelSec[i]-1 ];
                            if (tmp.a > src.a)
                                src = tmp;
                        }
                    }
#else

                    for (int i = 0; i < _HowManyLabelLayers; i++)
                    {
                        if (label[i] != 0)
                        {
                            float4 tmp = _SegmentsColors[_SegmentsColorsLayersStride[i] + label[i]-1 ];
                            if (tmp.a > src.a)
                                src = tmp;
                        }
                    }
#endif


#ifdef MODIFY_BRIGHTNESS_IN_LABELING                    //Density value is preserved in color brightness
                    float3 hsv = RGBtoHSV(src);
                    hsv.z = lerp(0.3,1,density);
                    float3 newColor = HSVtoRGB(hsv);
                    src.xyz = newColor;
#endif

                    src.a*= density * 0.7;              //0.7 works best to smooth ugly edges

                    if (src.a < 0.01)
                        continue;
#else  
                    float4 src = getTF1DColour(density);
                    if (src.a < 0.01)
                        continue;
#endif

                    // Apply lighting
#if defined(LIGHTING_ON) 
                    float3 gradient = getGradient(currPos);
                    float gradMag = length(gradient);
                    float gradMagNorm = gradMag / 1.75f;

                    src.rgb = calculateLighting(src.rgb, gradient/gradMag, getLightDirection(ray.direction), ray.direction, 0.3f);
#endif


                    src.rgb *= src.a;
                    col = (1.0f - col.a) * src + col;

                    if (col.a > 0.15 && t < tDepth) {
                        tDepth = t;
                    }
           

                    // Early ray termination
#if defined(RAY_TERMINATE_ON)
                    if (col.a > OPACITY_THRESHOLD) {
                        break;
                    }
#endif
                }

                // Write fragment output
                frag_out output;
                output.colour = col;

#if DEPTHWRITE_ON
                tDepth += (step(col.a, 0.0) * 1000.0); // Write large depth if no hit
                const float3 depthPos = lerp(ray.startPos, ray.endPos, tDepth) - float3(0.5f, 0.5f, 0.5f);
                output.depth = localToDepth(depthPos);
#endif
                return output;
            }

            // Maximum Intensity Projection mode
            frag_out frag_mip(frag_in i)
            {
                RayInfo ray = getRayBack2Front(i.vertexLocal);
                RaymarchInfo raymarchInfo = initRaymarch(ray, _stepNumber);

                float maxDensity = 0.0f;
                float3 maxDensityPos = ray.startPos;
                for (int iStep = 0; iStep < raymarchInfo.numSteps; iStep++)
                {
                    const float t = iStep * raymarchInfo.numStepsRecip;
                    const float3 currPos = lerp(ray.startPos, ray.endPos, t);
                    
#ifdef CROSS_SECTION_ON
                    if (IsCutout(currPos))
                        continue;
#endif


                    const float density = getDensity(currPos);

                    bool isInInterval = false;
                    // Apply visibility window
                    for (int i = 0; i < _visibilitySlidersCount; i++)
                    {
                        if ((density > _lowerVisibilityWindow[i]) && (density < _upperVisibilityWindow[i]))
                        {
                            isInInterval = true;
                            break;
                        }
                    }
                    if (!isInInterval)
                        continue;

                    if (density > maxDensity)
                    {
                        maxDensity = density;
                        maxDensityPos = currPos;
                    }
                }

                // Write fragment output
                frag_out output;
                output.colour = float4(1.0f, 1.0f, 1.0f, maxDensity); // maximum intensity
#if DEPTHWRITE_ON
                output.depth = localToDepth(maxDensityPos - float3(0.5f, 0.5f, 0.5f));
#endif
                return output;
            }

            // Surface rendering mode
            // Draws the first point (closest to camera) with a density within the user-defined thresholds.
            frag_out frag_surf(frag_in i)
            {
                //#define MAX_NUM_STEPS 512

                RayInfo ray = getRayFront2Back(i.vertexLocal);                     
                RaymarchInfo raymarchInfo = initRaymarch(ray, _stepNumber);

                // Create a small random offset in order to remove artifacts
                ray.startPos = ray.startPos + (JITTER_FACTOR * ray.direction * raymarchInfo.stepSize) * tex2D(_NoiseTex, float2(i.uv.x, i.uv.y)).r;

                float4 col = float4(0,0,0,0);
                for (int iStep = 0; iStep < raymarchInfo.numSteps; iStep++)
                {
                    const float t = iStep * raymarchInfo.numStepsRecip;
                    const float3 currPos = lerp(ray.startPos, ray.endPos, t);
                    
#ifdef CROSS_SECTION_ON
                    if (IsCutout(currPos))
                        continue;
#endif

                    const float density = getDensity(currPos);


                     //Apply visibility window

                    bool isInInterval = false;

                    for (int numberOfSlider = 0; numberOfSlider < _visibilitySlidersCount; numberOfSlider++)
                    {
                        if ((density > _lowerVisibilityWindow[numberOfSlider]) && (density < _upperVisibilityWindow[numberOfSlider]))
                        {
                            isInInterval = true;
                            float3 normal = normalize(getGradient(currPos));
                            col = getTF1DColour(density);
                            col.rgb = calculateLighting(col.rgb, normal, getLightDirection(-ray.direction), -ray.direction, 0.15);
                            col.a = 1.0f;
                            break;
                        }
                    }
                    if (isInInterval)
                        break;
                  
                }

                // Write fragment output
                frag_out output;
                output.colour = col;
#if DEPTHWRITE_ON
                
                const float tDepth = iStep * raymarchInfo.numStepsRecip + (step(col.a, 0.0) * 1000.0); // Write large depth if no hit
                output.depth = localToDepth(lerp(ray.startPos, ray.endPos, tDepth) - float3(0.5f, 0.5f, 0.5f));
#endif
                return output;
            }

            frag_in vert(vert_in v)
            {
                return vert_main(v);
            }

            frag_out frag(frag_in i)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

#if MODE_DVR
                return frag_dvr(i);
#elif MODE_MIP
                return frag_mip(i);
#elif MODE_SURF
                return frag_surf(i);
#endif
            }

            ENDCG
        }
    }
}

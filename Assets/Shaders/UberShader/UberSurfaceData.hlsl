#ifndef UBER_SURFACE_DATA_INCLUDED
    #define UBER_SURFACE_DATA_INCLUDED

    #ifndef EXTRA_SURFACE_DATA
        #define EXTRA_SURFACE_DATA 
    #endif

    struct UberSurfaceData
    {
        half3 albedo;
        half3 specular;
        half metallic;
        half smoothness;
        half3 normalTS;
        half3 emission;
        half occlusion;
        half alpha;
        half clearCoatMask;
        half clearCoatSmoothness;
        
    };

    struct UberExtraData
    {
        half sss_mask;
        half sss_thickness;
        half sss_distortion;
        half sss_power;
        half sss_scale;
        half3 sss_color;
        half3 diffuseTex;
        EXTRA_SURFACE_DATA
    };
    
#endif
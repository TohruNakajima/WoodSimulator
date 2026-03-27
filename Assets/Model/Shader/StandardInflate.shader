Shader "Custom/StandardInflate"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1.0
        _OcclusionMap ("Occlusion", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0,1)) = 1.0
        _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _EmissionMap ("Emission", 2D) = "white" {}

        [Space(20)]
        [Header(Inflation Settings)]
        _InflateAmount ("Inflate Amount", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _OcclusionMap;
        sampler2D _EmissionMap;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
        };

        half _Glossiness;
        half _Metallic;
        half _BumpScale;
        half _OcclusionStrength;
        fixed4 _Color;
        fixed4 _EmissionColor;
        float _InflateAmount;

        void vert (inout appdata_full v)
        {
            // 法線方向に頂点を押し出す
            v.vertex.xyz += v.normal * _InflateAmount;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;

            // Metallic and smoothness
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;

            // Normal map
            o.Normal = UnpackScaleNormal(tex2D(_BumpMap, IN.uv_BumpMap), _BumpScale);

            // Occlusion
            half occ = tex2D(_OcclusionMap, IN.uv_MainTex).g;
            o.Occlusion = LerpOneTo(occ, _OcclusionStrength);

            // Emission
            o.Emission = tex2D(_EmissionMap, IN.uv_MainTex).rgb * _EmissionColor.rgb;

            o.Alpha = c.a;
        }
        ENDCG
    }

    FallBack "Diffuse"
}

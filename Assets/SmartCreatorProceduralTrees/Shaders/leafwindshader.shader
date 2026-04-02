Shader "Custom/LeafUltimateWindURP"
{
    /* ─────────────────────────  MATERIAL PROPERTIES  ───────────────────────── */
    Properties
    {
        _MainTex           ("Leaf Texture",        2D)      = "white" {}
        _BumpMap           ("Normal Map",          2D)      = "bump"  {}
        _BumpScale         ("Normal Scale",        Float)   = 1
        _OcclusionMap      ("Occlusion Map",       2D)      = "white" {}
        _OcclusionStrength ("Occlusion Strength",  Range(0,2)) = 1

        /* Wind */
        _WindAmplitude     ("Wind Amplitude",      Float)   = 0.10
        _WindFrequency     ("Wind Frequency",      Float)   = 1
        _WindSpeed         ("Wind Speed",          Float)   = 1
        _WindRandomize     ("Wind Randomize",      Float)   = 1

        /* Sub‑surface translucency */
        _SubsurfaceColor   ("Subsurface Color",    Color)   = (0.6, 1.0, 0.6, 1)
        _SubsurfacePower   ("Subsurface Power",    Range(0,1)) = 0.35

        /* Lighting trim */
        _LightStrength     ("Light Strength",      Range(0,1)) = 0.4

        /* Visibility & density */
        _LeafVisMult       ("Leaf Vis. Multiplier",Float)   = 1
        _LeafDensity       ("Leaf Alpha Cutoff",   Range(0,1)) = 0.5

        /* Clustering controls */
        _ClusterMode       ("Cluster Mode (0=off,1=on)", Range(0,1)) = 0
        _ClusterMinHeight  ("Cluster Min Y",       Float)         = 0
        _ClusterMaxHeight  ("Cluster Max Y",       Float)         = 5

        /* Used only by LeafMaterialInstancer – shader ignores them */
        _ExtraLeafCopies   ("Extra Leaf Copies",   Int)             = 0
        _LeafSpread        ("Copy Spread (m)",     Range(0,0.25))   = 0.10
        _CopyYawDeg        ("Random Yaw (deg)",    Range(0,30))     = 10
    }

    /* ─────────────────────────  SUB‑SHADER  ─────────────────────────────────── */
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "TransparentCutout"
            "Queue"          = "AlphaTest"
        }

        LOD 200

        Pass
        {
            Name "FORWARD_UNLIT"
            Tags { "LightMode"="UniversalForward" }

            // alpha‐cutout + depth‐write so terrain painter will instance it
            Blend SrcAlpha OneMinusSrcAlpha
            Cull  Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            /* ───── TEXTURES */
            TEXTURE2D(_MainTex);      SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);      SAMPLER(sampler_BumpMap);
            TEXTURE2D(_OcclusionMap); SAMPLER(sampler_OcclusionMap);

            /* ───── MATERIAL CONSTANTS */
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float  _BumpScale;
                float  _OcclusionStrength;
                float  _WindAmplitude, _WindFrequency, _WindSpeed, _WindRandomize;
                float4 _SubsurfaceColor;
                float  _SubsurfacePower;
                float  _LightStrength;
                float  _LeafVisMult;
                float  _LeafDensity;         // alpha cutoff
                float  _ClusterMode;
                float  _ClusterMinHeight;
                float  _ClusterMaxHeight;
                float  _ExtraLeafCopies, _LeafSpread, _CopyYawDeg;
            CBUFFER_END

            /* ───── STRUCTS */
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 tangentWS   : TEXCOORD2;
                float  tangentSign : TEXCOORD3;
                float3 worldPosWS  : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            /* Per‑instance random phase */
            float GetInstancePhase()
            {
                float3 off = GetObjectToWorldMatrix()._m03_m13_m23;
                return frac(sin(dot(off, float3(12.9898,78.233,37.719))) * 43758.5453);
            }

            /* ───── VERTEX ─────────────────────────────────────────────────── */
            Varyings vert (Attributes IN)
            {
                Varyings OUT; UNITY_SETUP_INSTANCE_ID(IN);

                float t = _Time.y * _WindSpeed;
                float phase = (_WindRandomize > 0.5) ? GetInstancePhase() * TWO_PI : 0.0;

                float heightMask = saturate(IN.positionOS.y);
                float swayX = sin(IN.positionOS.x * _WindFrequency + t + phase) * _WindAmplitude;
                float swayZ = sin(IN.positionOS.z * _WindFrequency * 0.65 + t * 0.85 + phase)
                            * _WindAmplitude * 0.4;

                float4 posOS = IN.positionOS;
                posOS.x += swayX * lerp(0.2, 1.0, heightMask);
                posOS.z += swayZ * lerp(0.2, 1.0, heightMask);

                OUT.positionCS  = TransformObjectToHClip(posOS.xyz);
                OUT.worldPosWS  = TransformObjectToWorld(posOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.tangentWS   = TransformObjectToWorldDir(IN.tangentOS.xyz);
                OUT.tangentSign = IN.tangentOS.w;

                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                return OUT;
            }

            /* ───── FRAGMENT ───────────────────────────────────────────────── */
            half4 frag (Varyings IN) : SV_Target
            {
                // cluster fade by world‑height
                float mask = 1;
                if (_ClusterMode > 0.5)
                {
                    float range = max(0.0001, _ClusterMaxHeight - _ClusterMinHeight);
                    mask = saturate((IN.worldPosWS.y - _ClusterMinHeight) / range);
                }

                // stable alpha from mip0 so no pop‑out at distance
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half a    = _MainTex.SampleLevel(sampler_MainTex, IN.uv, 0).a * mask;
                clip(a - _LeafDensity);
                tex.a = a;

                // occlusion
                half ao  = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, IN.uv).r;
                tex.rgb *= lerp(1.0h, ao, _OcclusionStrength);

                // normal mapping
                half3 nTS = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv).xyz * 2 - 1;
                nTS.xy  *= _BumpScale; 
                nTS      = normalize(nTS);
                half3 T  = normalize(IN.tangentWS);
                half3 N  = normalize(IN.normalWS);
                half3 B  = cross(N, T) * IN.tangentSign;
                half3 normalWS = normalize(nTS.x * T + nTS.y * B + nTS.z * N);

                // direct lighting
                Light mainLight = GetMainLight();
                half3 L  = normalize(mainLight.direction);
                half  NdL = saturate(dot(normalWS, L));
                half3 direct = (0.6h + NdL * _LightStrength) * mainLight.color;

                // rim
                half3 V = normalize(_WorldSpaceCameraPos - IN.worldPosWS);
                half  rim = saturate(1 - dot(normalWS, V));
                direct += rim * 0.07h;

                // subsurface
                half subs = saturate(dot(-normalWS, L));
                direct += _SubsurfaceColor.rgb * subs * _SubsurfacePower;

                // ambient (simple constant ambient light)
                half3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz;

                // combine
                tex.rgb = tex.rgb * (direct + ambient) * _LeafVisMult;
                return tex;
            }
            ENDHLSL
        }
    }
}





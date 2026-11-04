Shader "Custom/PointCloudGlow"
{
    Properties
    {
        _ColorA ("Color claro", Color) = (0.55, 0.85, 1, 1)
        _ColorB ("Color profundo", Color) = (0.15, 0.4, 1, 1)
        _Intensity ("Brillo", Float) = 1.2
        _Size ("Tamaño total con halo (m)", Float) = 0.03
        _Wobble ("Amplitud movimiento (m)", Float) = 0.004
        _WobbleSpeed ("Velocidad movimiento", Float) = 1.5
        _DisperseDist ("Distancia dispersión (m)", Float) = 1.5
        _FadeTime ("Duración fade (s)", Float) = 1.5
        _Delay ("Retraso de onda (s/m)", Float) = 1.2
        _HitPos ("Hit Pos", Vector) = (0,0,0,0)
        _HitTime ("Hit Time", Float) = 1000000
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA, _ColorB, _HitPos;
                float _Intensity, _Size, _Wobble, _WobbleSpeed, _DisperseDist, _FadeTime, _Delay, _HitTime;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 corner : TEXCOORD0;
                float2 seed : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 corner : TEXCOORD0;
                float3 col : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 hash3(float2 s)
            {
                float3 p = float3(dot(s, float2(127.1, 311.7)), dot(s, float2(269.5, 183.3)), dot(s, float2(419.2, 371.9)));
                return frac(sin(p) * 43758.5453);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float t = _Time.y;
                float3 baseWS = TransformObjectToWorld(IN.positionOS);

                // Movimiento suave y continuo
                float3 wob = sin(t * _WobbleSpeed * float3(1.0, 1.3, 0.9) + IN.seed.xyx * 40.0 + float3(0, 1.7, 3.1));
                float3 center = baseWS + wob * _Wobble;

                // Dispersión en onda desde el punto de contacto
                float tt = max((t - _HitTime) - distance(baseWS, _HitPos.xyz) * _Delay, 0.0);
                float3 away = normalize(baseWS - _HitPos.xyz + 0.0001);
                float3 dir = normalize((hash3(IN.seed) * 2.0 - 1.0) + away * 0.8);
                center += dir * (1.0 - exp(-tt * 2.0)) * _DisperseDist;
                float life = 1.0 - saturate(tt / _FadeTime);

                // Brillo: color variable por punto, parpadeo suave, fade al acercarse a la cámara
                float twinkle = 0.75 + 0.25 * sin(t * 2.0 + IN.seed.x * 50.0);
                float camDist = distance(center, _WorldSpaceCameraPos);
                float nearFade = saturate((camDist - 0.15) * 5.0);
                OUT.col = lerp(_ColorA.rgb, _ColorB.rgb, IN.seed.x) * (_Intensity * twinkle * life * nearFade);

                // Billboard, el punto se encoge un poco al desvanecerse
                float3 right = UNITY_MATRIX_V[0].xyz;
                float3 up = UNITY_MATRIX_V[1].xyz;
                float size = _Size * 0.5 * (0.5 + 0.5 * life);
                float3 posWS = center + (right * IN.corner.x + up * IN.corner.y) * size;

                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.corner = IN.corner;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float d2 = dot(IN.corner, IN.corner);
                float core = exp(-d2 * 12.0);          // centro intenso
                float halo = exp(-d2 * 3.0) * 0.35;    // resplandor suave
                float edge = saturate((1.0 - d2) * 2.5); // evita corte duro en el borde
                return half4(IN.col * ((core + halo) * edge), 1);
            }
            ENDHLSL
        }
    }
}
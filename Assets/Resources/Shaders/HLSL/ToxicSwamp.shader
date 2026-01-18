Shader "Custom/URP/PoisonSwamp"
{
    Properties
    {
        [Header(Swamp)]
        _MainTex ("늪 베이스 텍스처", 2D) = "white" {}
        _MainTexScale ("텍스처 스케일", Range(0.1, 10)) = 1.0
        _DarkColor ("어두운 영역 색상", Color) = (0.08, 0.15, 0.08, 1)
        _BrightColor ("밝은 영역 색상", Color) = (0.25, 0.6, 0.3, 1)
        _ColorIntensity ("색상 강도", Range(0.5, 2.0)) = 1.0
        _WaveSpeed ("웨이브 속도", Range(0, 2)) = 0.5
        _DistortionStrength ("왜곡 강도", Range(0, 0.2)) = 0.05

        [Header(Bubble)]
        _BubbleCoreColor ("버블 색상", Color) = (0.5, 1.0, 0.6, 1)
        _BubbleIntensity ("버블 강도", Range(0, 2)) = 0.8
        _BubbleSpeed ("버블 속도", Range(0, 3)) = 1.0
        _BubbleScale ("버블 크기", Range(1, 1000)) = 10.0
        _BubbleThreshold ("버블 밀도", Range(0, 1)) = 0.6
        _BubbleFlowDirection ("흐름 방향", Vector) = (1, 0.3, 0, 0)
        _BubbleFlowSpeed ("흐름 속도", Range(0, 1)) = 0.2
        _BubbleSizeVariation ("크기 변화", Range(0, 1)) = 0.5

        [Header(Bubble Lifecycle)]
        _BubbleLifecycleSpeed ("라이프사이클 속도", Range(0.1, 2)) = 0.3
        _BubbleFadeInEnd ("생성 완료 시점", Range(0.1, 0.8)) = 0.5
        _BubbleFadeOutStart ("터지기 시작 시점", Range(0.5, 0.95)) = 0.8

        [Header(Grid)]
        _MaskTex ("청크 마스크 텍스처", 2D) = "white" {}
        _MaskEdgeSoftness ("마스크 경계 부드러움", Range(0, 0.5)) = 0.1
        _ChunkSize ("청크 크기 (미터)", Float) = 10.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "ToxicSwampPass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 maskUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Texture and Sampler declarations
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            // Property declarations
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MaskTex_ST;
                float4 _DarkColor;
                float4 _BrightColor;
                float4 _BubbleCoreColor;
                float4 _BubbleFlowDirection;
                float _MainTexScale;
                float _ColorIntensity;
                float _BubbleIntensity;
                float _WaveSpeed;
                float _BubbleSpeed;
                float _BubbleFlowSpeed;
                float _DistortionStrength;
                float _BubbleScale;
                float _BubbleThreshold;
                float _BubbleSizeVariation;
                float _BubbleLifecycleSpeed;
                float _BubbleFadeInEnd;
                float _BubbleFadeOutStart;
                float _ChunkSize;
                float _MaskEdgeSoftness;
            CBUFFER_END

            // 해시 함수 - 셀별 고유 랜덤 값 생성
            float Hash(float2 p)
            {
                float ret = frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
                return ret;
            }

            // 개선된 절차적 노이즈 함수 - 유기적 버블 생성용
            float OrganicBubbleNoise(float2 uv, float time, out float bubbleSize, out float bubbleSpeed)
            {
                float2 gridUV = uv * _BubbleScale;
                float2 gridID = floor(gridUV);
                float2 gridFrac = frac(gridUV);

                float minDist = 1.0;
                bubbleSize = 1.0;
                bubbleSpeed = 1.0;

                // 3x3 이웃 셀 검사
                for(int y = -1; y <= 1; y++)
                {
                    for(int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(x, y);
                        float2 cellID = gridID + neighbor;

                        // 셀별 고유 랜덤 시드
                        float cellSeed = Hash(cellID);

                        // 각 버블마다 다른 속도 (0.5 ~ 1.5 범위)
                        float localSpeed = 0.5 + cellSeed * 1.0;
                        float localTime = time * _BubbleSpeed * localSpeed;

                        // 각 버블마다 다른 크기 (크기 변화 적용)
                        float localSize = 1.0 - _BubbleSizeVariation + (cellSeed * _BubbleSizeVariation * 2.0);

                        // 흐름 방향으로 움직이는 오프셋 (스크롤링)
                        float2 flowOffset = _BubbleFlowDirection.xy * localTime * _BubbleFlowSpeed;

                        // 원운동 제거, 단순 흔들림만 추가
                        float2 wiggle = float2(
                            sin(cellSeed * 6.28 + localTime * 0.5) * 0.1,
                            cos(cellSeed * 3.14 + localTime * 0.7) * 0.1
                        );

                        // 최종 버블 위치 (흐름 + 흔들림)
                        float2 randomOffset = frac(float2(
                            sin(cellID.x * 12.9898 + cellID.y * 78.233) * 0.5 + 0.5,
                            cos(cellID.x * 43.758 + cellID.y * 93.989) * 0.5 + 0.5
                        ) + flowOffset + wiggle);

                        float2 pointPos = neighbor + randomOffset;
                        float2 diff = gridFrac - pointPos;
                        float dist = length(diff) / localSize;

                        if (dist < minDist)
                        {
                            minDist = dist;
                            bubbleSize = localSize;
                            bubbleSpeed = localSpeed;
                        }
                    }
                }

                return minDist;
            }

            // UV 왜곡 함수
            float2 DistortUV(float2 uv, float time)
            {
                float2 distortion = float2(
                    sin(uv.y * 5.0 + time * _WaveSpeed) * _DistortionStrength,
                    cos(uv.x * 5.0 + time * _WaveSpeed * 0.7) * _DistortionStrength
                );

                return uv + distortion;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                // 월드 공간으로 변환
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;

                // 메인 텍스처 UV (타일링/오프셋 적용)
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                // 마스크 UV (그리드 정렬 보장 - 타일링/오프셋 적용)
                output.maskUV = TRANSFORM_TEX(input.uv, _MaskTex);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float time = _Time.y;

                // === 1. 마스크 샘플링 및 가시성 체크 ===
                float maskValue = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.maskUV).r;

                // 완전히 비활성화된 영역은 픽셀 폐기
                clip(maskValue - 0.001);

                // 부드러운 경계 처리
                float maskAlpha = smoothstep(0.0, _MaskEdgeSoftness, maskValue);

                // === 2. UV 왜곡 및 이중 레이어 스크롤링 ===
                float2 scaledUV = input.uv * _MainTexScale;
                float2 distortedUV1 = DistortUV(scaledUV + float2(time * _WaveSpeed * 0.05, time * _WaveSpeed * 0.03), time);
                float2 distortedUV2 = DistortUV(scaledUV - float2(time * _WaveSpeed * 0.03, time * _WaveSpeed * 0.07), time * 1.3);

                // 베이스 텍스처 두 번 샘플링 후 블렌딩
                half4 baseTex1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV1);
                half4 baseTex2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV2);

                // 두 레이어의 그레이스케일 값 평균 계산
                float blendedGrayscale = lerp(baseTex1.r, baseTex2.r, 0.5);

                // 텍스처의 밝기 값을 기반으로 두 색상 사이 보간
                half3 baseColorRGB = lerp(_DarkColor.rgb, _BrightColor.rgb, blendedGrayscale);
                baseColorRGB *= _ColorIntensity;
                half4 baseColor = half4(baseColorRGB, 1.0);

                // === 3. 개선된 유기적 버블 생성 ===
                float bubbleSize, bubbleSpeed;
                float bubbleNoise = OrganicBubbleNoise(input.uv, time, bubbleSize, bubbleSpeed);

                // 버블 라이프사이클 (생성 -> 유지 -> 터짐)
                float lifecycle = frac(time * _BubbleSpeed * bubbleSpeed * _BubbleLifecycleSpeed);

                // 페이드인: 천천히 생성 (0.0 ~ _BubbleFadeInEnd)
                float fadeIn = smoothstep(0.0, _BubbleFadeInEnd, lifecycle);

                // 페이드아웃: 빠르게 터짐 (_BubbleFadeOutStart ~ 1.0)
                float fadeOut = 1.0 - smoothstep(_BubbleFadeOutStart, 1.0, lifecycle);

                // 최종 알파: 생성과 소멸 곱셈
                float bubbleAlpha = fadeIn * fadeOut;

                // 크기 기반 임계값 조정 (작은 버블은 더 자주 나타남)
                float sizeAdjustedThreshold = _BubbleThreshold * bubbleSize;

                // 부드러운 버블 경계
                float bubbleMask = smoothstep(sizeAdjustedThreshold, sizeAdjustedThreshold - 0.1, bubbleNoise);
                bubbleMask *= bubbleAlpha;

                // === 4. 최종 색상 합성 ===
                half4 finalColor = baseColor;

                // 버블 중심부만 매우 약하게 적용
                finalColor.rgb = lerp(finalColor.rgb, _BubbleCoreColor.rgb, bubbleMask * _BubbleIntensity * 0.15);

                // 마스크 알파 적용
                finalColor.a *= maskAlpha;

                return finalColor;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

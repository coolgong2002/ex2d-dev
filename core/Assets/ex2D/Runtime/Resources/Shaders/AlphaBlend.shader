// ======================================================================================
// File         : AlphaBlend.shader
// Author       : Wu Jie, Fredrik Ludvigsen
// Last Change  : 08/28/2013 | 11:41:33 PM | Wednesday,August
// Description  : 
// ======================================================================================

///////////////////////////////////////////////////////////////////////////////
//
///////////////////////////////////////////////////////////////////////////////

Shader "ex2D/Alpha Blended" {
    Properties {
        _MainTex ("Atlas Texture", 2D) = "white" {}
    }

    Category {
        Tags {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Fog { Mode Off }
        Blend SrcAlpha OneMinusSrcAlpha

        SubShader {
            BindChannels {
                Bind "Color", color
                Bind "Vertex", vertex
                Bind "TexCoord", texcoord
            }

            Pass {
                SetTexture [_MainTex] {
                    combine texture * primary
                }
            }
        }

        SubShader {
            pass {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "UnityCG.cginc"

                uniform sampler2D _MainTex;
                uniform float4 _MainTex_ST;

                struct appdata_t {
                    float4 vertex   : POSITION;
                    fixed4 color    : COLOR;
                    float2 texcoord : TEXCOORD0;
                };

                struct v2f {
                    float4 pos        : SV_POSITION;
                    fixed4 color      : COLOR;
                    float2 uv0        : TEXCOORD0;
                };

                v2f vert ( appdata_t _in ) {
                    v2f o;
                    o.pos = mul (UNITY_MATRIX_MVP, _in.vertex);
                    // Texture offset - GOOD
                    o.uv0 = TRANSFORM_TEX(_in.texcoord, _MainTex);
                    o.color = _in.color;
                    return o;
                }

                fixed4 frag ( v2f _in ) : COLOR {
                    fixed4 main = tex2D(_MainTex, _in.uv0);
                    return fixed4(main * _in.color);
                }
                ENDCG
            }
        }
    }
}

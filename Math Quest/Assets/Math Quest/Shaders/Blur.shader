// Smooth blur for a texture, with one slider (_Amount, 0 to 1) that fades between the
// sharp image and the blurred, tinted one. The RawImage's Color is an overlay on the
// blurred image (black at 50% alpha darkens it by half), and it fades in with the blur.
// Meant for a RawImage that shows a (downsampled)
// snapshot of the battle behind the answer panel (see BlurBackdropController).
Shader "MathQuest/UI/Blur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Amount ("Amount (0 sharp, 1 fully blurred)", Range(0, 1)) = 0
        _Radius ("Blur radius at Amount 1 (texture pixels)", Float) = 10
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define SAMPLES 32

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _Amount;
            float _Radius;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 texel = _MainTex_TexelSize.xy;
                float radius = _Amount * _Radius;

                // Samples spread over a disc (golden-angle spiral), centre-weighted.
                fixed4 sum = tex2D(_MainTex, i.uv);
                float total = 1.0;

                for (int k = 0; k < SAMPLES; k++)
                {
                    float r = sqrt((k + 0.5) / SAMPLES);
                    float a = k * 2.39996323;
                    float2 offset = float2(cos(a), sin(a)) * r * radius * texel;
                    float w = exp(-2.0 * r * r);
                    sum += tex2D(_MainTex, i.uv + offset) * w;
                    total += w;
                }

                fixed4 c = sum / total;

                // The image colour is an overlay, not a multiplier: its alpha says how much
                // of it covers the blurred image once the effect is fully faded in.
                c.rgb = lerp(c.rgb, i.color.rgb, i.color.a * _Amount);
                c.a = 1.0;
                return c;
            }
            ENDCG
        }
    }
}

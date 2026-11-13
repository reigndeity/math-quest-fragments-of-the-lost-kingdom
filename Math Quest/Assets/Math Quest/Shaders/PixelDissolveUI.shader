// A dissolve for UI Images (the characters are Images on the Canvas). The sprite breaks up in
// blocks that follow its own pixel grid, so it stays pixel art while it disappears, with a
// glowing band along the edge. Dissolve 0 = whole, 1 = gone.
Shader "MathQuest/UI/PixelDissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Dissolve)]
        _Dissolve ("Dissolve (0 whole, 1 gone)", Range(0, 1)) = 0
        _PixelSize ("Block Size (sprite pixels)", Float) = 1
        _HeightBias ("Top First (0 random, 1 a clean sweep)", Range(0, 1)) = 0.35
        _Seed ("Pattern Seed", Float) = 0

        [Header(Edge)]
        _EdgeWidth ("Edge Width", Range(0, 0.3)) = 0.08
        _EdgeInner ("Edge Color (inside)", Color) = (1, 0.35, 0.1, 1)
        _EdgeOuter ("Edge Color (outside)", Color) = (1, 0.95, 0.5, 1)

        // Standard UI settings, so masks and clipping keep working.
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            float _Dissolve;
            float _PixelSize;
            float _HeightBias;
            float _Seed;
            float _EdgeWidth;
            fixed4 _EdgeInner;
            fixed4 _EdgeOuter;

            v2f vert (appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            // A random number from 0 to 1 for each block, the same every frame.
            float Hash (float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                // Which block of sprite pixels this pixel belongs to.
                float2 texels = IN.texcoord * _MainTex_TexelSize.zw;
                float2 block = floor(texels / max(_PixelSize, 1.0));

                // When this block goes: random, leaning towards the top going first.
                float random = Hash(block + _Seed);
                float topFirst = 1.0 - frac(IN.texcoord.y); // 0 at the top of the texture
                float when = lerp(random, topFirst, _HeightBias);

                // Grows a little past 1 so that Dissolve 1 really removes every block.
                float front = _Dissolve * (1.0 + _EdgeWidth);

                if (when < front - _EdgeWidth)
                {
                    color.a = 0; // already gone
                }
                else if (when < front)
                {
                    // On the burning edge: inside colour near the gone part, outside colour further out.
                    float t = (when - (front - _EdgeWidth)) / max(_EdgeWidth, 0.0001);
                    color.rgb = lerp(_EdgeInner.rgb, _EdgeOuter.rgb, t);
                }

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}

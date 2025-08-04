Shader "Custom/VideoInvert"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _InvertAmount ("Invert Amount", Range(0, 1)) = 1.0
        _Brightness ("Brightness", Range(0, 2)) = 1.0
        _Contrast ("Contrast", Range(0, 2)) = 1.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Overlay"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _InvertAmount;
            float _Brightness;
            float _Contrast;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // テクスチャから色を取得
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // 色調反転
                fixed3 invertedColor = 1.0 - col.rgb;
                
                // 反転量に基づいて元の色と反転色をブレンド
                col.rgb = lerp(col.rgb, invertedColor, _InvertAmount);
                
                // 明度調整
                col.rgb *= _Brightness;
                
                // コントラスト調整
                col.rgb = (col.rgb - 0.5) * _Contrast + 0.5;
                
                // 色をクランプ
                col.rgb = saturate(col.rgb);
                
                return col;
            }
            ENDCG
        }
    }
}

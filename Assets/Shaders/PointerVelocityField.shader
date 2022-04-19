Shader "CustomRenderTexture/PointerVelocityField"
{
    Properties
    {
        _PointerLocalTexturePosition ("Pointer Local Texture Position", Vector) = (1,1,1,1)
        _PointerVelocity ("Pointer Velocity", Vector) = (1,1,1,1)
        _PointerRadius ("Pointer Radius", Float) = 1
    }

     SubShader
     {
        Lighting Off
        Blend One Zero

        Pass
        {
            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            float2 _PointerLocalTexturePosition;
            float3 _PointerVelocity;
            float _PointerRadius;
            
            float circularOut(float t) {
              return sqrt((2.0 - t) * t);
            }
            
            float4 frag(v2f_customrendertexture IN) : COLOR
            {
                float3 color = tex2D(_SelfTexture2D, IN.localTexcoord.xy).rgb;
                
                color.xy = lerp(color, float2(0.5, 0.5), 0.05);
                
                float dist = distance(_PointerLocalTexturePosition, IN.localTexcoord.xy);
                
                if (dist <= _PointerRadius)
                {
                    float3 velocity = lerp(_PointerVelocity, color, circularOut(dist / _PointerRadius));
                    color = lerp(color, velocity, 0.5);
                }
                
                return float4(color.rgb, 1);
            }
            ENDCG
            }
    }
}
Shader "Leap Motion/Graphic Renderer/Defaults/DynamicText" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader {
		Tags {"Queue"="Transparent+1" "RenderType"="Opaque" }
		LOD 100

    ZWrite Off
    ZTest On
    Offset -1, -90000 //text should appear in front of things that it is aligned with
    Blend SrcAlpha OneMinusSrcAlpha

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

      #pragma shader_feature _ GRAPHIC_RENDERER_CYLINDRICAL GRAPHIC_RENDERER_SPHERICAL
      #pragma shader_feature _ GRAPHIC_RENDERER_VERTEX_COLORS
      #define GRAPHIC_RENDERER_VERTEX_UV_0
      #define GRAPHIC_ID_FROM_UV0
      #include "Assets/LeapMotionModules/GraphicRenderer/Resources/DynamicRenderer.cginc"
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			
      v2f_graphic_dynamic vert(appdata_graphic_dynamic v) {
        return ApplyDynamicGraphics(v);
      }
			
			fixed4 frag (v2f_graphic_dynamic i) : SV_Target {
				fixed4 col = tex2D(_MainTex, i.uv0);

#ifdef GRAPHIC_RENDERER_VERTEX_COLORS
        return fixed4(i.color.rgb, col.a * i.color.a);
#else
        return fixed4(0, 0, 0, col.a);
#endif
			}
			ENDCG
		}
	}
}

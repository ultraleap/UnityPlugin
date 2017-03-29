Shader "Unlit/DynamicText" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader {
		Tags {"Queue"="Transparent+1" "RenderType"="Opaque" }
		LOD 100

    ZWrite Off
    ZTest On
    Blend SrcAlpha OneMinusSrcAlpha

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

      #pragma shader_feature _ LEAP_GUI_CYLINDRICAL LEAP_GUI_SPHERICAL
      #pragma shader_feature _ LEAP_GUI_VERTEX_COLORS
      #define LEAP_GUI_VERTEX_UV_0
      #define GUI_ELEMENT_ID_FROM_UV0
      #include "Assets/LeapMotionModules/ElementRenderer/Resources/DynamicRenderer.cginc"
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			
      v2f_gui_dynamic vert(appdata_gui_dynamic v) {
        return ApplyDynamicGui(v);
      }
			
			fixed4 frag (v2f_gui_dynamic i) : SV_Target {
				fixed4 col = tex2D(_MainTex, i.uv0);

#ifdef LEAP_GUI_VERTEX_COLORS
        return fixed4(i.color.rgb, col.a * i.color.a);
#else
        return fixed4(0, 0, 0, col.a);
#endif
			}
			ENDCG
		}
	}
}

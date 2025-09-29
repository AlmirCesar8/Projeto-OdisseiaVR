// Shader simples para exibir uma textura panorâmica 360 no interior de uma esfera.
// Não é afetado por iluminação (Unlit) e renderiza os dois lados da malha (Cull Off).
Shader "IniciacaoCientifica/PanoramaUnlitShader"
{
    // Propriedades que aparecerão no Inspector do Material na Unity.
    Properties
    {
        _MainTex ("Textura Panorâmica (RGB)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            // A linha mais importante: "Cull Off".
            // O padrão é "Cull Back", que não renderiza as faces internas de um objeto.
            // "Cull Off" diz à GPU para renderizar AMBOS os lados, resolvendo o problema da esfera invertida.
            Cull Off

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Simplesmente retorna a cor da textura na coordenada UV correspondente.
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
    // Fallback para shaders mais antigos, caso este não seja suportado.
    FallBack "Mobile/Unlit"
}
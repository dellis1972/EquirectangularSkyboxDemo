//-----------------------------------------------------------------------------
// EquirectangularSkybox.fx
//
// Renders an equirectangular panorama texture as a skybox.
// Camera is always treated as being at the origin — translation is stripped
// before this shader is called (handled in C#).
//
// Designed for MonoGame using the same macro conventions as built-in effects.
//-----------------------------------------------------------------------------

#include "Macros.fxh"

DECLARE_TEXTURE(SkyMap, 0)

BEGIN_CONSTANTS

    float4x4 RotationProjection  _vs(c0)  _cb(c0);

END_CONSTANTS

// ---------- Structures ----------

struct VSInput
{
    float4 Position : POSITION0;
};

struct VSOutput
{
    float4 Position : SV_POSITION;
    float3 ViewDir  : TEXCOORD0;
};

// ---------- Vertex Shader ----------

VSOutput MainVS(VSInput input)
{
    VSOutput output;

    // Transform by rotation+projection only (no translation).
    output.Position = mul(input.Position, RotationProjection);

    // Push to far plane: set z = w so the depth value becomes 1.0
    output.Position.z = output.Position.w;

    // The view direction is the unit-sphere vertex position itself.
    output.ViewDir = normalize(input.Position.xyz);

    return output;
}

// ---------- Pixel Shader ----------

float4 MainPS(VSOutput input) : SV_Target0
{
    float3 dir = normalize(input.ViewDir);

    // Convert 3D direction to equirectangular UV:
    //   u = atan2(dir.z, dir.x) / (2*PI) + 0.5   (longitude, wraps around)
    //   v = acos(dir.y) / PI                      (latitude, maps angle linearly)
    float u = atan2(dir.z, dir.x) / (2.0 * 3.14159265358979) + 0.5;
    float v = acos(dir.y) / 3.14159265358979;

    return SAMPLE_TEXTURE(SkyMap, float2(u, v));
}

// ---------- Technique ----------

TECHNIQUE(EquirectangularSkybox, MainVS, MainPS);

#version 330 core

in vec2 texCoords;

out vec4 FragColor;

uniform sampler2D screenTexture;
uniform sampler2D depthTexture;

uniform vec3 cameraPos;
uniform float yaw;
uniform float pitch;
uniform vec2 resolution;

uniform vec3 sunPos;
vec3 dirToSun = vec3(0, 0, 0);

float near = .1;
float far = 100.;

uniform vec3 planetCenter;
float planetRadius = 8;
float atmosphereRadius = 12.;
float densityFalloff = 4;

uniform vec3 scatteringCoefficients;

#define FLT_MAX 3.402823466e+38
vec2 raySphereIntersection (vec3 sphereCenter, float sphereRadius, vec3 rayOrigin, vec3 rayDirection) {

    vec3 offset = rayOrigin - sphereCenter;
    float a = 1; // dot(rayDirection, rayDirection)
    float b = 2 * dot(offset, rayDirection);
    float c = dot(offset, offset) - sphereRadius * sphereRadius;
    float d = b * b - 4 * a * c;

    if (d > 0) {
        float s = sqrt(d);
        float dstToSphereNear = max(0, (-b - s) / (2 * a));
        float dstToSphereFar = (-b + s) / (2 * a);

        if (dstToSphereFar >= 0) {
            return vec2(dstToSphereNear, dstToSphereFar - dstToSphereNear);
        }
    }

    return vec2(FLT_MAX, 0);
}

mat3 getYawMatrix() {
    return mat3(
        cos(yaw), 0, sin(yaw),
        0, 1, 0,
        -sin(yaw), 0, cos(yaw)
    );
}

mat3 getPitchMatrix() {
    return mat3(
        1, 0, 0,
        0, cos(pitch), -sin(pitch),
        0, sin(pitch), cos(pitch)
    );
}

float sdfSphere (vec3 point, vec3 position, float radius)
{
    return length(point - position) - radius;
}

float getDist (vec3 point)
{
    vec3 sphereCenter = vec3(0., 0., 0.);
    float sphereDist = sdfSphere(point, sphereCenter, 1.);
    
    return max(0, sphereDist);
}

#define MAX_STEPS 100
#define MAX_DIST 100.
#define SURF_DIST .01
float rayMarch(vec3 rayOrigin, vec3 rayDirection) {
    float dst = 0.;

    for (int i = 0; i < MAX_STEPS; i++) {
        vec3 p = rayOrigin + rayDirection * dst;
        float dst_i = getDist(p);
        dst += dst_i;
        if (dst < SURF_DIST || dst > MAX_DIST) break;
    }
    
    return dst;
}

float linearizeDepth (float depth)
{
    return (2. * near * far) / (far + near - (depth * 2. - 1.) * (far - near));
}

float densityAtPoint(vec3 densitySamplePoint) {
    float heightAboveSurface = length(densitySamplePoint - planetCenter) - planetRadius;
    float height01 = heightAboveSurface / (atmosphereRadius - planetRadius);
    float localDensity = exp(-height01 * densityFalloff) * (1 - height01);
    return localDensity;
}

#define NUM_OD_POINTS 20
float opticalDepth(vec3 rayOrigin, vec3 rayDirection, float rayLength) {
    vec3 densitySamplePoint = rayOrigin;
    float stepSize = rayLength / (NUM_OD_POINTS - 1);
    float opticalDepth = 0;

    for (int i = 0; i < NUM_OD_POINTS; i++) {
        float localDensity = densityAtPoint(densitySamplePoint);

        opticalDepth += localDensity * stepSize;
        densitySamplePoint += rayDirection * stepSize;
    }

    return opticalDepth;
}

#define NUM_POINTS 30
vec3 calculateLight(vec3 rayOrigin, vec3 rayDirection, float rayLength, vec3 originalCol)
{
    vec3 inScatterPoint = rayOrigin;
    float stepSize = rayLength / (NUM_POINTS - 1);
    vec3 inScatteredLight = vec3(0, 0, 0);
    float viewRayOpticalDepth = 0.;

    for (int i = 0; i < NUM_POINTS; i++) {
        vec3 currentDirToSun = normalize(sunPos - inScatterPoint);
        float sunRayLength = raySphereIntersection(planetCenter, atmosphereRadius, inScatterPoint, currentDirToSun).y;
        float sunRayOpticalDepth = opticalDepth(inScatterPoint, currentDirToSun, sunRayLength);
        viewRayOpticalDepth = opticalDepth(inScatterPoint, -rayDirection, stepSize * i);
        vec3 transmittance = exp(-(sunRayOpticalDepth + viewRayOpticalDepth) * scatteringCoefficients); // How much light will reach the inscatter point
        float localDensity = densityAtPoint(inScatterPoint);

        inScatteredLight += localDensity * transmittance * scatteringCoefficients * stepSize;
        inScatterPoint += rayDirection * stepSize;
    }

    float originalColTransmittance = exp(-viewRayOpticalDepth);
    return (originalCol * originalColTransmittance) + inScatteredLight;
}

#define EPSILON 0.00001
void main()
{
    vec4 originalColor = texture(screenTexture, texCoords.st);
    vec4 depth = texture(depthTexture, texCoords.st);

    vec2 uv = (texCoords - .5);
    uv.x *= resolution.x / resolution.y;

    // Ray origin and direction calculation
    vec3 rayOrigin = vec3(cameraPos.x, cameraPos.y, cameraPos.z);
    vec3 rayDirection = normalize(vec3(uv.x, uv.y, -1.));
    rayDirection = rayDirection * getPitchMatrix();
    rayDirection = rayDirection * getYawMatrix();

    dirToSun = normalize(sunPos - rayOrigin);

    // With raytracing concepts

    vec2 hitInfo = raySphereIntersection(planetCenter, atmosphereRadius, rayOrigin, rayDirection);
    float dstToAtmosphere = hitInfo.x;
    float dstThroughAtmosphere = hitInfo.y;

    vec3 rtCol = vec3(dstThroughAtmosphere / (atmosphereRadius * 2.));

    // With raymarching concept
    // float lambda = rayMarch(rayOrigin, rayDirection);
    // lambda /= 6.;
    // vec3 rmCol = vec3(1. - lambda);

    // FragColor = max(originalColor, vec4(rtCol, 1.));
    // FragColor = vec4(dirToSun, 1.);

    if (dstThroughAtmosphere > 0) {
        vec3 pointInAtmosphere = rayOrigin + rayDirection * dstToAtmosphere;
        vec3 light = calculateLight(pointInAtmosphere, rayDirection, dstThroughAtmosphere - EPSILON * 2, originalColor.xyz);
        // FragColor = originalColor * (1 - light) + light;
        FragColor = vec4(light, 0);
        return;
    }

    FragColor = originalColor;
}
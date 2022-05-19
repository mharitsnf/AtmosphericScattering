#version 330 core

in vec2 texCoords;

out vec4 FragColor;

uniform sampler2D screenTexture;
uniform vec3 cameraPos;
uniform vec3 cameraFront;
uniform float yaw;
uniform float pitch;
uniform vec2 resolution;
uniform mat4 view;

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

// Current problems:
// 1. if using viewDir as the rayDirection, it affects the whole screen instead of each pixel and
// and moving the camera with w and s doesn't affect the sphere
// 2. when using texCoords / uv coordinates, sphere moves but in a weird fashion
void main()
{
    vec4 originalColor = texture(screenTexture, texCoords.st);

    vec2 uv = (texCoords - .5);
    uv.x *= resolution.x / resolution.y;

    // With raytracing concepts
    vec3 rayOrigin = vec3(cameraPos.x, cameraPos.y, cameraPos.z);
    vec3 rayDirection = normalize(vec3(uv.x, uv.y, -1.));
    rayDirection = rayDirection * getPitchMatrix();
    rayDirection = rayDirection * getYawMatrix();

    vec3 sphereCenter = vec3(5., 0., 0.);
    float sphereRadius = 5.;

    vec2 hitInfo = raySphereIntersection(sphereCenter, sphereRadius, rayOrigin, rayDirection);
    float dstToAtmosphere = hitInfo.x;
    float dstThroughAtmosphere = hitInfo.y;

    vec3 rtCol = vec3(dstThroughAtmosphere / (sphereRadius * 2.));

    // Working raymarching code
    // vec3 rayOrigin = vec3(cameraPos.x, cameraPos.y, cameraPos.z);
    // vec3 rayDirection = normalize(vec3(uv.x, uv.y, -1.));
    // rayDirection = rayDirection * getPitchMatrix();
    // rayDirection = rayDirection * getYawMatrix();

    // float lambda = rayMarch(rayOrigin, rayDirection);
    // lambda /= 6.;
    // vec3 rmCol = vec3(1. - lambda);

    FragColor = originalColor;
    // FragColor = vec4(rmCol, 1.);
    FragColor = vec4(max(originalColor.xyz, rtCol), 1.);
}
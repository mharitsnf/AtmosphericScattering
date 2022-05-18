#version 330 core

in vec2 texCoords;

out vec4 FragColor;

uniform sampler2D screenTexture;
uniform vec3 cameraPos;
uniform vec3 viewDir;

#define FLT_MAX 3.402823466e+38
vec2 raySphereIntersection (vec3 sphereCenter, float sphereRadius, vec3 rayOrigin, vec3 rayDirection) {
    vec3 offset = rayOrigin - sphereCenter;
    float a = dot(rayDirection, rayDirection);
    float b = 2 * dot(offset, rayDirection);
    float c = dot(offset, offset) - sphereRadius * sphereRadius;
    float d = b * b - 4 * a * c;

    if (d > 0) {
        float s = sqrt(d);
        float dstToSphereNear = max(0, (-b - s) / (2 * a));
        float dstToSphereFar = (-b + s) / (2 * a);

        if (dstToSphereFar >= 0) {
            return vec2 (dstToSphereNear, dstToSphereFar - dstToSphereNear);
        }
    }

    return vec2(FLT_MAX, 0);
}

void main()
{
    vec4 originalColor = texture(screenTexture, texCoords.st);
    vec3 rayOrigin = cameraPos;
    vec3 rayDirection = normalize(vec3(texCoords.x * viewDir.x, texCoords.y * viewDir.y, viewDir.z));
    vec3 sphereCenter = vec3(0., 0., 0.);
    float sphereRadius = 1.;

    vec2 hitInfo = raySphereIntersection(sphereCenter, sphereRadius, rayOrigin, rayDirection);
    float dstToAtmosphere = hitInfo.x;
    float dstThroughAtmosphere = hitInfo.y;

    FragColor = max(originalColor, vec4(vec3(dstThroughAtmosphere / (sphereRadius * 2)), 1.));
}
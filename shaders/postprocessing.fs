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

// atmosphere is most dense close to the surface
// exponentially less dense as higher as we go
float densityAtPoint(vec3 densitySamplePoint) {
    // height aboce the surface
    float heightAboveSurface = length(densitySamplePoint - planetCenter) - planetRadius;
    // we scale the density for zero at the surface and 1 at the outer shell of the atmosphere
    float height01 = heightAboveSurface / (atmosphereRadius - planetRadius);
    // density Fall of for controlling the exponential shape of the curve
    // (1 - the scaled height) to avoid the curve going to 0 beyong the outer shell
    float localDensity = exp(-height01 * densityFalloff) * (1 - height01);

    // returning result
    return localDensity;
}

#define NUM_OD_POINTS 20
// calculating the average atmospheric density along a ray
// we break the ray from the sun down into a number of points 
// "sampling" the density at each point
// then adding them together multiplying by the step size (that is the distance between the points)
float opticalDepth(vec3 rayOrigin, vec3 rayDirection, float rayLength) {
    // sample point start at rayOrigin
    vec3 densitySamplePoint = rayOrigin;
    // calc step size
    float stepSize = rayLength / (NUM_OD_POINTS - 1);
    // density to zero
    float opticalDepth = 0;

    for (int i = 0; i < NUM_OD_POINTS; i++) {
        // calc density at curretn sample point
        float localDensity = densityAtPoint(densitySamplePoint);
        
        // add that to density multiplied by step size
        opticalDepth += localDensity * stepSize;
        // move sample point along ray
        densitySamplePoint += rayDirection * stepSize;
    }

    //return result
    return opticalDepth;
}

#define NUM_POINTS 30

// Following function is to describe the view ray of the camera through the atmosphere for the current pixel
vec3 calculateLight(vec3 rayOrigin, vec3 rayDirection, float rayLength, vec3 originalCol)
{
    // lights travels from the sun and get scattered in the path of the view ray (referred to as "in-scattering")
    // set first inScatter point to rayOrigin
    vec3 inScatterPoint = rayOrigin;
    // calculate distance between points
    float stepSize = rayLength / (NUM_POINTS - 1);
    // total amount lights scattered in across all those points
    vec3 inScatteredLight = vec3(0, 0, 0);
    float viewRayOpticalDepth = 0.;

    // move the points along the view ray by "step size", which is the distance between the points
    for (int i = 0; i < NUM_POINTS; i++) {
        vec3 currentDirToSun = normalize(sunPos - inScatterPoint);
        // length of sun ray
        float sunRayLength = raySphereIntersection(planetCenter, atmosphereRadius, inScatterPoint, currentDirToSun).y;
        // depth of the atmosphere (density along a ray)
        float sunRayOpticalDepth = opticalDepth(inScatterPoint, currentDirToSun, sunRayLength);
        // as the light travels to the camera, some light will be scattered away too
        viewRayOpticalDepth = opticalDepth(inScatterPoint, -rayDirection, stepSize * i);
        // How much light will reach the inscatter point
            // the following expression show in a clearer way what happens:
            // the transmittance gets updated by the amount of light that scatters from inScatter point to the camera, i.e., exp(-viewRayOpticalDepth)
            // float transmittance = exp(-sunRayOpticalDepth) * exp(-viewRayOpticalDepth)
            // then, we simplify the equation:
        vec3 transmittance = exp(-(sunRayOpticalDepth + viewRayOpticalDepth) * scatteringCoefficients);
        // density at a single point
        // the greater the density, the more light will be scattered
        float localDensity = densityAtPoint(inScatterPoint);
        // increase the inScattered light by localDensity * transmittance * stepSize
        inScatteredLight += localDensity * transmittance * scatteringCoefficients * stepSize;
        inScatterPoint += rayDirection * stepSize;
    }
    // amount of light that makes it to the camera
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


    // check if we are looking through the atmosphere
    if (dstThroughAtmosphere > 0) {
        // if so, find the first point along the view ray that is inside the atmosphere
        vec3 pointInAtmosphere = rayOrigin + rayDirection * dstToAtmosphere;
        // call calcLight; pass in that point with ray direction and distance through atmosphere
        vec3 light = calculateLight(pointInAtmosphere, rayDirection, dstThroughAtmosphere - EPSILON * 2, originalColor.xyz);
        // output original color blendet with the light
        FragColor = vec4(light, 0);
        return;
    }


    // if we are not looking through the atmosphere, then show the original Color
    FragColor = originalColor;
}
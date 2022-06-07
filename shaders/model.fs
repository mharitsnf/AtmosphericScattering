#version 330 core
out vec4 FragColor;

in vec2 TexCoords;
in vec3 Normal;  
in vec3 FragPos;  
uniform vec3 lightPos; 
uniform vec3 lightColor;
uniform sampler2D texture_diffuse1;
void main()
{    
    // ambient
    float ambientStrength = 0.1;
    vec3 ambient = ambientStrength * lightColor;
  	
    // diffuse 
    vec3 norm = normalize(Normal);
    vec3 lightDir = normalize(lightPos - FragPos);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * lightColor;
            
    //vec3 result = (ambient + diffuse) * texture(texture_diffuse1, TexCoords);
    vec3 result = (ambient + diffuse) *vec3(texture(texture_diffuse1, TexCoords).x,texture(texture_diffuse1, TexCoords).y,texture(texture_diffuse1, TexCoords).z);
    FragColor = vec4(result, 1.0);
    //FragColor = texture(texture_diffuse1, TexCoords);

}
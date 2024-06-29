#shader vertex
#version 330 core
layout (location=0) in vec3 aPos;
layout (location=1) in vec3 aColor;

out vec3 fColor;

uniform mat4 projectionMatrix;
uniform mat4 viewMatrix;

void main()
{
    fColor = aColor;

    gl_Position = projectionMatrix * viewMatrix * vec4(aPos, 1.0);
}

#shader fragment
#version 330 core

in vec3 fColor;

out vec4 color;

void main()
{
    color = vec4(fColor, 1);
}
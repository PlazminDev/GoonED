#shader vertex
#version 330 core
layout (location=0) in vec3 position;

uniform mat4 projectionMatrix;
uniform mat4 viewMatrix;

void main()
{
    gl_Position = projectionMatrix * viewMatrix * vec4(position, 1.0);
}

#shader fragment
#version 330 core

uniform float alpha;

out vec4 color;

void main()
{
    color = vec4(1, 1, 0, alpha);
}
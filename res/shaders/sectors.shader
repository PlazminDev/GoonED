#shader vertex
#version 330 core
layout (location=0) in vec3 position;
layout (location=1) in vec2 uv;

out vec2 uvs;

uniform mat4 projectionMatrix;
uniform mat4 viewMatrix;

void main()
{
    uvs = uv;
    gl_Position = projectionMatrix * viewMatrix * vec4(position, 1.0);
}

#shader fragment
#version 330 core

in vec2 uvs;

out vec4 color;

uniform float alpha;
uniform sampler2D textureSampler;

void main()
{
    vec4 tex = texture(textureSampler, uvs);
    tex.w *= alpha;
    color = tex;
}
#shader vertex
#version 330 core
layout (location=0) in vec3 position;
layout (location=1) in vec2 UVs;

out vec2 uv;

uniform mat4 projectionMatrix;
uniform mat4 viewMatrix;
uniform mat4 modelMatrix;

void main() {
	uv = UVs;

	gl_Position = projectionMatrix * viewMatrix * modelMatrix * vec4(position, 1.0);
}

#shader fragment
#version 330 core

in vec2 uv;

uniform sampler2D textureSampler;

out vec4 out_color;

void main() {
	//out_color = vec4(uv.x, uv.y, 0, 1);
	out_color = texture(textureSampler, uv);
}
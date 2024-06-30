#shader vertex
#version 330 core
layout (location=0) in vec3 position;
layout (location=1) in vec2 UVs;

uniform mat4 projectionMatrix;
uniform mat4 viewMatrix;

void main() {
	gl_Position = projectionMatrix * viewMatrix * modelMatrix * vec4(position, 1.0);
}

#shader fragment
#version 330 core

out vec4 out_color;

void main() {
	out_color = vec4(1, 0, 0, 1);
}
shader_type canvas_item;
// Aged paper: grain, fibres and foxing keyed to screen position, so it needs no texture.
// Only the node's own drawing is affected; its children keep the default material.

uniform float grain : hint_range(0.0, 0.4) = 0.08;
uniform float foxing : hint_range(0.0, 1.0) = 0.1;

// Hash without sine (Dave Hoskins), stable at large coordinates on GLES3 and WebGL.
float hash(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), f.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), f.x), f.y);
}

void fragment() {
	vec2 p = FRAGCOORD.xy;
	float speck = hash(floor(p)) - 0.5;
	float fibre = noise(p * vec2(0.04, 0.5)) - 0.5;
	float cloud = noise(p * 0.008) * 0.55 + noise(p * 0.03) * 0.3 + noise(p * 0.11) * 0.15;
	float stain = smoothstep(0.5, 0.85, cloud);
	vec3 c = COLOR.rgb * (1.0 + speck * grain + fibre * grain * 0.7);
	// Foxing browns the paper rather than greying it.
	c = mix(c, c * vec3(0.8, 0.68, 0.52), stain * foxing * 2.5);
	COLOR.rgb = c;
}

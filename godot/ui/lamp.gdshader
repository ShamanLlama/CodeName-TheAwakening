shader_type canvas_item;
// Lamplight over the desk: warm in the middle, falling into darkness at the edges.

uniform vec2 center = vec2(0.46, 0.46);
uniform float strength : hint_range(0.0, 1.0) = 0.62;

void fragment() {
	vec2 d = (UV - center) * vec2(1.2, 1.0);
	float fall = smoothstep(0.22, 0.9, length(d));
	COLOR = vec4(0.03, 0.02, 0.01, fall * strength);
}

shader_type canvas_item;
uniform sampler2D bg;
uniform sampler2D noise;

void fragment() {
    COLOR = texture(bg,vec2(UV.x+(sin(TIME)/20.0*texture(noise,UV).r),UV.y+(cos(TIME)/20.0*texture(noise,UV).r)));
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}

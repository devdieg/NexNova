#ifndef NOVA_RENDERER_H
#define NOVA_RENDERER_H

#include <stdint.h>

int renderer_init(int width, int height);
void renderer_shutdown(void);

void renderer_resize(int width, int height);

void renderer_begin_frame(uint32_t clear_color);
void renderer_render(void);
void renderer_end_frame(void);

uint32_t* renderer_framebuffer(void);

int renderer_width(void);
int renderer_height(void);

#endif
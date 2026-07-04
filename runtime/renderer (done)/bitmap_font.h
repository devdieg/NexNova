#ifndef NOVA_BITMAP_FONT_H
#define NOVA_BITMAP_FONT_H

#include <stdint.h>

void bitmap_font_init(void);

int bitmap_font_char_width(void);
int bitmap_font_char_height(void);

void bitmap_font_draw_char(
    uint32_t* framebuffer,
    int fb_width,
    int fb_height,
    int x,
    int y,
    char c,
    uint32_t color);

void bitmap_font_draw_text(
    uint32_t* framebuffer,
    int fb_width,
    int fb_height,
    int x,
    int y,
    const char* text,
    uint32_t color);

#endif
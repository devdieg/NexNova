#ifndef NOVA_COLOR_H
#define NOVA_COLOR_H

#include <stdint.h>

typedef uint32_t NovaColor;

#define NOVA_COLOR_BLACK       0xFF000000
#define NOVA_COLOR_WHITE       0xFFFFFFFF
#define NOVA_COLOR_RED         0xFFFF0000
#define NOVA_COLOR_GREEN       0xFF00FF00
#define NOVA_COLOR_BLUE        0xFF0000FF
#define NOVA_COLOR_YELLOW      0xFFFFFF00
#define NOVA_COLOR_CYAN        0xFF00FFFF
#define NOVA_COLOR_MAGENTA     0xFFFF00FF
#define NOVA_COLOR_GRAY        0xFF808080
#define NOVA_COLOR_TRANSPARENT 0x00000000

NovaColor color_rgb(uint8_t r, uint8_t g, uint8_t b);
NovaColor color_rgba(uint8_t r, uint8_t g, uint8_t b, uint8_t a);

uint8_t color_r(NovaColor color);
uint8_t color_g(NovaColor color);
uint8_t color_b(NovaColor color);
uint8_t color_a(NovaColor color);

NovaColor color_with_alpha(NovaColor color, uint8_t alpha);

NovaColor color_blend(
    NovaColor background,
    NovaColor foreground);

NovaColor color_from_hex(const char* hex);

#endif
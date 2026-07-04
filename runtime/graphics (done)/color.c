#include "color.h"

NovaColor color_rgb(uint8_t r, uint8_t g, uint8_t b)
{
    return ((uint32_t)255 << 24) |
           ((uint32_t)r << 16) |
           ((uint32_t)g << 8) |
           (uint32_t)b;
}

NovaColor color_rgba(uint8_t r, uint8_t g, uint8_t b, uint8_t a)
{
    return ((uint32_t)a << 24) |
           ((uint32_t)r << 16) |
           ((uint32_t)g << 8) |
           (uint32_t)b;
}

uint8_t color_r(NovaColor color)
{
    return (color >> 16) & 0xFF;
}

uint8_t color_g(NovaColor color)
{
    return (color >> 8) & 0xFF;
}

uint8_t color_b(NovaColor color)
{
    return color & 0xFF;
}

uint8_t color_a(NovaColor color)
{
    return (color >> 24) & 0xFF;
}

NovaColor color_with_alpha(NovaColor color, uint8_t alpha)
{
    return (color & 0x00FFFFFF) | ((uint32_t)alpha << 24);
}

NovaColor color_blend(
    NovaColor background,
    NovaColor foreground)
{
    uint8_t fa = color_a(foreground);

    if (fa == 255)
        return foreground;

    if (fa == 0)
        return background;

    uint8_t fr = color_r(foreground);
    uint8_t fg = color_g(foreground);
    uint8_t fb = color_b(foreground);

    uint8_t br = color_r(background);
    uint8_t bg = color_g(background);
    uint8_t bb = color_b(background);

    uint8_t r = (uint8_t)((fr * fa + br * (255 - fa)) / 255);
    uint8_t g = (uint8_t)((fg * fa + bg * (255 - fa)) / 255);
    uint8_t b = (uint8_t)((fb * fa + bb * (255 - fa)) / 255);

    return color_rgb(r, g, b);
}
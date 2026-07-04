#include "bitmap_font.h"

#define FONT_WIDTH 8
#define FONT_HEIGHT 8

static uint8_t glyphs[128][8];

void bitmap_font_init(void)
{
    for (int i = 0; i < 128; i++)
    {
        for (int y = 0; y < 8; y++)
            glyphs[i][y] = 0;
    }
}

int bitmap_font_char_width(void)
{
    return FONT_WIDTH;
}

int bitmap_font_char_height(void)
{
    return FONT_HEIGHT;
}

void bitmap_font_draw_char(
    uint32_t* framebuffer,
    int fb_width,
    int fb_height,
    int x,
    int y,
    char c,
    uint32_t color)
{
    if (!framebuffer)
        return;

    unsigned char ch = (unsigned char)c;

    if (ch >= 128)
        return;

    for (int row = 0; row < FONT_HEIGHT; row++)
    {
        uint8_t bits = glyphs[ch][row];

        for (int col = 0; col < FONT_WIDTH; col++)
        {
            if (bits & (1 << (7 - col)))
            {
                int px = x + col;
                int py = y + row;

                if (px >= 0 &&
                    py >= 0 &&
                    px < fb_width &&
                    py < fb_height)
                {
                    framebuffer[py * fb_width + px] = color;
                }
            }
        }
    }
}

void bitmap_font_draw_text(
    uint32_t* framebuffer,
    int fb_width,
    int fb_height,
    int x,
    int y,
    const char* text,
    uint32_t color)
{
    if (!text)
        return;

    int cursor = x;

    while (*text)
    {
        bitmap_font_draw_char(
            framebuffer,
            fb_width,
            fb_height,
            cursor,
            y,
            *text,
            color);

        cursor += FONT_WIDTH;

        text++;
    }
}
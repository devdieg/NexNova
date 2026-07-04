#include "font.h"

#include <string.h>

static NovaFont g_default_font;

int font_init(void)
{
    g_default_font.size = 16;
    g_default_font.line_height = 18;
    g_default_font.ascent = 14;
    g_default_font.descent = 4;

    return 1;
}

void font_shutdown(void)
{
}

NovaFont* font_default(void)
{
    return &g_default_font;
}

int font_measure_char(
    NovaFont* font,
    char c)
{
    (void)c;

    if (!font)
        return 0;

    return font->size / 2;
}

int font_measure_text(
    NovaFont* font,
    const char* text)
{
    if (!font)
        return 0;

    if (!text)
        return 0;

    return (int)strlen(text) *
           font_measure_char(font, 'A');
}

int font_line_height(
    NovaFont* font)
{
    if (!font)
        return 0;

    return font->line_height;
}
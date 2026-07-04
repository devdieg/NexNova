#pragma once

#include <stdint.h>

typedef struct
{
    int size;
    int line_height;
    int ascent;
    int descent;
} NovaFont;

int font_init(void);
void font_shutdown(void);

NovaFont* font_default(void);

int font_measure_char(
    NovaFont* font,
    char c);

int font_measure_text(
    NovaFont* font,
    const char* text);

int font_line_height(
    NovaFont* font);
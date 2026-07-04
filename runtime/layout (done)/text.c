#include "text.h"

#include <string.h>

int text_layout_initialize(void)
{
    return 1;
}

void text_layout_shutdown(void)
{
}

NovaTextMetrics text_measure(
    const char* text,
    int32_t font_size)
{
    NovaTextMetrics metrics;

    if (!text)
    {
        metrics.width = 0;
        metrics.height = 0;
        return metrics;
    }

    metrics.width = (int32_t)strlen(text) * (font_size / 2);
    metrics.height = font_size;

    return metrics;
}

int32_t text_line_height(
    int32_t font_size)
{
    return font_size + (font_size / 4);
}

int32_t text_wrap(
    const char* text,
    int32_t max_width,
    int32_t font_size)
{
    if (!text || font_size <= 0)
        return 0;

    int chars_per_line = max_width / (font_size / 2);

    if (chars_per_line <= 0)
        chars_per_line = 1;

    int length = (int)strlen(text);

    int lines = (length + chars_per_line - 1) / chars_per_line;

    return lines;
}
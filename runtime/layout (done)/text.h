#ifndef NOVA_TEXT_LAYOUT_H
#define NOVA_TEXT_LAYOUT_H

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct
{
    int32_t width;
    int32_t height;
} NovaTextMetrics;

int text_layout_initialize(void);
void text_layout_shutdown(void);

NovaTextMetrics text_measure(
    const char* text,
    int32_t font_size);

int32_t text_line_height(
    int32_t font_size);

int32_t text_wrap(
    const char* text,
    int32_t max_width,
    int32_t font_size);

#ifdef __cplusplus
}
#endif

#endif
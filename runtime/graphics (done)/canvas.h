#ifndef NOVA_CANVAS_H
#define NOVA_CANVAS_H

#include <stdint.h>
#include "bitmap.h"
#include "color.h"

typedef struct
{
    NovaBitmap* bitmap;
} NovaCanvas;

void canvas_init(NovaCanvas* canvas, NovaBitmap* bitmap);

void canvas_clear(
    NovaCanvas* canvas,
    NovaColor color);

void canvas_set_pixel(
    NovaCanvas* canvas,
    int x,
    int y,
    NovaColor color);

NovaColor canvas_get_pixel(
    NovaCanvas* canvas,
    int x,
    int y);

void canvas_draw_line(
    NovaCanvas* canvas,
    int x0,
    int y0,
    int x1,
    int y1,
    NovaColor color);

void canvas_draw_rect(
    NovaCanvas* canvas,
    int x,
    int y,
    int width,
    int height,
    NovaColor color);

void canvas_fill_rect(
    NovaCanvas* canvas,
    int x,
    int y,
    int width,
    int height,
    NovaColor color);

void canvas_draw_bitmap(
    NovaCanvas* canvas,
    const NovaBitmap* bitmap,
    int x,
    int y);

#endif
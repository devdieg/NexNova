#ifndef NOVA_BITMAP_H
#define NOVA_BITMAP_H

#include <stdint.h>
#include "color.h"

typedef struct
{
    int width;
    int height;
    NovaColor* pixels;
} NovaBitmap;

NovaBitmap* bitmap_create(int width, int height);

void bitmap_destroy(NovaBitmap* bitmap);

int bitmap_resize(
    NovaBitmap* bitmap,
    int width,
    int height);

void bitmap_clear(
    NovaBitmap* bitmap,
    NovaColor color);

void bitmap_set_pixel(
    NovaBitmap* bitmap,
    int x,
    int y,
    NovaColor color);

NovaColor bitmap_get_pixel(
    const NovaBitmap* bitmap,
    int x,
    int y);

NovaBitmap* bitmap_clone(
    const NovaBitmap* bitmap);

#endif
#include "bitmap.h"

#include <stdlib.h>
#include <string.h>

NovaBitmap* bitmap_create(int width, int height)
{
    if (width <= 0 || height <= 0)
        return NULL;

    NovaBitmap* bitmap = (NovaBitmap*)malloc(sizeof(NovaBitmap));

    if (!bitmap)
        return NULL;

    bitmap->width = width;
    bitmap->height = height;

    bitmap->pixels = (NovaColor*)calloc(
        width * height,
        sizeof(NovaColor));

    if (!bitmap->pixels)
    {
        free(bitmap);
        return NULL;
    }

    return bitmap;
}

void bitmap_destroy(NovaBitmap* bitmap)
{
    if (!bitmap)
        return;

    free(bitmap->pixels);
    free(bitmap);
}

int bitmap_resize(
    NovaBitmap* bitmap,
    int width,
    int height)
{
    if (!bitmap)
        return 0;

    if (width <= 0 || height <= 0)
        return 0;

    NovaColor* pixels = (NovaColor*)calloc(
        width * height,
        sizeof(NovaColor));

    if (!pixels)
        return 0;

    int copyWidth =
        bitmap->width < width ?
        bitmap->width : width;

    int copyHeight =
        bitmap->height < height ?
        bitmap->height : height;

    for (int y = 0; y < copyHeight; y++)
    {
        memcpy(
            pixels + y * width,
            bitmap->pixels + y * bitmap->width,
            copyWidth * sizeof(NovaColor));
    }

    free(bitmap->pixels);

    bitmap->pixels = pixels;
    bitmap->width = width;
    bitmap->height = height;

    return 1;
}

void bitmap_clear(
    NovaBitmap* bitmap,
    NovaColor color)
{
    if (!bitmap)
        return;

    int total = bitmap->width * bitmap->height;

    for (int i = 0; i < total; i++)
    {
        bitmap->pixels[i] = color;
    }
}

void bitmap_set_pixel(
    NovaBitmap* bitmap,
    int x,
    int y,
    NovaColor color)
{
    if (!bitmap)
        return;

    if (x < 0 || y < 0)
        return;

    if (x >= bitmap->width)
        return;

    if (y >= bitmap->height)
        return;

    bitmap->pixels[y * bitmap->width + x] = color;
}

NovaColor bitmap_get_pixel(
    const NovaBitmap* bitmap,
    int x,
    int y)
{
    if (!bitmap)
        return NOVA_COLOR_TRANSPARENT;

    if (x < 0 || y < 0)
        return NOVA_COLOR_TRANSPARENT;

    if (x >= bitmap->width)
        return NOVA_COLOR_TRANSPARENT;

    if (y >= bitmap->height)
        return NOVA_COLOR_TRANSPARENT;

    return bitmap->pixels[
        y * bitmap->width + x];
}

NovaBitmap* bitmap_clone(
    const NovaBitmap* bitmap)
{
    if (!bitmap)
        return NULL;

    NovaBitmap* clone =
        bitmap_create(
            bitmap->width,
            bitmap->height);

    if (!clone)
        return NULL;

    memcpy(
        clone->pixels,
        bitmap->pixels,
        bitmap->width *
        bitmap->height *
        sizeof(NovaColor));

    return clone;
}
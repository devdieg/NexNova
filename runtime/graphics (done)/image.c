#include "image.h"

#include <stdlib.h>
#include <string.h>
#include <stdio.h>

NovaImage* image_create(uint32_t width,
                        uint32_t height,
                        uint8_t channels)
{
    NovaImage* image = (NovaImage*)malloc(sizeof(NovaImage));

    if (!image)
        return NULL;

    image->width = width;
    image->height = height;
    image->channels = channels;

    size_t size = (size_t)width * height * channels;

    image->pixels = (uint8_t*)malloc(size);

    if (!image->pixels)
    {
        free(image);
        return NULL;
    }

    memset(image->pixels, 0, size);

    return image;
}

void image_destroy(NovaImage* image)
{
    if (!image)
        return;

    free(image->pixels);
    free(image);
}

NovaImage* image_from_bitmap(const NovaBitmap* bitmap)
{
    if (!bitmap)
        return NULL;

    NovaImage* image = image_create(bitmap->width,
                                    bitmap->height,
                                    4);

    if (!image)
        return NULL;

    memcpy(image->pixels,
           bitmap->pixels,
           (size_t)bitmap->width * bitmap->height * 4);

    return image;
}

NovaBitmap* image_to_bitmap(const NovaImage* image)
{
    if (!image)
        return NULL;

    NovaBitmap* bitmap = bitmap_create(image->width,
                                       image->height);

    if (!bitmap)
        return NULL;

    memcpy(bitmap->pixels,
           image->pixels,
           (size_t)image->width * image->height * 4);

    return bitmap;
}

NovaImage* image_load(const char* path)
{
    (void)path;

    /*
        Decoder pendiente.

        Más adelante este método detectará:
        PNG
        JPEG
        BMP
        GIF
        WebP
        AVIF
    */

    return NULL;
}

int image_save(const NovaImage* image,
               const char* path)
{
    (void)image;
    (void)path;

    /*
        Encoder pendiente.
    */

    return 0;
}
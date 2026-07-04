#ifndef NOVA_GRAPHICS_IMAGE_H
#define NOVA_GRAPHICS_IMAGE_H

#include <stdint.h>
#include "bitmap.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef struct
{
    uint32_t width;
    uint32_t height;
    uint8_t channels;
    uint8_t* pixels;
} NovaImage;

NovaImage* image_create(uint32_t width, uint32_t height, uint8_t channels);
void image_destroy(NovaImage* image);

NovaImage* image_from_bitmap(const NovaBitmap* bitmap);
NovaBitmap* image_to_bitmap(const NovaImage* image);

NovaImage* image_load(const char* path);
int image_save(const NovaImage* image, const char* path);

#ifdef __cplusplus
}
#endif

#endif
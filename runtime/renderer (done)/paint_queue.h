#ifndef NOVA_PAINT_QUEUE_H
#define NOVA_PAINT_QUEUE_H

#include <stdint.h>

#define NOVA_MAX_PAINT_COMMANDS 8192

typedef enum
{
    NOVA_PAINT_RECT,
    NOVA_PAINT_TEXT,
    NOVA_PAINT_IMAGE,
    NOVA_PAINT_LINE
} NovaPaintType;

typedef struct
{
    int x;
    int y;
    int width;
    int height;
    uint32_t color;
} NovaPaintRect;

typedef struct
{
    int x;
    int y;
    const char* text;
    uint32_t color;
} NovaPaintText;

typedef struct
{
    int x1;
    int y1;
    int x2;
    int y2;
    uint32_t color;
} NovaPaintLine;

typedef struct
{
    int x;
    int y;
    int width;
    int height;
    void* pixels;
} NovaPaintImage;

typedef struct
{
    NovaPaintType type;

    union
    {
        NovaPaintRect rect;
        NovaPaintText text;
        NovaPaintLine line;
        NovaPaintImage image;
    };
} NovaPaintCommand;

void paint_queue_init(void);
void paint_queue_clear(void);

int paint_queue_count(void);

const NovaPaintCommand* paint_queue_get(int index);

void paint_queue_add_rect(
    int x,
    int y,
    int width,
    int height,
    uint32_t color);

void paint_queue_add_text(
    int x,
    int y,
    const char* text,
    uint32_t color);

void paint_queue_add_line(
    int x1,
    int y1,
    int x2,
    int y2,
    uint32_t color);

void paint_queue_add_image(
    int x,
    int y,
    int width,
    int height,
    void* pixels);

#endif
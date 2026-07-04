#pragma once

#include <stdint.h>
#include "../dom/dom.h"

#define NOVA_MAX_PAINT_COMMANDS 8192

typedef enum
{
    NOVA_PAINT_NONE = 0,
    NOVA_PAINT_RECT,
    NOVA_PAINT_TEXT
} NovaPaintCommandType;

typedef struct
{
    NovaPaintCommandType type;

    int x;
    int y;
    int width;
    int height;

    uint32_t color;

    const char* text;
} NovaPaintCommand;

int paint_init(void);
void paint_shutdown(void);

void paint_begin(void);

void paint_document(
    NovaDomDocument* document);

int paint_command_count(void);

const NovaPaintCommand* paint_command(
    int index);
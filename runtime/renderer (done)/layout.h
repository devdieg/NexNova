#pragma once

#include <stdint.h>
#include "../dom/dom.h"
#include "font.h"

typedef struct
{
    int x;
    int y;
    int width;
    int height;
} NovaLayoutBox;

int layout_init(void);
void layout_shutdown(void);

void layout_document(
    NovaDomDocument* document,
    int viewport_width,
    int viewport_height);

NovaLayoutBox layout_get_box(
    NovaDomNode* node);
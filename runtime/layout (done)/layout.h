#ifndef NOVA_LAYOUT_H
#define NOVA_LAYOUT_H

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct NovaNode NovaNode;

typedef struct
{
    int32_t x;
    int32_t y;
    int32_t width;
    int32_t height;
} NovaLayoutBox;

typedef struct
{
    int32_t viewport_width;
    int32_t viewport_height;
} NovaLayoutContext;

int layout_initialize(void);
void layout_shutdown(void);

void layout_document(
    NovaNode* root,
    NovaLayoutContext* context);

void layout_node(
    NovaNode* node,
    NovaLayoutContext* context);

NovaLayoutBox layout_measure(
    NovaNode* node,
    NovaLayoutContext* context);

#ifdef __cplusplus
}
#endif

#endif
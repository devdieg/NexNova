#ifndef NOVA_BLOCK_LAYOUT_H
#define NOVA_BLOCK_LAYOUT_H

#include "layout.h"

#ifdef __cplusplus
extern "C" {
#endif

int block_layout_initialize(void);
void block_layout_shutdown(void);

void block_layout(
    NovaNode* node,
    NovaLayoutContext* context);

NovaLayoutBox block_measure(
    NovaNode* node,
    NovaLayoutContext* context);

#ifdef __cplusplus
}
#endif

#endif
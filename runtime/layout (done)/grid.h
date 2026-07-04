#ifndef NOVA_GRID_LAYOUT_H
#define NOVA_GRID_LAYOUT_H

#include "layout.h"

#ifdef __cplusplus
extern "C" {
#endif

int grid_layout_initialize(void);
void grid_layout_shutdown(void);

void grid_layout(
    NovaNode* node,
    NovaLayoutContext* context);

NovaLayoutBox grid_measure(
    NovaNode* node,
    NovaLayoutContext* context);

#ifdef __cplusplus
}
#endif

#endif
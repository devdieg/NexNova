#ifndef NOVA_INLINE_LAYOUT_H
#define NOVA_INLINE_LAYOUT_H

#include "layout.h"

#ifdef __cplusplus
extern "C" {
#endif

int inline_layout_initialize(void);
void inline_layout_shutdown(void);

void inline_layout(
    NovaNode* node,
    NovaLayoutContext* context);

NovaLayoutBox inline_measure(
    NovaNode* node,
    NovaLayoutContext* context);

#ifdef __cplusplus
}
#endif

#endif
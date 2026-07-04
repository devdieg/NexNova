#ifndef NOVA_FLEX_LAYOUT_H
#define NOVA_FLEX_LAYOUT_H

#include "layout.h"

#ifdef __cplusplus
extern "C" {
#endif

int flex_layout_initialize(void);
void flex_layout_shutdown(void);

void flex_layout(
    NovaNode* node,
    NovaLayoutContext* context);

NovaLayoutBox flex_measure(
    NovaNode* node,
    NovaLayoutContext* context);

#ifdef __cplusplus
}
#endif

#endif
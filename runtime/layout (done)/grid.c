#include "grid.h"

int grid_layout_initialize(void)
{
    return 1;
}

void grid_layout_shutdown(void)
{
}

void grid_layout(
    NovaNode* node,
    NovaLayoutContext* context)
{
    if (!node || !context)
        return;

    /*
        Futuro algoritmo CSS Grid:

        - grid-template-columns
        - grid-template-rows
        - gap
        - grid-column
        - grid-row
        - auto-placement
        - align-items
        - justify-items
    */

    (void)node;
}

NovaLayoutBox grid_measure(
    NovaNode* node,
    NovaLayoutContext* context)
{
    NovaLayoutBox box;

    box.x = 0;
    box.y = 0;

    if (context)
    {
        box.width = context->viewport_width;
        box.height = 0;
    }
    else
    {
        box.width = 0;
        box.height = 0;
    }

    (void)node;

    return box;
}
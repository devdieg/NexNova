#include "layout.h"

#include <stdlib.h>

int layout_initialize(void)
{
    return 1;
}

void layout_shutdown(void)
{
}

void layout_document(
    NovaNode* root,
    NovaLayoutContext* context)
{
    if (!root || !context)
        return;

    layout_node(root, context);
}

void layout_node(
    NovaNode* node,
    NovaLayoutContext* context)
{
    if (!node || !context)
        return;

    /*
        Aquí solamente se despacha el tipo
        de layout.

        Más adelante hará algo parecido a:

        switch(node->display)
        {
            case DISPLAY_BLOCK:
                block_layout(...);
                break;

            case DISPLAY_INLINE:
                inline_layout(...);
                break;

            case DISPLAY_FLEX:
                flex_layout(...);
                break;

            case DISPLAY_GRID:
                grid_layout(...);
                break;
        }
    */
}

NovaLayoutBox layout_measure(
    NovaNode* node,
    NovaLayoutContext* context)
{
    NovaLayoutBox box;

    box.x = 0;
    box.y = 0;

    if (context)
    {
        box.width = context->viewport_width;
        box.height = context->viewport_height;
    }
    else
    {
        box.width = 0;
        box.height = 0;
    }

    (void)node;

    return box;
}
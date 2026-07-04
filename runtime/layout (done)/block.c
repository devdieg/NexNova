#include "block.h"

int block_layout_initialize(void)
{
    return 1;
}

void block_layout_shutdown(void)
{
}

void block_layout(
    NovaNode* node,
    NovaLayoutContext* context)
{
    if (!node || !context)
        return;

    /*
        Algoritmo (más adelante):

        1. Obtener márgenes.
        2. Obtener padding.
        3. Obtener borde.
        4. Calcular ancho.
        5. Colocar debajo del bloque anterior.
        6. Layout de los hijos.
        7. Calcular altura final.
    */
}

NovaLayoutBox block_measure(
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
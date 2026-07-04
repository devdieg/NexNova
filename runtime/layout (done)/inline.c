#include "inline.h"

int inline_layout_initialize(void)
{
    return 1;
}

void inline_layout_shutdown(void)
{
}

void inline_layout(
    NovaNode* node,
    NovaLayoutContext* context)
{
    if (!node || !context)
        return;

    /*
        Futuro algoritmo:

        1. Medir texto.
        2. Medir imágenes inline.
        3. Colocar elementos uno al lado del otro.
        4. Si exceden el ancho:
              -> salto de línea.
        5. Calcular altura de la línea.
        6. Continuar con la siguiente línea.
    */
}

NovaLayoutBox inline_measure(
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
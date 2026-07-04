#include "flex.h"

int flex_layout_initialize(void)
{
    return 1;
}

void flex_layout_shutdown(void)
{
}

void flex_layout(
    NovaNode* node,
    NovaLayoutContext* context)
{
    if (!node || !context)
        return;

    /*
        Implementación futura:

        1. Obtener flex-direction.
        2. Obtener flex-wrap.
        3. Obtener justify-content.
        4. Obtener align-items.
        5. Medir todos los hijos.
        6. Repartir espacio disponible.
        7. Posicionar cada hijo.
        8. Calcular tamaño final del contenedor.
    */

    (void)node;
}

NovaLayoutBox flex_measure(
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
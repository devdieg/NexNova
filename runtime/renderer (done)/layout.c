#include "layout.h"

#include <string.h>

static int s_cursor_y = 0;

int layout_init(void)
{
    s_cursor_y = 0;
    return 1;
}

void layout_shutdown(void)
{
}

static int measure_node_height(
    NovaDomNode* node)
{
    if (!node)
        return 0;

    if (node->text && node->text[0] != '\0')
    {
        return font_line_height(font_default()) + 8;
    }

    return 24;
}

static void layout_node(
    NovaDomNode* node,
    int x,
    int width)
{
    if (!node)
        return;

    node->layout.x = x;
    node->layout.y = s_cursor_y;
    node->layout.width = width;
    node->layout.height = measure_node_height(node);

    s_cursor_y += node->layout.height;

    for (int i = 0; i < node->child_count; i++)
    {
        layout_node(
            node->children[i],
            x + 8,
            width - 16);
    }
}

void layout_document(
    NovaDomDocument* document,
    int viewport_width,
    int viewport_height)
{
    (void)viewport_height;

    if (!document)
        return;

    s_cursor_y = 0;

    layout_node(
        document->root,
        0,
        viewport_width);
}

NovaLayoutBox layout_get_box(
    NovaDomNode* node)
{
    NovaLayoutBox box = {0};

    if (!node)
        return box;

    box.x = node->layout.x;
    box.y = node->layout.y;
    box.width = node->layout.width;
    box.height = node->layout.height;

    return box;
}
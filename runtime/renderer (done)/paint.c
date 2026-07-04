#include "paint.h"

#include <string.h>

static NovaPaintCommand g_commands[NOVA_MAX_PAINT_COMMANDS];
static int g_command_count = 0;

int paint_init(void)
{
    g_command_count = 0;
    return 1;
}

void paint_shutdown(void)
{
}

void paint_begin(void)
{
    g_command_count = 0;
}

static void push_rect(
    NovaDomNode* node)
{
    if (g_command_count >= NOVA_MAX_PAINT_COMMANDS)
        return;

    NovaPaintCommand* cmd =
        &g_commands[g_command_count++];

    cmd->type = NOVA_PAINT_RECT;

    cmd->x = node->layout.x;
    cmd->y = node->layout.y;
    cmd->width = node->layout.width;
    cmd->height = node->layout.height;

    cmd->color = 0xFFF0F0F0;

    cmd->text = NULL;
}

static void push_text(
    NovaDomNode* node)
{
    if (!node->text)
        return;

    if (node->text[0] == '\0')
        return;

    if (g_command_count >= NOVA_MAX_PAINT_COMMANDS)
        return;

    NovaPaintCommand* cmd =
        &g_commands[g_command_count++];

    cmd->type = NOVA_PAINT_TEXT;

    cmd->x = node->layout.x + 4;
    cmd->y = node->layout.y + 4;

    cmd->width = 0;
    cmd->height = 0;

    cmd->color = 0xFF000000;

    cmd->text = node->text;
}

static void paint_node(
    NovaDomNode* node)
{
    if (!node)
        return;

    push_rect(node);
    push_text(node);

    for (int i = 0; i < node->child_count; i++)
    {
        paint_node(node->children[i]);
    }
}

void paint_document(
    NovaDomDocument* document)
{
    if (!document)
        return;

    paint_begin();

    paint_node(document->root);
}

int paint_command_count(void)
{
    return g_command_count;
}

const NovaPaintCommand* paint_command(
    int index)
{
    if (index < 0)
        return NULL;

    if (index >= g_command_count)
        return NULL;

    return &g_commands[index];
}
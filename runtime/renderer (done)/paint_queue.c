#include "paint_queue.h"

static NovaPaintCommand queue[NOVA_MAX_PAINT_COMMANDS];
static int queue_count = 0;

void paint_queue_init(void)
{
    queue_count = 0;
}

void paint_queue_clear(void)
{
    queue_count = 0;
}

int paint_queue_count(void)
{
    return queue_count;
}

const NovaPaintCommand* paint_queue_get(int index)
{
    if (index < 0)
        return 0;

    if (index >= queue_count)
        return 0;

    return &queue[index];
}

void paint_queue_add_rect(
    int x,
    int y,
    int width,
    int height,
    uint32_t color)
{
    if (queue_count >= NOVA_MAX_PAINT_COMMANDS)
        return;

    NovaPaintCommand* cmd = &queue[queue_count++];

    cmd->type = NOVA_PAINT_RECT;

    cmd->rect.x = x;
    cmd->rect.y = y;
    cmd->rect.width = width;
    cmd->rect.height = height;
    cmd->rect.color = color;
}

void paint_queue_add_text(
    int x,
    int y,
    const char* text,
    uint32_t color)
{
    if (queue_count >= NOVA_MAX_PAINT_COMMANDS)
        return;

    NovaPaintCommand* cmd = &queue[queue_count++];

    cmd->type = NOVA_PAINT_TEXT;

    cmd->text.x = x;
    cmd->text.y = y;
    cmd->text.text = text;
    cmd->text.color = color;
}

void paint_queue_add_line(
    int x1,
    int y1,
    int x2,
    int y2,
    uint32_t color)
{
    if (queue_count >= NOVA_MAX_PAINT_COMMANDS)
        return;

    NovaPaintCommand* cmd = &queue[queue_count++];

    cmd->type = NOVA_PAINT_LINE;

    cmd->line.x1 = x1;
    cmd->line.y1 = y1;
    cmd->line.x2 = x2;
    cmd->line.y2 = y2;
    cmd->line.color = color;
}

void paint_queue_add_image(
    int x,
    int y,
    int width,
    int height,
    void* pixels)
{
    if (queue_count >= NOVA_MAX_PAINT_COMMANDS)
        return;

    NovaPaintCommand* cmd = &queue[queue_count++];

    cmd->type = NOVA_PAINT_IMAGE;

    cmd->image.x = x;
    cmd->image.y = y;
    cmd->image.width = width;
    cmd->image.height = height;
    cmd->image.pixels = pixels;
}
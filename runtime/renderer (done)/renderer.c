#include "renderer.h"

#include "bitmap_font.h"
#include "paint.h"
#include "paint_queue.h"

#include <stdlib.h>
#include <string.h>

static uint32_t* framebuffer = NULL;
static int fb_width = 0;
static int fb_height = 0;

int renderer_init(int width, int height)
{
    fb_width = width;
    fb_height = height;

    framebuffer = (uint32_t*)malloc(width * height * sizeof(uint32_t));

    if (!framebuffer)
        return 0;

    bitmap_font_init();
    paint_queue_init();

    return 1;
}

void renderer_shutdown(void)
{
    if (framebuffer)
    {
        free(framebuffer);
        framebuffer = NULL;
    }

    fb_width = 0;
    fb_height = 0;
}

void renderer_resize(int width, int height)
{
    if (width == fb_width && height == fb_height)
        return;

    free(framebuffer);

    fb_width = width;
    fb_height = height;

    framebuffer = (uint32_t*)malloc(width * height * sizeof(uint32_t));
}

void renderer_begin_frame(uint32_t clear_color)
{
    if (!framebuffer)
        return;

    for (int i = 0; i < fb_width * fb_height; i++)
        framebuffer[i] = clear_color;

    paint_queue_clear();
}

void renderer_render(void)
{
    int count = paint_queue_count();

    for (int i = 0; i < count; i++)
    {
        const NovaPaintCommand* cmd = paint_queue_get(i);

        switch (cmd->type)
        {
        case NOVA_PAINT_RECT:

            paint_fill_rect(
                framebuffer,
                fb_width,
                fb_height,
                cmd->rect.x,
                cmd->rect.y,
                cmd->rect.width,
                cmd->rect.height,
                cmd->rect.color);

            break;

        case NOVA_PAINT_TEXT:

            bitmap_font_draw_text(
                framebuffer,
                fb_width,
                fb_height,
                cmd->text.x,
                cmd->text.y,
                cmd->text.text,
                cmd->text.color);

            break;

        case NOVA_PAINT_LINE:

            paint_draw_line(
                framebuffer,
                fb_width,
                fb_height,
                cmd->line.x1,
                cmd->line.y1,
                cmd->line.x2,
                cmd->line.y2,
                cmd->line.color);

            break;

        case NOVA_PAINT_IMAGE:

            paint_draw_image(
                framebuffer,
                fb_width,
                fb_height,
                cmd->image.x,
                cmd->image.y,
                cmd->image.width,
                cmd->image.height,
                cmd->image.pixels);

            break;
        }
    }
}

void renderer_end_frame(void)
{
}

uint32_t* renderer_framebuffer(void)
{
    return framebuffer;
}

int renderer_width(void)
{
    return fb_width;
}

int renderer_height(void)
{
    return fb_height;
}
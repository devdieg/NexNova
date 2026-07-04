#include "canvas.h"

#include <stdlib.h>

static int inside(const NovaCanvas* canvas, int x, int y)
{
    if (!canvas || !canvas->bitmap)
        return 0;

    return x >= 0 &&
           y >= 0 &&
           x < canvas->bitmap->width &&
           y < canvas->bitmap->height;
}

void canvas_init(NovaCanvas* canvas, NovaBitmap* bitmap)
{
    if (!canvas)
        return;

    canvas->bitmap = bitmap;
}

void canvas_clear(
    NovaCanvas* canvas,
    NovaColor color)
{
    if (!canvas || !canvas->bitmap)
        return;

    int total = canvas->bitmap->width * canvas->bitmap->height;

    for (int i = 0; i < total; i++)
        canvas->bitmap->pixels[i] = color;
}

void canvas_set_pixel(
    NovaCanvas* canvas,
    int x,
    int y,
    NovaColor color)
{
    if (!inside(canvas, x, y))
        return;

    canvas->bitmap->pixels[y * canvas->bitmap->width + x] = color;
}

NovaColor canvas_get_pixel(
    NovaCanvas* canvas,
    int x,
    int y)
{
    if (!inside(canvas, x, y))
        return NOVA_COLOR_TRANSPARENT;

    return canvas->bitmap->pixels[y * canvas->bitmap->width + x];
}

void canvas_draw_line(
    NovaCanvas* canvas,
    int x0,
    int y0,
    int x1,
    int y1,
    NovaColor color)
{
    int dx = abs(x1 - x0);
    int sx = x0 < x1 ? 1 : -1;

    int dy = -abs(y1 - y0);
    int sy = y0 < y1 ? 1 : -1;

    int err = dx + dy;

    while (1)
    {
        canvas_set_pixel(canvas, x0, y0, color);

        if (x0 == x1 && y0 == y1)
            break;

        int e2 = err * 2;

        if (e2 >= dy)
        {
            err += dy;
            x0 += sx;
        }

        if (e2 <= dx)
        {
            err += dx;
            y0 += sy;
        }
    }
}

void canvas_draw_rect(
    NovaCanvas* canvas,
    int x,
    int y,
    int width,
    int height,
    NovaColor color)
{
    canvas_draw_line(canvas, x, y, x + width - 1, y, color);
    canvas_draw_line(canvas, x, y, x, y + height - 1, color);

    canvas_draw_line(canvas,
                     x + width - 1,
                     y,
                     x + width - 1,
                     y + height - 1,
                     color);

    canvas_draw_line(canvas,
                     x,
                     y + height - 1,
                     x + width - 1,
                     y + height - 1,
                     color);
}

void canvas_fill_rect(
    NovaCanvas* canvas,
    int x,
    int y,
    int width,
    int height,
    NovaColor color)
{
    for (int iy = 0; iy < height; iy++)
    {
        for (int ix = 0; ix < width; ix++)
        {
            canvas_set_pixel(
                canvas,
                x + ix,
                y + iy,
                color);
        }
    }
}

void canvas_draw_bitmap(
    NovaCanvas* canvas,
    const NovaBitmap* bitmap,
    int x,
    int y)
{
    if (!canvas || !bitmap)
        return;

    for (int iy = 0; iy < bitmap->height; iy++)
    {
        for (int ix = 0; ix < bitmap->width; ix++)
        {
            NovaColor color =
                bitmap->pixels[iy * bitmap->width + ix];

            canvas_set_pixel(
                canvas,
                x + ix,
                y + iy,
                color);
        }
    }
}
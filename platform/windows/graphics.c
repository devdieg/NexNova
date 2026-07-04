#include "../common/platform.h"

#ifdef _WIN32

#include <windows.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

typedef struct
{
    HDC hdc;
    HDC memory_dc;
    HBITMAP bitmap;
    BITMAPINFO bmi;

    uint32_t* pixels;

    int width;
    int height;
} GraphicsContext;

static GraphicsContext g_ctx;

extern HWND window_get_native_handle(PlatformWindow* window);

int graphics_init(PlatformWindow* window)
{
    memset(&g_ctx, 0, sizeof(g_ctx));

    HWND hwnd = window_get_native_handle(window);
    if (!hwnd)
        return 0;

    RECT rect;
    GetClientRect(hwnd, &rect);

    g_ctx.width = rect.right - rect.left;
    g_ctx.height = rect.bottom - rect.top;

    g_ctx.hdc = GetDC(hwnd);
    g_ctx.memory_dc = CreateCompatibleDC(g_ctx.hdc);

    ZeroMemory(&g_ctx.bmi, sizeof(BITMAPINFO));

    g_ctx.bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
    g_ctx.bmi.bmiHeader.biWidth = g_ctx.width;
    g_ctx.bmi.bmiHeader.biHeight = -g_ctx.height;
    g_ctx.bmi.bmiHeader.biPlanes = 1;
    g_ctx.bmi.bmiHeader.biBitCount = 32;
    g_ctx.bmi.bmiHeader.biCompression = BI_RGB;

    g_ctx.bitmap = CreateDIBSection(
        g_ctx.memory_dc,
        &g_ctx.bmi,
        DIB_RGB_COLORS,
        (void**)&g_ctx.pixels,
        NULL,
        0);

    if (!g_ctx.bitmap)
        return 0;

    SelectObject(g_ctx.memory_dc, g_ctx.bitmap);

    return 1;
}

void graphics_shutdown(void)
{
    if (g_ctx.bitmap)
        DeleteObject(g_ctx.bitmap);

    if (g_ctx.memory_dc)
        DeleteDC(g_ctx.memory_dc);

    if (g_ctx.hdc)
        ReleaseDC(WindowFromDC(g_ctx.hdc), g_ctx.hdc);

    memset(&g_ctx, 0, sizeof(g_ctx));
}

void graphics_clear(uint32_t color)
{
    int count = g_ctx.width * g_ctx.height;

    for (int i = 0; i < count; i++)
        g_ctx.pixels[i] = color;
}

void graphics_draw_pixel(int x, int y, uint32_t color)
{
    if (x < 0 || y < 0)
        return;

    if (x >= g_ctx.width || y >= g_ctx.height)
        return;

    g_ctx.pixels[y * g_ctx.width + x] = color;
}

void graphics_draw_rect(int x,
                        int y,
                        int width,
                        int height,
                        uint32_t color)
{
    for (int yy = 0; yy < height; yy++)
    {
        for (int xx = 0; xx < width; xx++)
        {
            graphics_draw_pixel(x + xx, y + yy, color);
        }
    }
}

void graphics_present(void)
{
    BitBlt(
        g_ctx.hdc,
        0,
        0,
        g_ctx.width,
        g_ctx.height,
        g_ctx.memory_dc,
        0,
        0,
        SRCCOPY);
}

uint32_t* graphics_get_framebuffer(void)
{
    return g_ctx.pixels;
}

int graphics_get_width(void)
{
    return g_ctx.width;
}

int graphics_get_height(void)
{
    return g_ctx.height;
}

#endif
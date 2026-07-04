#include "../common/platform.h"

#ifdef _WIN32

#include <windows.h>
#include <string.h>

static unsigned char g_keys[256];
static unsigned char g_mouse_buttons[8];

static int g_mouse_x = 0;
static int g_mouse_y = 0;
static int g_mouse_wheel = 0;

void input_init(void)
{
    memset(g_keys, 0, sizeof(g_keys));
    memset(g_mouse_buttons, 0, sizeof(g_mouse_buttons));

    g_mouse_x = 0;
    g_mouse_y = 0;
    g_mouse_wheel = 0;
}

void input_shutdown(void)
{
}

void input_update(void)
{
    POINT p;

    if (GetCursorPos(&p))
    {
        g_mouse_x = p.x;
        g_mouse_y = p.y;
    }

    for (int i = 0; i < 256; i++)
    {
        g_keys[i] = (GetAsyncKeyState(i) & 0x8000) != 0;
    }

    g_mouse_buttons[0] =
        (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;

    g_mouse_buttons[1] =
        (GetAsyncKeyState(VK_RBUTTON) & 0x8000) != 0;

    g_mouse_buttons[2] =
        (GetAsyncKeyState(VK_MBUTTON) & 0x8000) != 0;
}

int input_key_down(int key)
{
    if (key < 0 || key >= 256)
        return 0;

    return g_keys[key];
}

int input_mouse_down(int button)
{
    if (button < 0 || button >= 8)
        return 0;

    return g_mouse_buttons[button];
}

int input_mouse_x(void)
{
    return g_mouse_x;
}

int input_mouse_y(void)
{
    return g_mouse_y;
}

int input_mouse_wheel(void)
{
    return g_mouse_wheel;
}

void input_set_mouse_wheel(int delta)
{
    g_mouse_wheel += delta;
}

void input_reset_mouse_wheel(void)
{
    g_mouse_wheel = 0;
}

#endif
#include "../common/platform.h"

#ifdef _WIN32

#include <windows.h>
#include <stdlib.h>
#include <string.h>

struct PlatformWindow {
    HWND hwnd;
    int width;
    int height;
    int should_close;
};

static const char* WINDOW_CLASS = "NexNovaWindowClass";

extern void input_set_mouse_wheel(int delta);

static LRESULT CALLBACK WindowProc(HWND hwnd,
                                   UINT msg,
                                   WPARAM wParam,
                                   LPARAM lParam)
{
    PlatformWindow* window =
        (PlatformWindow*)GetWindowLongPtr(hwnd, GWLP_USERDATA);

    switch (msg)
    {
        case WM_CLOSE:
            if (window)
                window->should_close = 1;

            DestroyWindow(hwnd);
            return 0;

        case WM_DESTROY:
            PostQuitMessage(0);
            return 0;

        case WM_SIZE:
            if (window)
            {
                window->width = LOWORD(lParam);
                window->height = HIWORD(lParam);
            }
        case WM_MOUSEWHEEL:
            {
                input_set_mouse_wheel(GET_WHEEL_DELTA_WPARAM(wParam));
                return 0;
            }
            return 0;
    }

    return DefWindowProc(hwnd, msg, wParam, lParam);
}

static int class_registered = 0;

static int register_window_class(void)
{
    if (class_registered)
        return 1;

    WNDCLASS wc;
    ZeroMemory(&wc, sizeof(wc));

    wc.lpfnWndProc = WindowProc;
    wc.hInstance = GetModuleHandle(NULL);
    wc.lpszClassName = WINDOW_CLASS;
    wc.hCursor = LoadCursor(NULL, IDC_ARROW);

    if (!RegisterClass(&wc))
        return 0;

    class_registered = 1;
    return 1;
}

PlatformWindow* window_create(const char* title,
                              int width,
                              int height)
{
    if (!register_window_class())
        return NULL;

    PlatformWindow* window =
        (PlatformWindow*)calloc(1, sizeof(PlatformWindow));

    if (!window)
        return NULL;

    window->width = width;
    window->height = height;
    window->should_close = 0;

    HWND hwnd = CreateWindowEx(
        0,
        WINDOW_CLASS,
        title,
        WS_OVERLAPPEDWINDOW,
        CW_USEDEFAULT,
        CW_USEDEFAULT,
        width,
        height,
        NULL,
        NULL,
        GetModuleHandle(NULL),
        NULL);

    if (!hwnd)
    {
        free(window);
        return NULL;
    }

    window->hwnd = hwnd;

    SetWindowLongPtr(
        hwnd,
        GWLP_USERDATA,
        (LONG_PTR)window);

    ShowWindow(hwnd, SW_SHOW);
    UpdateWindow(hwnd);

    return window;
}

void window_destroy(PlatformWindow* window)
{
    if (!window)
        return;

    if (window->hwnd)
        DestroyWindow(window->hwnd);

    free(window);
}

void window_show(PlatformWindow* window)
{
    if (!window)
        return;

    ShowWindow(window->hwnd, SW_SHOW);
    UpdateWindow(window->hwnd);
}

void window_poll_events(void)
{
    MSG msg;

    while (PeekMessage(&msg,
                       NULL,
                       0,
                       0,
                       PM_REMOVE))
    {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }
}

int window_should_close(PlatformWindow* window)
{
    if (!window)
        return 1;

    return window->should_close;
}

int window_get_width(PlatformWindow* window)
{
    if (!window)
        return 0;

    return window->width;
}

int window_get_height(PlatformWindow* window)
{
    if (!window)
        return 0;

    return window->height;
}

void window_set_title(PlatformWindow* window,
                      const char* title)
{
    if (!window)
        return;

    SetWindowText(window->hwnd, title);
}

HWND window_get_native_handle(PlatformWindow* window)
{
    if (!window)
        return NULL;

    return window->hwnd;
}

#endif
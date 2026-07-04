#include "windows.h"
#include "../common/platform.h"

static const char* WINDOW_CLASS_NAME = "NexNovaWindowClass";

struct PlatformWindow {
    HWND hwnd;
    int width;
    int height;
    BOOL should_close;
};

static PlatformWindow g_window = {0};
static PlatformWindow* g_active_window = NULL;
static HINSTANCE g_instance = NULL;
static BOOL g_class_registered = FALSE;

static LRESULT CALLBACK WindowProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    (void)wParam;
    (void)lParam;

    switch (msg) {
    case WM_CLOSE:
    case WM_DESTROY:
        if (g_active_window) {
            g_active_window->should_close = TRUE;
        }
        PostQuitMessage(0);
        return 0;
    default:
        return DefWindowProcA(hwnd, msg, wParam, lParam);
    }
}

static BOOL register_window_class(void) {
    WNDCLASSEXA wc = {0};
    wc.cbSize = sizeof(wc);
    wc.style = CS_HREDRAW | CS_VREDRAW;
    wc.lpfnWndProc = WindowProc;
    wc.cbClsExtra = 0;
    wc.cbWndExtra = 0;
    wc.hInstance = g_instance;
    wc.hIcon = NULL;
    wc.hCursor = NULL;
    wc.hbrBackground = NULL;
    wc.lpszMenuName = NULL;
    wc.lpszClassName = WINDOW_CLASS_NAME;
    wc.hIconSm = NULL;

    return RegisterClassExA(&wc) != 0;
}

PlatformWindow* window_create(const char* title, int width, int height) {
    if (g_active_window) {
        return NULL;
    }

    if (!g_instance) {
        g_instance = GetModuleHandleA(NULL);
        if (!g_instance) {
            return NULL;
        }
    }

    if (!g_class_registered) {
        if (!register_window_class()) {
            return NULL;
        }
        g_class_registered = TRUE;
    }

    HWND hwnd = CreateWindowExA(
        0,
        WINDOW_CLASS_NAME,
        title,
        WS_OVERLAPPEDWINDOW | WS_VISIBLE,
        CW_USEDEFAULT,
        CW_USEDEFAULT,
        width,
        height,
        NULL,
        NULL,
        g_instance,
        NULL);

    if (!hwnd) {
        return NULL;
    }

    g_window.hwnd = hwnd;
    g_window.width = width;
    g_window.height = height;
    g_window.should_close = FALSE;
    g_active_window = &g_window;
    return &g_window;
}

void window_destroy(PlatformWindow* window) {
    if (!window || !window->hwnd) {
        return;
    }

    DestroyWindow(window->hwnd);
    if (g_active_window == window) {
        g_active_window = NULL;
    }
}

void window_show(PlatformWindow* window) {
    if (!window || !window->hwnd) {
        return;
    }

    ShowWindow(window->hwnd, SW_SHOW);
    UpdateWindow(window->hwnd);
}

void window_poll_events(void) {
    MSG msg;

    while (PeekMessageA(&msg, NULL, 0, 0, PM_REMOVE)) {
        TranslateMessage(&msg);
        DispatchMessageA(&msg);
    }
}

int window_should_close(PlatformWindow* window) {
    return window ? window->should_close : 1;
}

int window_get_width(PlatformWindow* window) {
    return window ? window->width : 0;
}

int window_get_height(PlatformWindow* window) {
    return window ? window->height : 0;
}

void window_set_title(PlatformWindow* window, const char* title) {
    if (!window || !window->hwnd || !title) {
        return;
    }

    SetWindowTextA(window->hwnd, title);
}

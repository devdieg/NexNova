#include "../common/platform.h"
#include <windows.h>

PlatformWindow* window_create(const char* title, int width, int height);
void window_destroy(PlatformWindow* window);
void window_show(PlatformWindow* window);
void window_poll_events(void);
int window_should_close(PlatformWindow* window);
int window_get_width(PlatformWindow* window);
int window_get_height(PlatformWindow* window);
void window_set_title(PlatformWindow* window, const char* title);
void input_init(void);
void input_shutdown(void);

static PlatformWindow* s_main_window = NULL;

int platform_init(void) {
    input_init();
    return 1;
}

void platform_shutdown(void) {
    input_shutdown();
}

PlatformWindow* platform_window_create(const char* title, int width, int height) {
    s_main_window = window_create(title, width, height);
    return s_main_window;
}

void platform_window_destroy(PlatformWindow* window) {
    if (window) {
        window_destroy(window);
        if (window == s_main_window) {
            s_main_window = NULL;
        }
    }
}

void platform_window_show(PlatformWindow* window) {
    if (window) {
        window_show(window);
    }
}

void platform_poll_events(void) {
    window_poll_events();
}

int platform_window_should_close(PlatformWindow* window) {
    return window ? window_should_close(window) : 1;
}

int platform_window_get_width(PlatformWindow* window) {
    return window ? window_get_width(window) : 0;
}

int platform_window_get_height(PlatformWindow* window) {
    return window ? window_get_height(window) : 0;
}

void platform_window_set_title(PlatformWindow* window, const char* title) {
    if (window) {
        window_set_title(window, title);
    }
}

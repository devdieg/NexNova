#pragma once

#include <stdint.h>
#include <stddef.h>
#include <winsock2.h>

#ifdef __cplusplus
#ifdef _WIN32
extern "C" {
#endif

typedef struct PlatformWindow PlatformWindow;

typedef SOCKET NovaSocket;

#else

typedef int NovaSocket;

#endif

int socket_init(void);
void socket_shutdown(void);

NovaSocket socket_create(void);
void socket_close(NovaSocket socket);

int socket_connect(
    NovaSocket socket,
    const char* host,
    unsigned short port);

int socket_send(
    NovaSocket socket,
    const void* data,
    int size);

int socket_receive(
    NovaSocket socket,
    void* buffer,
    int size);

typedef enum
{
    PLATFORM_FALSE = 0,
    PLATFORM_TRUE = 1

} PlatformBool;

typedef enum
{
    PLATFORM_KEY_UNKNOWN = 0,

    PLATFORM_KEY_ESCAPE,

    PLATFORM_KEY_ENTER,

    PLATFORM_KEY_SPACE,

    PLATFORM_KEY_TAB,

    PLATFORM_KEY_BACKSPACE,

    PLATFORM_KEY_LEFT,

    PLATFORM_KEY_RIGHT,

    PLATFORM_KEY_UP,

    PLATFORM_KEY_DOWN,

    PLATFORM_KEY_SHIFT,

    PLATFORM_KEY_CONTROL,

    PLATFORM_KEY_ALT,

    PLATFORM_KEY_A,
    PLATFORM_KEY_B,
    PLATFORM_KEY_C,
    PLATFORM_KEY_D,
    PLATFORM_KEY_E,
    PLATFORM_KEY_F,
    PLATFORM_KEY_G,
    PLATFORM_KEY_H,
    PLATFORM_KEY_I,
    PLATFORM_KEY_J,
    PLATFORM_KEY_K,
    PLATFORM_KEY_L,
    PLATFORM_KEY_M,
    PLATFORM_KEY_N,
    PLATFORM_KEY_O,
    PLATFORM_KEY_P,
    PLATFORM_KEY_Q,
    PLATFORM_KEY_R,
    PLATFORM_KEY_S,
    PLATFORM_KEY_T,
    PLATFORM_KEY_U,
    PLATFORM_KEY_V,
    PLATFORM_KEY_W,
    PLATFORM_KEY_X,
    PLATFORM_KEY_Y,
    PLATFORM_KEY_Z

} PlatformKey;

typedef struct
{
    int x;
    int y;

} PlatformPoint;

typedef struct
{
    int width;
    int height;

} PlatformSize;

typedef struct
{
    uint8_t r;
    uint8_t g;
    uint8_t b;
    uint8_t a;

} PlatformColor;

typedef struct
{
    PlatformBool left;
    PlatformBool middle;
    PlatformBool right;

    int x;
    int y;

    int wheel;

} PlatformMouseState;

typedef struct
{
    PlatformBool keys[256];

} PlatformKeyboardState;

int platform_init(void)
{
    input_init();

    if (!socket_init())
        return 0;

    return 1;
}

void platform_shutdown(void)
{
    socket_shutdown();
    input_shutdown();
}

PlatformWindow*
platform_window_create(
    const char* title,
    int width,
    int height
);

void
platform_window_destroy(
    PlatformWindow* window
);

void
platform_window_show(
    PlatformWindow* window
);

void
platform_window_hide(
    PlatformWindow* window
);

void
platform_window_poll_events(void);

PlatformBool
platform_window_should_close(
    PlatformWindow* window
);

void
platform_window_set_title(
    PlatformWindow* window,
    const char* title
);

void
platform_window_resize(
    PlatformWindow* window,
    int width,
    int height
);

int
platform_window_width(
    PlatformWindow* window
);

int
platform_window_height(
    PlatformWindow* window
);

void*
platform_graphics_begin(
    PlatformWindow* window
);

void
platform_graphics_end(
    PlatformWindow* window
);

void
platform_graphics_present(
    PlatformWindow* window
);

void
platform_graphics_clear(
    uint32_t color
);

void
platform_graphics_draw_pixel(
    int x,
    int y,
    uint32_t color
);

void
platform_graphics_draw_rect(
    int x,
    int y,
    int width,
    int height,
    uint32_t color
);

void
platform_graphics_draw_text(
    int x,
    int y,
    const char* text,
    uint32_t color
);

void
platform_input_update(void);

PlatformMouseState
platform_mouse_state(void);

PlatformKeyboardState
platform_keyboard_state(void);

PlatformBool
platform_key_down(
    PlatformKey key
);

PlatformBool
platform_mouse_down(
    int button
);

uint64_t
platform_time_ms(void);

void
platform_sleep(
    uint32_t milliseconds
);

int
platform_socket_initialize(void);

void
platform_socket_shutdown(void);

void input_init(void);
void input_shutdown(void);
void input_update(void);

int input_key_down(int key);

int input_mouse_down(int button);

int input_mouse_x(void);
int input_mouse_y(void);

int input_mouse_wheel(void);

void input_set_mouse_wheel(int delta);
void input_reset_mouse_wheel(void);

#ifdef __cplusplus
}
#endif
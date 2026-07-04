#include "windows.h"

#define MAX_INPUT_BUFFER 4096

typedef struct {
    char buffer[MAX_INPUT_BUFFER];
    size_t length;
    BOOL is_active;

    int mouse_x;
    int mouse_y;
    BOOL left_button;
    BOOL right_button;
} InputContext;

static InputContext input_context = {0};

void input_init(void) {
    input_context = (InputContext){0};
    input_context.is_active = TRUE;
}

void input_clear(void); // Forward declaration

void input_handle_key(UINT message, WPARAM wParam, LPARAM lParam) {
    (void)lParam;
    if (!input_context.is_active) return;

    switch (message) {
    case WM_CHAR:
        if ((unsigned)wParam >= 32u && wParam != 127u) {
            if (input_context.length < MAX_INPUT_BUFFER - 1) {
                input_context.buffer[input_context.length++] = (char)wParam;
                input_context.buffer[input_context.length] = '\0';
            }
        }
        break;

    case WM_KEYDOWN:
        switch (wParam) {
        case VK_BACK:
            if (input_context.length > 0) {
                input_context.buffer[--input_context.length] = '\0';
            }
            break;
        case VK_DELETE:
            input_clear();
            break;
        case VK_RETURN:
            // TODO: dispatch input submission or action
            input_clear();
            break;
        case VK_ESCAPE:
            input_clear();
            break;
        default:
            break;
        }
        break;

    default:
        break;
    }
}

void input_handle_mouse(UINT message, WPARAM wParam, LPARAM lParam) {
    input_context.mouse_x = GET_X_LPARAM(lParam);
    input_context.mouse_y = GET_Y_LPARAM(lParam);

    switch (message) {
    case WM_LBUTTONDOWN:
        input_context.left_button = TRUE;
        break;
    case WM_LBUTTONUP:
        input_context.left_button = FALSE;
        break;
    case WM_RBUTTONDOWN:
        input_context.right_button = TRUE;
        break;
    case WM_RBUTTONUP:
        input_context.right_button = FALSE;
        break;
    case WM_MOUSEMOVE:
        break;
    case WM_MOUSEWHEEL:
        (void)wParam;
        break;
    default:
        break;
    }
}

const char* input_get_buffer(void) {
    return input_context.buffer;
}

size_t input_get_length(void) {
    return input_context.length;
}

int input_get_mouse_x(void) {
    return input_context.mouse_x;
}

int input_get_mouse_y(void) {
    return input_context.mouse_y;
}

BOOL input_is_left_button_down(void) {
    return input_context.left_button;
}

BOOL input_is_right_button_down(void) {
    return input_context.right_button;
}

void input_clear(void) {
    input_context.length = 0;
    input_context.buffer[0] = '\0';
}

void input_set_active(BOOL active) {
    input_context.is_active = active;
}

BOOL input_is_active(void) {
    return input_context.is_active;
}

void input_shutdown(void) {
    input_clear();
    input_context.is_active = FALSE;
}
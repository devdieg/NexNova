#include "../common/platform.h"

#ifdef _WIN32

#include <winsock2.h>
#include <ws2tcpip.h>

#pragma comment(lib, "ws2_32.lib")

static int g_initialized = 0;

int socket_init(void)
{
    if (g_initialized)
        return 1;

    WSADATA data;

    if (WSAStartup(MAKEWORD(2, 2), &data) != 0)
        return 0;

    g_initialized = 1;
    return 1;
}

void socket_shutdown(void)
{
    if (!g_initialized)
        return;

    WSACleanup();
    g_initialized = 0;
}

NovaSocket socket_create(void)
{
    return socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
}

void socket_close(NovaSocket s)
{
    if (s != INVALID_SOCKET)
        closesocket(s);
}

int socket_connect(NovaSocket s, const char* host, unsigned short port)
{
    struct addrinfo hints;
    struct addrinfo* result = NULL;

    char port_string[16];
    sprintf(port_string, "%u", port);

    ZeroMemory(&hints, sizeof(hints));

    hints.ai_family = AF_UNSPEC;
    hints.ai_socktype = SOCK_STREAM;
    hints.ai_protocol = IPPROTO_TCP;

    if (getaddrinfo(host, port_string, &hints, &result) != 0)
        return 0;

    int ok = 0;

    for (struct addrinfo* ptr = result; ptr; ptr = ptr->ai_next)
    {
        if (connect(s, ptr->ai_addr, (int)ptr->ai_addrlen) == 0)
        {
            ok = 1;
            break;
        }
    }

    freeaddrinfo(result);

    return ok;
}

int socket_send(NovaSocket s, const void* data, int size)
{
    return send(s, (const char*)data, size, 0);
}

int socket_receive(NovaSocket s, void* buffer, int size)
{
    return recv(s, (char*)buffer, size, 0);
}

#endif
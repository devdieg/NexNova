#pragma once

typedef struct
{
    char** files;
    int count;
    int capacity;

} SourceList;


void scanner_init(SourceList* list);

void scanner_free(SourceList* list);

int scanner_scan_directory(
    SourceList* list,
    const char* directory
);

void scanner_print(
    const SourceList* list
);
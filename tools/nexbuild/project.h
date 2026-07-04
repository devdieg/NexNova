#pragma once

#include "scanner.h"


typedef struct
{
    char name[128];

    char output[256];

    char source[256];

    char compiler[64];

    char flags[256];

    char entry[128];


} NovaProject;



int project_load(
    NovaProject* project,
    const char* file
);



int project_build(
    NovaProject* project
);
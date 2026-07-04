#include "project.h"

#include "parser.h"
#include "linker.h"

#include <stdio.h>
#include <string.h>



int project_load(
    NovaProject* project,
    const char* file
)
{

    memset(
        project,
        0,
        sizeof(NovaProject)
    );


    if(!parser_read_project(file, project))
    {
        printf("Failed loading project\n");
        return 0;
    }


    return 1;
}




int project_build(
    NovaProject* project
)
{

    printf("\nBuilding %s...\n",
        project->name
    );



    SourceList sources;


    scanner_init(
        &sources
    );



    if(!scanner_scan_directory(
        &sources,
        project->source
    ))
    {
        printf(
            "Source scan failed\n"
        );

        return 0;
    }



    scanner_print(
        &sources
    );



    int result =
        linker_build(
            project,
            &sources
        );



    scanner_free(
        &sources
    );



    return result;

}
#include "linker.h"

#include <stdio.h>
#include <stdlib.h>



int linker_build(
    NovaProject* project,
    SourceList* sources
)
{

    char command[4096];


    strcpy(
        command,
        project->compiler
    );


    strcat(
        command,
        " "
    );



    for(int i=0;i<sources->count;i++)
    {

        strcat(
            command,
            sources->files[i]
        );


        strcat(
            command,
            " "
        );

    }



    strcat(
        command,
        "-o "
    );


    strcat(
        command,
        project->output
    );



    printf(
        "\nExecuting:\n%s\n\n",
        command
    );



    return system(command)==0;

}
#include "project.h"

#include <stdio.h>



int main(int argc,char** argv)
{

    const char* project_file =
        "nexnova.project";



    if(argc > 1)
        project_file = argv[1];



    NovaProject project;



    if(!project_load(
        &project,
        project_file
    ))
    {
        return 1;
    }



    if(!project_build(
        &project
    ))
    {
        printf(
            "Build failed\n"
        );

        return 1;
    }



    printf(
        "\nBuild completed!\n"
    );


    return 0;

}
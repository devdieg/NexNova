#include "parser.h"

#include <stdio.h>
#include <string.h>



static void trim(char* s)
{
    char* p = strchr(s,'\n');

    if(p)
        *p=0;
}



int parser_read_project(
    const char* file,
    NovaProject* project
)
{

    FILE* f =
        fopen(file,"r");


    if(!f)
        return 0;



    char line[512];


    while(
        fgets(line,sizeof(line),f)
    )
    {

        trim(line);



        char key[128];
        char value[256];


        if(sscanf(
            line,
            "%127[^=]=%255s",
            key,
            value
        ) != 2)
            continue;



        if(strcmp(key,"name")==0)
            strcpy(project->name,value);


        else if(strcmp(key,"output")==0)
            strcpy(project->output,value);


        else if(strcmp(key,"source")==0)
            strcpy(project->source,value);


        else if(strcmp(key,"compiler")==0)
            strcpy(project->compiler,value);


        else if(strcmp(key,"flags")==0)
            strcpy(project->flags,value);


        else if(strcmp(key,"entry")==0)
            strcpy(project->entry,value);

    }



    fclose(f);


    return 1;

}
#include "scanner.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#ifdef _WIN32
#include <windows.h>
#else
#include <dirent.h>
#include <sys/stat.h>
#endif


static int has_c_extension(const char* name)
{
    int len = strlen(name);

    if (len < 3)
        return 0;

    return strcmp(name + len - 2, ".c") == 0;
}


static int should_ignore(const char* name)
{
    if(strcmp(name, ".git") == 0)
        return 1;

    if(strcmp(name, "build") == 0)
        return 1;

    if(strcmp(name, "bin") == 0)
        return 1;

    if(strcmp(name, "obj") == 0)
        return 1;

    return 0;
}


static void add_source(
    SourceList* list,
    const char* path
)
{
    if(list->count >= list->capacity)
    {
        list->capacity *= 2;

        list->files =
            realloc(
                list->files,
                sizeof(char*) * list->capacity
            );
    }


    list->files[list->count] =
        malloc(strlen(path)+1);


    strcpy(
        list->files[list->count],
        path
    );


    list->count++;
}



void scanner_init(SourceList* list)
{
    list->count = 0;

    list->capacity = 32;

    list->files =
        malloc(
            sizeof(char*) *
            list->capacity
        );
}



void scanner_free(SourceList* list)
{
    for(int i = 0; i < list->count; i++)
    {
        free(list->files[i]);
    }


    free(list->files);

    list->files = NULL;
    list->count = 0;
}



int scanner_scan_directory(
    SourceList* list,
    const char* directory
)
{

#ifdef _WIN32


    char search[MAX_PATH];

    snprintf(
        search,
        MAX_PATH,
        "%s\\*",
        directory
    );


    WIN32_FIND_DATAA data;


    HANDLE handle =
        FindFirstFileA(
            search,
            &data
        );


    if(handle == INVALID_HANDLE_VALUE)
        return 0;



    do
    {

        const char* name =
            data.cFileName;


        if(
            strcmp(name,".") == 0 ||
            strcmp(name,"..") == 0
        )
            continue;



        if(should_ignore(name))
            continue;



        char full[MAX_PATH];


        snprintf(
            full,
            MAX_PATH,
            "%s\\%s",
            directory,
            name
        );



        if(data.dwFileAttributes &
           FILE_ATTRIBUTE_DIRECTORY)
        {

            scanner_scan_directory(
                list,
                full
            );

        }
        else
        {

            if(has_c_extension(name))
            {
                add_source(
                    list,
                    full
                );
            }

        }


    }
    while(
        FindNextFileA(
            handle,
            &data
        )
    );


    FindClose(handle);


#else


    DIR* dir =
        opendir(directory);


    if(!dir)
        return 0;



    struct dirent* entry;


    while(
        (entry = readdir(dir))
    )
    {

        if(
            strcmp(entry->d_name,".")==0 ||
            strcmp(entry->d_name,"..")==0
        )
            continue;


        if(should_ignore(entry->d_name))
            continue;



        char path[1024];


        snprintf(
            path,
            sizeof(path),
            "%s/%s",
            directory,
            entry->d_name
        );



        struct stat st;


        stat(path,&st);



        if(S_ISDIR(st.st_mode))
        {

            scanner_scan_directory(
                list,
                path
            );

        }
        else
        {

            if(has_c_extension(entry->d_name))
            {
                add_source(
                    list,
                    path
                );
            }

        }

    }


    closedir(dir);


#endif


    return 1;
}



void scanner_print(
    const SourceList* list
)
{

    printf(
        "Sources found: %d\n",
        list->count
    );


    for(int i=0;i<list->count;i++)
    {
        printf(
            " - %s\n",
            list->files[i]
        );
    }

}
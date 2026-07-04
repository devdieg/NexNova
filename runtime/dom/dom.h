#ifndef NOVA_DOM_H
#define NOVA_DOM_H

#include <stddef.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef enum
{
    NOVA_NODE_DOCUMENT,
    NOVA_NODE_ELEMENT,
    NOVA_NODE_TEXT,
    NOVA_NODE_COMMENT
} NovaNodeType;

typedef struct NovaAttribute
{
    char* name;
    char* value;

    struct NovaAttribute* next;
} NovaAttribute;

typedef struct NovaNode
{
    NovaNodeType type;

    char* tag_name;
    char* text;

    NovaAttribute* attributes;

    struct NovaNode* parent;

    struct NovaNode** children;

    size_t child_count;
    size_t child_capacity;

    void* style;
    void* layout;

} NovaNode;

typedef struct
{
    NovaNode* root;
} NovaDocument;

NovaDocument* dom_document_create(void);
void dom_document_destroy(NovaDocument* document);

NovaNode* dom_node_create(
    NovaNodeType type,
    const char* tag);

NovaNode* dom_text_create(
    const char* text);

void dom_node_destroy(
    NovaNode* node);

void dom_append_child(
    NovaNode* parent,
    NovaNode* child);

void dom_remove_child(
    NovaNode* parent,
    NovaNode* child);

NovaNode* dom_get_child(
    NovaNode* node,
    size_t index);

size_t dom_child_count(
    const NovaNode* node);

void dom_set_attribute(
    NovaNode* node,
    const char* name,
    const char* value);

const char* dom_get_attribute(
    const NovaNode* node,
    const char* name);

void dom_remove_attribute(
    NovaNode* node,
    const char* name);

NovaNode* dom_find_by_id(
    NovaNode* node,
    const char* id);

#ifdef __cplusplus
}
#endif

#endif
#include "dom.h"

#include <stdlib.h>
#include <string.h>

static char* dom_strdup(const char* text)
{
    if (!text)
        return NULL;

    size_t length = strlen(text);

    char* result = malloc(length + 1);

    if (!result)
        return NULL;

    memcpy(result, text, length + 1);

    return result;
}

NovaDocument* dom_document_create(void)
{
    NovaDocument* document = malloc(sizeof(NovaDocument));

    if (!document)
        return NULL;

    document->root = NULL;

    return document;
}

NovaNode* dom_node_create(
    NovaNodeType type,
    const char* tag)
{
    NovaNode* node = calloc(1, sizeof(NovaNode));

    if (!node)
        return NULL;

    node->type = type;
    node->tag_name = dom_strdup(tag);

    return node;
}

NovaNode* dom_text_create(
    const char* text)
{
    NovaNode* node = dom_node_create(
        NOVA_NODE_TEXT,
        NULL);

    if (!node)
        return NULL;

    node->text = dom_strdup(text);

    return node;
}

void dom_append_child(
    NovaNode* parent,
    NovaNode* child)
{
    if (!parent || !child)
        return;

    if (parent->child_count >= parent->child_capacity)
    {
        size_t new_capacity =
            parent->child_capacity == 0
            ? 4
            : parent->child_capacity * 2;

        NovaNode** new_children =
            realloc(parent->children,
                    sizeof(NovaNode*) * new_capacity);

        if (!new_children)
            return;

        parent->children = new_children;
        parent->child_capacity = new_capacity;
    }

    parent->children[parent->child_count++] = child;
    child->parent = parent;
}

NovaNode* dom_get_child(
    NovaNode* node,
    size_t index)
{
    if (!node)
        return NULL;

    if (index >= node->child_count)
        return NULL;

    return node->children[index];
}

size_t dom_child_count(
    const NovaNode* node)
{
    return node ? node->child_count : 0;
}

void dom_set_attribute(
    NovaNode* node,
    const char* name,
    const char* value)
{
    if (!node || !name)
        return;

    NovaAttribute* attribute =
        node->attributes;

    while (attribute)
    {
        if (strcmp(attribute->name, name) == 0)
        {
            free(attribute->value);
            attribute->value = dom_strdup(value);
            return;
        }

        attribute = attribute->next;
    }

    attribute = malloc(sizeof(NovaAttribute));

    if (!attribute)
        return;

    attribute->name = dom_strdup(name);
    attribute->value = dom_strdup(value);

    attribute->next = node->attributes;

    node->attributes = attribute;
}

const char* dom_get_attribute(
    const NovaNode* node,
    const char* name)
{
    if (!node || !name)
        return NULL;

    NovaAttribute* attribute =
        node->attributes;

    while (attribute)
    {
        if (strcmp(attribute->name, name) == 0)
            return attribute->value;

        attribute = attribute->next;
    }

    return NULL;
}

void dom_remove_attribute(
    NovaNode* node,
    const char* name)
{
    if (!node || !name)
        return;

    NovaAttribute* previous = NULL;
    NovaAttribute* current = node->attributes;

    while (current)
    {
        if (strcmp(current->name, name) == 0)
        {
            if (previous)
                previous->next = current->next;
            else
                node->attributes = current->next;

            free(current->name);
            free(current->value);
            free(current);

            return;
        }

        previous = current;
        current = current->next;
    }
}

NovaNode* dom_find_by_id(
    NovaNode* node,
    const char* id)
{
    if (!node || !id)
        return NULL;

    const char* value =
        dom_get_attribute(node, "id");

    if (value && strcmp(value, id) == 0)
        return node;

    for (size_t i = 0; i < node->child_count; i++)
    {
        NovaNode* result =
            dom_find_by_id(node->children[i], id);

        if (result)
            return result;
    }

    return NULL;
}

void dom_remove_child(
    NovaNode* parent,
    NovaNode* child)
{
    if (!parent || !child)
        return;

    for (size_t i = 0; i < parent->child_count; i++)
    {
        if (parent->children[i] == child)
        {
            for (size_t j = i; j + 1 < parent->child_count; j++)
                parent->children[j] = parent->children[j + 1];

            parent->child_count--;
            child->parent = NULL;
            return;
        }
    }
}

void dom_node_destroy(
    NovaNode* node)
{
    if (!node)
        return;

    while (node->child_count)
        dom_node_destroy(node->children[--node->child_count]);

    free(node->children);

    while (node->attributes)
    {
        NovaAttribute* next = node->attributes->next;

        free(node->attributes->name);
        free(node->attributes->value);
        free(node->attributes);

        node->attributes = next;
    }

    free(node->tag_name);
    free(node->text);

    free(node);
}

void dom_document_destroy(
    NovaDocument* document)
{
    if (!document)
        return;

    dom_node_destroy(document->root);

    free(document);
}
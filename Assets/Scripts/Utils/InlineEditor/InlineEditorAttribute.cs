using UnityEngine;

public class InlineEditorAttribute : PropertyAttribute
{
    public bool expandedByDefault = true;

    public InlineEditorAttribute(bool expandedByDefault = true)
    {
        this.expandedByDefault = expandedByDefault;
    }
}
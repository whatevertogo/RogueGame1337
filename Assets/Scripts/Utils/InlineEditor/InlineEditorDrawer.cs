using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(InlineEditorAttribute))]
public class InlineEditorDrawer : PropertyDrawer
{
    // 使用字典存储每个属性的折叠状态（避免多个属性共享同一个foldout）
    private static readonly System.Collections.Generic.Dictionary<string, bool> FoldoutStates = new();

    private bool GetFoldout(SerializedProperty property)
    {
        string key = property.propertyPath;
        if (! FoldoutStates.ContainsKey(key))
        {
            FoldoutStates[key] = true;
        }
        return FoldoutStates[key];
    }

    private void SetFoldout(SerializedProperty property, bool value)
    {
        FoldoutStates[property.propertyPath] = value;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        bool isFoldout = GetFoldout(property);
        
        // 基础高度：折叠栏
        float height = EditorGUIUtility. singleLineHeight;
        
        if (! isFoldout)
        {
            return height + 4f; // 折叠时只返回折叠栏高度
        }
        
        // 展开时：折叠栏 + 对象选择框
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        
        // 如果有引用对象，计算其所有属性的高度
        if (property.objectReferenceValue != null)
        {
            SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue);
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                
                // 跳过 m_Script 字段
                if (iterator.name == "m_Script") continue;
                
                height += EditorGUI.GetPropertyHeight(iterator, true);
                height += EditorGUIUtility.standardVerticalSpacing;
            }
        }

        return height + 8f; // 额外边距
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 开始属性绘制
        EditorGUI.BeginProperty(position, label, property);
        
        bool isFoldout = GetFoldout(property);
        
        // 绘制折叠栏
        Rect foldoutRect = new Rect(
            position.x, 
            position.y, 
            position.width, 
            EditorGUIUtility. singleLineHeight
        );
        
        bool newFoldout = EditorGUI.Foldout(foldoutRect, isFoldout, label, true);
        if (newFoldout != isFoldout)
        {
            SetFoldout(property, newFoldout);
        }

        if (!newFoldout)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;

        float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        // 绘制对象选择框
        Rect objFieldRect = new Rect(
            position. x,
            y,
            position.width,
            EditorGUIUtility.singleLineHeight
        );

        EditorGUI.PropertyField(objFieldRect, property, GUIContent.none);
        y += EditorGUIUtility. singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        // 绘制引用对象的所有属性
        if (property.objectReferenceValue != null)
        {
            SerializedObject serialized = new SerializedObject(property.objectReferenceValue);
            serialized.Update();
            
            SerializedProperty iterator = serialized.GetIterator();
            bool enterChildren = true;

            // 绘制背景框
            float contentStartY = y;
            float contentHeight = 0f;
            
            // 先计算内容高度
            SerializedProperty tempIterator = serialized.GetIterator();
            bool tempEnter = true;
            while (tempIterator.NextVisible(tempEnter))
            {
                tempEnter = false;
                if (tempIterator.name == "m_Script") continue;
                contentHeight += EditorGUI.GetPropertyHeight(tempIterator, true);
                contentHeight += EditorGUIUtility.standardVerticalSpacing;
            }
            
            // 绘制背景
            if (contentHeight > 0)
            {
                Rect boxRect = new Rect(
                    position.x + EditorGUI.indentLevel * 15f - 2f,
                    contentStartY - 2f,
                    position.width - EditorGUI.indentLevel * 15f + 4f,
                    contentHeight + 4f
                );
                EditorGUI.DrawRect(boxRect, new Color(0.1f, 0.1f, 0.1f, 0.2f));
            }

            // 绘制属性
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                // 跳过 m_Script 字段
                if (iterator.name == "m_Script") continue;

                float propHeight = EditorGUI.GetPropertyHeight(iterator, true);
                Rect propRect = new Rect(position.x, y, position.width, propHeight);

                EditorGUI.PropertyField(propRect, iterator, true);

                y += propHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            serialized.ApplyModifiedProperties();
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }
}
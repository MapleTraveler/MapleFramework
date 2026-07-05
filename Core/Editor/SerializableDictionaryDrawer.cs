using Maple.Core;
using UnityEditor;
using UnityEngine;

namespace Maple.Core.Editor
{
    [CustomPropertyDrawer(typeof(SerializableDictionary<,>), true)]
    public class SerializableDictionaryDrawer : PropertyDrawer
    {
        private const float LineVSpace = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var so = property.serializedObject;
            so.Update();

            var keysProp = property.FindPropertyRelative("keys");
            var valuesProp = property.FindPropertyRelative("values");

            EditorGUI.BeginProperty(position, label, property);
            var foldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldRect, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                position.y += EditorGUIUtility.singleLineHeight + LineVSpace;

                int count = Mathf.Max(keysProp.arraySize, valuesProp.arraySize);
                if (keysProp.arraySize != count) keysProp.arraySize = count;
                if (valuesProp.arraySize != count) valuesProp.arraySize = count;

                var headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(new Rect(headerRect.x, headerRect.y, headerRect.width * 0.5f, headerRect.height), "Key");
                EditorGUI.LabelField(new Rect(headerRect.x + headerRect.width * 0.5f, headerRect.y, headerRect.width * 0.5f, headerRect.height), "Value");
                position.y += EditorGUIUtility.singleLineHeight + LineVSpace;

                for (int i = 0; i < count; i++)
                {
                    var keyProp = keysProp.GetArrayElementAtIndex(i);
                    var valProp = valuesProp.GetArrayElementAtIndex(i);

                    float kh = EditorGUI.GetPropertyHeight(keyProp, GUIContent.none, true);
                    float vh = EditorGUI.GetPropertyHeight(valProp, GUIContent.none, true);
                    float rowH = Mathf.Max(kh, vh);

                    var keyRect = new Rect(position.x, position.y, position.width * 0.5f - 2, rowH);
                    var valRect = new Rect(position.x + position.width * 0.5f + 2, position.y, position.width * 0.5f - 2, rowH);

                    EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none, true);
                    EditorGUI.PropertyField(valRect, valProp, GUIContent.none, true);

                    position.y += rowH + LineVSpace;
                }

                var btnRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                var halfW = btnRect.width * 0.5f;

                if (GUI.Button(new Rect(btnRect.x, btnRect.y, halfW - 2, btnRect.height), "Add"))
                {
                    Undo.RecordObject(so.targetObject, "Add Dictionary Entry");
                    keysProp.InsertArrayElementAtIndex(keysProp.arraySize);
                    valuesProp.InsertArrayElementAtIndex(valuesProp.arraySize);
                    InitDefault(keysProp.GetArrayElementAtIndex(keysProp.arraySize - 1));
                    InitDefault(valuesProp.GetArrayElementAtIndex(valuesProp.arraySize - 1));
                    EditorUtility.SetDirty(so.targetObject);
                }

                if (GUI.Button(new Rect(btnRect.x + halfW + 2, btnRect.y, halfW - 2, btnRect.height), "Remove Last"))
                {
                    Undo.RecordObject(so.targetObject, "Remove Dictionary Entry");
                    if (keysProp.arraySize > 0) keysProp.DeleteArrayElementAtIndex(keysProp.arraySize - 1);
                    if (valuesProp.arraySize > 0) valuesProp.DeleteArrayElementAtIndex(valuesProp.arraySize - 1);
                    EditorUtility.SetDirty(so.targetObject);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
            so.ApplyModifiedProperties();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float h = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded) return h;

            var keysProp = property.FindPropertyRelative("keys");
            var valuesProp = property.FindPropertyRelative("values");
            int count = Mathf.Max(keysProp.arraySize, valuesProp.arraySize);

            h += LineVSpace;
            h += EditorGUIUtility.singleLineHeight + LineVSpace;

            for (int i = 0; i < count; i++)
            {
                var keyProp = keysProp.GetArrayElementAtIndex(Mathf.Min(i, keysProp.arraySize - 1));
                var valProp = valuesProp.GetArrayElementAtIndex(Mathf.Min(i, valuesProp.arraySize - 1));
                float kh = keyProp != null ? EditorGUI.GetPropertyHeight(keyProp, GUIContent.none, true) : EditorGUIUtility.singleLineHeight;
                float vh = valProp != null ? EditorGUI.GetPropertyHeight(valProp, GUIContent.none, true) : EditorGUIUtility.singleLineHeight;
                h += Mathf.Max(kh, vh) + LineVSpace;
            }

            h += EditorGUIUtility.singleLineHeight + LineVSpace;
            return h;
        }

        private static void InitDefault(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.String:
                    if (string.IsNullOrEmpty(prop.stringValue)) prop.stringValue = "NewKey";
                    break;
                case SerializedPropertyType.ObjectReference:
                    prop.objectReferenceValue = null;
                    break;
            }
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class TagManagerUpdater
    {
        [MenuItem("Game/Setup/Tags and Layers")]
        public static void Setup()
        {
            var tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            var so = new SerializedObject(tagManager);
            AddToStringArray(so, "tags", "Player");
            AddToStringArray(so, "tags", "MainCamera");
            AddToStringArray(so, "layers", "Player");
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log("TAGS_SETUP_DONE");
        }

        static void AddToStringArray(SerializedObject so, string property, string value)
        {
            var prop = so.FindProperty(property);
            for (int i = 0; i < prop.arraySize; i++) {
                if (prop.GetArrayElementAtIndex(i).stringValue == value) { return; }
            }
            for (int i = 0; i < prop.arraySize; i++) {
                var el = prop.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(el.stringValue)) { el.stringValue = value; return; }
            }
            prop.InsertArrayElementAtIndex(prop.arraySize);
            prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = value;
        }
    }
}

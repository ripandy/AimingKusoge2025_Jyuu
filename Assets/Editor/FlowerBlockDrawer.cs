using Domain.Chapters.BeeHarvest;
using UnityEditor;
using UnityEngine;

namespace YukiQuest.EditorTools
{
    /// <summary>
    /// Draws a <see cref="FlowerBlock"/> as a single row.
    /// </summary>
    /// <remarks>
    /// The default struct drawer spends a foldout and three lines per element. A level is a few
    /// dozen blocks read as a strip, so the default turns the one thing you need to see — the shape
    /// of the level — into several screens of scrolling.
    /// </remarks>
    [CustomPropertyDrawer(typeof(FlowerBlock))]
    public class FlowerBlockDrawer : PropertyDrawer
    {
        private const float Gap = 4f;
        private const float IndexWidth = 62f;
        private const float NectarWidth = 54f;
        private const float ScaleWidth = 48f;
        private const float MiniLabelWidth = 14f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var indent = EditorGUI.indentLevel;
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUI.indentLevel = 0;

            var type = property.FindPropertyRelative("type");
            var nectar = property.FindPropertyRelative("nectar");
            var scale = property.FindPropertyRelative("scale");

            var x = position.x;
            var y = position.y;
            var height = EditorGUIUtility.singleLineHeight;

            // "Element 12" — the block index is the one thing that tells you where you are.
            EditorGUI.LabelField(new Rect(x, y, IndexWidth, height), label);
            x += IndexWidth + Gap;

            var typeWidth = Mathf.Max(60f,
                position.width - IndexWidth - NectarWidth - ScaleWidth - MiniLabelWidth * 2 - Gap * 5);
            EditorGUI.PropertyField(new Rect(x, y, typeWidth, height), type, GUIContent.none);
            x += typeWidth + Gap;

            // An empty block has neither nectar nor a size, so hide the fields rather than invite
            // values that the generator will ignore.
            // intValue, not enumValueIndex: the latter is a position in the declaration list, which
            // stops matching the enum's own numbers the moment FlowerType gets a gap.
            var isEmpty = type.intValue == (int)FlowerType.None;
            if (!isEmpty)
            {
                EditorGUIUtility.labelWidth = MiniLabelWidth;

                EditorGUI.PropertyField(
                    new Rect(x, y, NectarWidth + MiniLabelWidth, height), nectar, new GUIContent("N"));
                x += NectarWidth + MiniLabelWidth + Gap;

                EditorGUI.PropertyField(
                    new Rect(x, y, ScaleWidth + MiniLabelWidth, height), scale, new GUIContent("×"));
            }

            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight;
    }
}

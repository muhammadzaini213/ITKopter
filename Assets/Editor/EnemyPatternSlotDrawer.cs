using UnityEditor;
using UnityEngine;

// Shows only the fields relevant to the selected pattern inside each
// EnemyPatternSlot array element (patterns[] and phase2Patterns[]).
// Data layout unchanged: display only, plus default values applied
// when the pattern dropdown changes.
[CustomPropertyDrawer(typeof(EnemyPatternSlot))]
public class EnemyPatternSlotDrawer : PropertyDrawer
{
    // Fields drawn for every pattern, in order.
    // "pattern" must stay in this list. Add shared fields here
    // (for example the new aiming / rotation fields).
    private static readonly string[] CommonFields =
    {
        "enabled",
        "pattern",
        "startDelay",
    };

    // Index = EnemyShotPattern enum order. Keep in sync.
    private static readonly string[][] PatternFields =
    {
        /* Aimed       */ new[] { "count", "spread", "speed", "interval" },
        /* Burst       */ new[] { "count", "burstDelay", "speed", "cooldown" },
        /* Spiral      */ new[] { "arms", "angleStep", "speed", "interval" },
        /* Ring        */ new[] { "count", "speed", "repeat", "repeatDelay", "cooldown" },
        /* Wall        */ new[] { "count", "spacingX", "randomGap", "speed", "interval" },
        /* Fan         */ new[] { "count", "arc", "aimed", "speed", "interval" },
        /* Heavy       */ new[] { "count", "arc", "aimed", "speed", "scale", "interval" },
        /* SineBullet  */ new[] { "count", "spacingX", "speed", "amplitude", "frequency", "interval" },
        /* DoubleAimed */ new[] { "offsetX", "speed", "interval" },
        /* Scatter     */ new[] { "count", "cone", "speedMin", "speedMax", "interval" },
    };

    // Default values applied when the pattern is changed in the Inspector.
    // Same index order as PatternFields. Bools: 1 = true, 0 = false.
    private static readonly (string name, float value)[][] PatternDefaults =
    {
        /* Aimed */ new[]
        {
            ("count", 1f), ("spread", 0f), ("speed", 5f), ("interval", 1.5f),
        },
        /* Burst */ new[]
        {
            ("count", 3f), ("burstDelay", 0.12f), ("speed", 7f), ("cooldown", 2.5f),
        },
        /* Spiral */ new[]
        {
            ("arms", 2f), ("angleStep", 12f), ("speed", 3.5f), ("interval", 0.12f),
        },
        /* Ring */ new[]
        {
            ("count", 12f), ("speed", 2.5f), ("repeat", 2f),
            ("repeatDelay", 1.2f), ("cooldown", 2f),
        },
        /* Wall */ new[]
        {
            ("count", 7f), ("spacingX", 0.8f), ("randomGap", 1f),
            ("speed", 2.5f), ("interval", 2f),
        },
        /* Fan */ new[]
        {
            ("count", 5f), ("arc", 60f), ("aimed", 0f),
            ("speed", 4f), ("interval", 1.4f),
        },
        /* Heavy */ new[]
        {
            ("count", 3f), ("arc", 20f), ("speed", 2f),
            ("scale", 1.5f), ("interval", 1f),
        },
        /* SineBullet */ new[]
        {
            ("count", 2f), ("spacingX", 0.4f), ("speed", 3.5f),
            ("amplitude", 0.6f), ("frequency", 2f), ("interval", 1.2f),
        },
        /* DoubleAimed */ new[]
        {
            ("offsetX", 1f), ("speed", 5f), ("interval", 3f),
        },
        /* Scatter */ new[]
        {
            ("count", 8f), ("cone", 120f), ("speedMin", 2f),
            ("speedMax", 5f), ("interval", 0.7f),
        },
    };

    private static float Line => EditorGUIUtility.singleLineHeight;
    private static float Gap => EditorGUIUtility.standardVerticalSpacing;

    // Returns the pattern-specific field names. outOfSync is true when the
    // enum value has no entry in PatternFields (enum and table out of sync).
    private static string[] GetPatternFields(
        SerializedProperty pattern, out bool outOfSync)
    {
        outOfSync = false;

        if (pattern == null)
            return System.Array.Empty<string>();

        int index = pattern.enumValueIndex;
        if (index < 0 || index >= PatternFields.Length)
        {
            outOfSync = true;
            return System.Array.Empty<string>();
        }

        return PatternFields[index];
    }

    // Sum of the heights of all existing fields, each preceded by a gap.
    // Missing fields are skipped so they leave no empty space.
    private static float SectionHeight(
        SerializedProperty parent, string[] names)
    {
        float height = 0f;

        foreach (string name in names)
        {
            var prop = parent.FindPropertyRelative(name);
            if (prop == null)
                continue;

            height += Gap + EditorGUI.GetPropertyHeight(prop, true);
        }

        return height;
    }

    public override float GetPropertyHeight(
        SerializedProperty property, GUIContent label)
    {
        // Collapsed: just the foldout header line.
        if (!property.isExpanded)
            return Line;

        var pattern = property.FindPropertyRelative("pattern");
        var patternFields = GetPatternFields(pattern, out bool outOfSync);

        float height = Line;
        height += SectionHeight(property, CommonFields);
        height += SectionHeight(property, patternFields);

        if (outOfSync)
            height += Gap + Line * 2f;

        return height;
    }

    public override void OnGUI(
        Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var pattern = property.FindPropertyRelative("pattern");
        int indexBefore = pattern != null ? pattern.enumValueIndex : 0;

        // Resolve the field list once, before drawing. Changing the pattern
        // below only affects the layout on the next repaint.
        var patternFields = GetPatternFields(pattern, out bool outOfSync);

        // Foldout header: "Element 0: Aimed"
        string patternName = string.Empty;
        if (pattern != null)
        {
            int index = pattern.enumValueIndex;
            if (index >= 0 && index < pattern.enumDisplayNames.Length)
                patternName = pattern.enumDisplayNames[index];
        }

        Rect row = new Rect(position.x, position.y, position.width, Line);

        property.isExpanded = EditorGUI.Foldout(
            row, property.isExpanded,
            $"{label.text}: {patternName}", true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            row.y += Line;
            DrawSection(ref row, property, CommonFields);
            DrawSection(ref row, property, patternFields);

            if (outOfSync)
            {
                row.y += Gap;
                row.height = Line * 2f;
                EditorGUI.HelpBox(
                    row,
                    "No field list for this pattern. Update PatternFields.",
                    MessageType.Warning);
            }

            EditorGUI.indentLevel--;

            // If the dropdown changed this repaint, fill in that pattern's defaults.
            if (pattern != null && pattern.enumValueIndex != indexBefore)
                ApplyDefaults(property, pattern.enumValueIndex);
        }

        EditorGUI.EndProperty();
    }

    // Draws each existing field on its own row using its real height.
    // 'row' is advanced past everything drawn.
    private static void DrawSection(
        ref Rect row, SerializedProperty parent, string[] names)
    {
        foreach (string name in names)
        {
            var prop = parent.FindPropertyRelative(name);
            if (prop == null)
                continue;

            float h = EditorGUI.GetPropertyHeight(prop, true);

            row.y += Gap;
            row.height = h;
            EditorGUI.PropertyField(row, prop, true);
            row.y += h;
        }
    }

    // Writes the default values of the given pattern into the slot.
    // Only fields that exist on EnemyPatternSlot are touched.
    private static void ApplyDefaults(SerializedProperty slot, int patternIndex)
    {
        if (patternIndex < 0 || patternIndex >= PatternDefaults.Length)
            return;

        foreach (var (name, value) in PatternDefaults[patternIndex])
        {
            var prop = slot.FindPropertyRelative(name);
            if (prop == null)
                continue;

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    prop.intValue = Mathf.RoundToInt(value);
                    break;
                case SerializedPropertyType.Float:
                    prop.floatValue = value;
                    break;
                case SerializedPropertyType.Boolean:
                    prop.boolValue = value > 0.5f;
                    break;
            }
        }
    }
}
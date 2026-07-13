using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(BallEffectData))]
public class BallEffectDataDrawer : PropertyDrawer
{
    const float Space = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;

        float y = position.y + EditorGUIUtility.singleLineHeight + Space;
        List<SerializedProperty> visibleProperties = GetVisibleProperties(property);

        for (int i = 0; i < visibleProperties.Count; i++)
        {
            DrawProperty(position, ref y, visibleProperties[i]);
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        float height = EditorGUIUtility.singleLineHeight;
        List<SerializedProperty> visibleProperties = GetVisibleProperties(property);

        for (int i = 0; i < visibleProperties.Count; i++)
        {
            height += Space + EditorGUI.GetPropertyHeight(visibleProperties[i], true);
        }

        return height;
    }

    List<SerializedProperty> GetVisibleProperties(SerializedProperty property)
    {
        List<SerializedProperty> properties = new List<SerializedProperty>();

        SerializedProperty effectType = property.FindPropertyRelative("effectType");
        BallEffectType type = (BallEffectType)effectType.enumValueIndex;

        Add(properties, effectType);
        Add(properties, property.FindPropertyRelative("triggerChance"));
        Add(properties, property.FindPropertyRelative("cooldown"));
        Add(properties, property.FindPropertyRelative("maxTriggerCount"));

        AddEffectValueProperties(properties, property, type);

        return properties;
    }

    void AddEffectValueProperties(List<SerializedProperty> properties, SerializedProperty property, BallEffectType type)
    {
        switch (type)
        {
            case BallEffectType.HealDurability:
            case BallEffectType.SplitBall:
            case BallEffectType.AddRandomScoreValue:
            case BallEffectType.AddGold:
                Add(properties, property.FindPropertyRelative("minValue"));
                Add(properties, property.FindPropertyRelative("maxValue"));
                break;

            case BallEffectType.StackCashOutChips:
                Add(properties, property.FindPropertyRelative("chipsPerStack"));
                break;

            case BallEffectType.StackCashOutMultiplier:
                Add(properties, property.FindPropertyRelative("multiplierPerStack"));
                break;

            case BallEffectType.StackChips:
                Add(properties, property.FindPropertyRelative("targetStack"));
                Add(properties, property.FindPropertyRelative("chipsIncrease"));
                break;

            case BallEffectType.StackMultiplier:
                Add(properties, property.FindPropertyRelative("targetStack"));
                Add(properties, property.FindPropertyRelative("multiplierIncrease"));
                break;

            case BallEffectType.OverheatChips:
                Add(properties, property.FindPropertyRelative("baseChipsIncrease"));
                Add(properties, property.FindPropertyRelative("chipsIncreasePerOverheat"));
                AddOverheatRiskProperties(properties, property);
                break;

            case BallEffectType.OverheatMultiplier:
                Add(properties, property.FindPropertyRelative("baseMultiplierIncrease"));
                Add(properties, property.FindPropertyRelative("multiplierIncreasePerOverheat"));
                AddOverheatRiskProperties(properties, property);
                break;

            case BallEffectType.StackCashOutEffect:
                AddRewardType(properties, property);
                if (RewardNeedsValue(property))
                {
                    Add(properties, property.FindPropertyRelative("rewardValuePerStack"));
                }
                break;

            case BallEffectType.StackEffect:
                Add(properties, property.FindPropertyRelative("targetStack"));
                AddRewardType(properties, property);
                if (RewardNeedsValue(property))
                {
                    Add(properties, property.FindPropertyRelative("rewardMinValue"));
                    Add(properties, property.FindPropertyRelative("rewardMaxValue"));
                }
                break;

            case BallEffectType.OverheatEffect:
                AddRewardType(properties, property);
                if (RewardNeedsValue(property))
                {
                    Add(properties, property.FindPropertyRelative("baseRewardValue"));
                    Add(properties, property.FindPropertyRelative("rewardValuePerOverheat"));
                }
                AddOverheatRiskProperties(properties, property);
                break;
        }
    }

    void AddRewardType(List<SerializedProperty> properties, SerializedProperty property)
    {
        Add(properties, property.FindPropertyRelative("rewardEffectType"));
    }

    void AddOverheatRiskProperties(List<SerializedProperty> properties, SerializedProperty property)
    {
        Add(properties, property.FindPropertyRelative("selfDestroyStartOverheat"));
        Add(properties, property.FindPropertyRelative("selfDestroyChance"));
    }

    bool RewardNeedsValue(SerializedProperty property)
    {
        SerializedProperty rewardType = property.FindPropertyRelative("rewardEffectType");
        BallEffectRewardType type = (BallEffectRewardType)rewardType.enumValueIndex;

        return type != BallEffectRewardType.DestroySelf;
    }

    void Add(List<SerializedProperty> properties, SerializedProperty property)
    {
        if (property != null)
        {
            properties.Add(property);
        }
    }

    void DrawProperty(Rect position, ref float y, SerializedProperty property)
    {
        float height = EditorGUI.GetPropertyHeight(property, true);
        Rect row = new Rect(position.x, y, position.width, height);

        EditorGUI.PropertyField(row, property, true);
        y += height + Space;
    }
}

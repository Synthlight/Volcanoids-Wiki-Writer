using System.IO;
using System.Reflection;
using Base_Mod;
using Base_Mod.Models;
using JetBrains.Annotations;
using UnityEngine;

namespace Wiki_Writer.Wiki_Stuff;

public static class WriteQuestOrder {
    private static readonly FieldInfo QUEST_MANAGER_M_QUESTS = typeof(QuestManager).GetField("m_quests", BindingFlags.NonPublic | BindingFlags.Instance);

    [OnIslandSceneLoaded]
    [UsedImplicitly]
    public static void Go() {
        var msg = Plugin.GetHeader() +
                  "Name | Priority | Type\r\n" +
                  "--- | --- | ---\r\n";

        var questManagers = Resources.FindObjectsOfTypeAll<QuestManager>();
        if (questManagers != null) {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var questManager in questManagers) {
                var quests = questManager.GetPrivateField<Quest[]>(QUEST_MANAGER_M_QUESTS);
                if (quests?.Length > 0) {
                    // ReSharper disable once LoopCanBeConvertedToQuery
                    foreach (var quest in quests) {
                        msg += $"{quest.GetName()} | {quest.Priority} | {GetQuestType(quest)}\r\n";
                    }
                }
            }
        }

        File.WriteAllText(Plugin.BASE_OUTPUT_PATH + "Quest Order.txt", msg);
    }

    private static string GetQuestType(Quest quest) {
        // ReSharper disable once ConvertTypeCheckPatternToNullCheck
        return quest switch {
            EventQuest _ => AddTypeIfNeeded("EventQuest", quest),
            PlayerQuest _ => AddTypeIfNeeded("PlayerQuest", quest),
            Quest _ => AddTypeIfNeeded("Quest", quest),
            _ => quest.GetType().ToString()
        };
    }

    private static string AddTypeIfNeeded(string text, Quest quest) {
        var questType = quest.GetType();

        return questType.ToString() == text ? text : $"{text} ({questType})";
    }
}
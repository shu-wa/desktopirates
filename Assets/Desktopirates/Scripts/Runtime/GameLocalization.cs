using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Desktopirates
{
    public enum GameLanguage { English, Japanese }

    /// <summary>Small, deliberate bilingual layer. Nautical nouns stay English where that supports the game's tone.</summary>
    public static class GameLocalization
    {
        private const string PreferenceKey = "game_language";
        private static bool languageResolved;
        private static GameLanguage resolvedLanguage;
        private static readonly Dictionary<string, string> Japanese = new Dictionary<string, string>
        {
            { "VOLUME", "音量" }, { "SIZE", "表示サイズ" }, { "MUTE", "消音" }, { "UNMUTE", "消音解除" },
            { "MAP", "海図" }, { "BAG", "船倉" }, { "CAPTAIN LOG", "船長記録" }, { "LANGUAGE", "言語" }, { "EXIT", "終了" },
            { "LOCAL", "周辺" }, { "WIDE", "広域" }, { "BACK", "戻る" }, { "BACK TO PORT", "港へ戻る" },
            { "SAIL", "出航" }, { "DEPART HARBOR", "出航する" }, { "SHIPYARD  CUSTOMIZE", "SHIPYARD  船を改装" },
            { "REPAIR  +10%  20G", "船体修理  +10%  20G" }, { "BUY FOOD  +10  18G", "食料  +10  18G" },
            { "BUY WATER  +10  14G", "飲料水  +10  14G" }, { "GUN DECK", "GUN DECK" },
            { "SYSTEMS", "船体設備" }, { "CREW", "船員" }, { "MOUNT / STORE", "搭載 / 倉庫へ" },
            { "DAMAGE", "威力" }, { "RELOAD", "装填" }, { "RANGE", "射程" }, { "AMMO", "砲弾" },
            { "CAPACITY", "積載" }, { "PROPULSION", "推進" }, { "ARMOR", "装甲" }, { "TURNING", "旋回" },
            { "GUNS", "艦砲" }, { "HIRE CREW", "船員を雇う" }, { "SHIP LEVEL", "SHIP LEVEL" },
            { "PERKS", "パーク" }, { "FOOD", "食料" }, { "WATER", "飲料水" },
            { "ROUND SHOT", "通常弾" }, { "CHAIN SHOT", "鎖弾" }, { "FIRE SHOT", "焼夷弾" }
        };

        public static GameLanguage Current
        {
            get
            {
                if (languageResolved) return resolvedLanguage;
                string[] arguments = Environment.GetCommandLineArgs();
                for (int i = 0; i < arguments.Length; i++)
                {
                    if (arguments[i].Equals("--lang=ja", StringComparison.OrdinalIgnoreCase))
                    {
                        resolvedLanguage = GameLanguage.Japanese;
                        languageResolved = true;
                        return resolvedLanguage;
                    }
                    if (arguments[i].Equals("--lang=en", StringComparison.OrdinalIgnoreCase))
                    {
                        resolvedLanguage = GameLanguage.English;
                        languageResolved = true;
                        return resolvedLanguage;
                    }
                }
                int fallback = Application.systemLanguage == SystemLanguage.Japanese ? 1 : 0;
                resolvedLanguage = (GameLanguage)Mathf.Clamp(PlayerPrefs.GetInt(PreferenceKey, fallback), 0, 1);
                languageResolved = true;
                return resolvedLanguage;
            }
        }

        public static bool IsJapanese => Current == GameLanguage.Japanese;
        public static string Choose(string english, string japanese) => IsJapanese ? japanese : english;

        public static string Text(string english)
            => IsJapanese && Japanese.TryGetValue(english, out string localized) ? localized : english;

        public static void Toggle()
        {
            resolvedLanguage = IsJapanese ? GameLanguage.English : GameLanguage.Japanese;
            languageResolved = true;
            PlayerPrefs.SetInt(PreferenceKey, (int)resolvedLanguage);
            PlayerPrefs.Save();
        }
    }

    public sealed class LocalizedUiText : MonoBehaviour
    {
        private Text target;
        private string english;

        public static void Bind(Text text, string englishText)
        {
            if (text == null) return;
            LocalizedUiText label = text.GetComponent<LocalizedUiText>() ?? text.gameObject.AddComponent<LocalizedUiText>();
            label.target = text;
            label.english = englishText;
            label.Refresh();
        }

        public void Refresh()
        {
            if (target == null) target = GetComponent<Text>();
            if (target != null && !string.IsNullOrEmpty(english)) target.text = GameLocalization.Text(english);
        }
    }
}

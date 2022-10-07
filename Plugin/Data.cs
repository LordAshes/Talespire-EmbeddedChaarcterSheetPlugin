using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

namespace LordAshes
{
    public partial class TaleSpireEmbeddedCharacterSheetPlugin
    {
        public enum GuiElementType
        {
            label = 0,
            button = 1,
        }

        public class GuiLocation
        {
            public int x { get; set; } = 0;
            public int y { get; set; } = 0;
        }

        public class GuiSize
        {
            public int w { get; set; } = 0;
            public int h { get; set; } = 0;
        }

        public class GuiElement
        {
            public GuiElementType type { get; set; } = GuiElementType.label;
            public GuiLocation position { get; set; } = new GuiLocation();
            public GuiSize size { get; set; } = new GuiSize();
            public string style { get; set; } = "";
            public string content { get; set; } = "";
            public string roll { get; set; } = "";
            [NonSerialized]
            public Texture2D _texture = null;
            [NonSerialized]
            public GUIStyle _style = new GUIStyle();

            public GuiElement()
            {
                _style.normal.textColor = defaultEntryTextColor.Value;
                _style.fontSize = defaultEntryTextSize.Value;
                _style.fontStyle = FontStyle.Normal;
                _style.richText = false;
                _style.wordWrap = false;
            }
        }

        public class GuiLayout
        {
            public GuiLocation position = new GuiLocation() { x = 10, y = 40 };
            public GuiSize size = new GuiSize() { w = Screen.width - 20, h = Screen.height - 20 };
            public List<GuiElement> elements = new List<GuiElement>();
            public string background = "";
            [NonSerialized]
            public Texture2D _background = null;
        }

        public class CharacterData
        {
            public string layout { get; set; } = "";
            public Dictionary<string, string> stats { get; set; } = new Dictionary<string, string>();

            [NonSerialized]
            public string _id = "";

            public string Formula(string roll)
            {
                return roll.Replace("{", "").Replace("}", "");
            }

            public string Evaluate(string roll)
            {
                bool changed = true;
                int pass = 0;
                while (changed)
                {
                    pass++;
                    changed = false;
                    foreach (KeyValuePair<string, string> replacement in this.stats)
                    {
                        string preChange = roll;
                        roll = roll.Replace(replacement.Key, replacement.Value);
                        if (preChange != roll) { changed = true; }
                    }
                    if (pass >= 10) { break; }
                }

                DataTable dt = new DataTable();
                string[] rolls = roll.Split('/');
                for (int r = 0; r < rolls.Length; r++)
                {
                    object result = null;
                    try { result = dt.Compute(rolls[r], ""); } catch {; }
                    if (result != null)
                    {
                        int modifier = 0;
                        if (int.TryParse(result.ToString(), out modifier))
                        {
                            rolls[r] = modifier.ToString();
                        }
                    }
                }
                roll = String.Join("/", rolls);

                return roll;
            }
        }
    }
}

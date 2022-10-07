using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;


namespace LordAshes
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(LordAshes.FileAccessPlugin.Guid)]
    [BepInDependency(LordAshes.ChatRollPlugin.Guid,BepInDependency.DependencyFlags.SoftDependency)]
    public partial class TaleSpireEmbeddedCharacterSheetPlugin : BaseUnityPlugin
    {
        // Plugin info
        public const string Name = "Embedded Character Sheet Plug-In";              
        public const string Guid = "org.lordashes.plugins.embeddedcharactersheet";
        public const string Version = "1.0.0.0";                    

        // Configuration
        public enum RollMethod
        {
            talespire_dice = 1,
            chat_roll = 2
        }

        private ConfigEntry<RollMethod> rollMethod { get; set; }
        private ConfigEntry<KeyboardShortcut> triggerKey { get; set; }
        private ConfigEntry<KeyboardShortcut> triggerReset { get; set; }
        private ConfigEntry<bool> closeAfteRoll { get; set; }
        private ConfigEntry<bool> applyNameMod { get; set; }
        public static ConfigEntry<bool> logDiagnostics { get; set; }
        private static ConfigEntry<Color> defaultEntryTextColor { get; set; }
        private static ConfigEntry<int> defaultEntryTextSize { get; set; }

        // Vars
        private bool characterSheetShowing = false;
        private GuiLayout currentLayout = null;
        private CharacterData currentData = null;

        private Dictionary<string, GuiLayout> layouts = new Dictionary<string, GuiLayout>();
        private Dictionary<string, CharacterData> data = new Dictionary<string, CharacterData>();

        void Awake()
        {
            UnityEngine.Debug.Log("Embedded Character Sheet Plugin: "+this.GetType().AssemblyQualifiedName+" Active.");

            triggerKey = Config.Bind("Hotkeys", "Toggle Character Sheet", new KeyboardShortcut(KeyCode.S, KeyCode.LeftControl));
            triggerReset = Config.Bind("Hotkeys", "You Know Nothing Jon Snow", new KeyboardShortcut(KeyCode.S, KeyCode.RightControl));
            rollMethod = Config.Bind("Settings", "Roll Method", RollMethod.talespire_dice);
            closeAfteRoll = Config.Bind("Settings", "Close Character Sheet After Roll", true);
            applyNameMod = Config.Bind("Settings", "Apply Name Mod On Roll Results In Chat", true);
            logDiagnostics = Config.Bind("Settings", "Log Diagnostics To Log", true);
            defaultEntryTextColor = Config.Bind("Defaults", "Entry Text Color", Color.black);
            defaultEntryTextSize = Config.Bind("Defaults", "Entry Text Size", 16);

            // Add Info menu selection to main character menu
            RadialUI.RadialSubmenu.EnsureMainMenuItem(RadialUI.RadialUIPlugin.Guid + ".Info",
                                                        RadialUI.RadialSubmenu.MenuType.character,
                                                        "Info",
                                                        FileAccessPlugin.Image.LoadSprite("Icon.Info.png")
                                                     );

            // Add Icons sub menu item
            RadialUI.RadialSubmenu.CreateSubMenuItem(RadialUI.RadialUIPlugin.Guid + ".Info",
                                                        "Character Sheet",
                                                        FileAccessPlugin.Image.LoadSprite("Icon.CharacterSheet.png"),
                                                        ToggleSheet,
                                                        true, () => { return LocalClient.HasControlOfCreature(new CreatureGuid(RadialUI.RadialUIPlugin.GetLastRadialTargetCreature())); }
                                                    );

            if (applyNameMod.Value)
            {
                var harmony = new Harmony(TaleSpireEmbeddedCharacterSheetPlugin.Guid);
                harmony.PatchAll();
            }

            Utility.PostOnMainPage(this.GetType());
        }

        void Update()
        {
            if (Utility.isBoardLoaded())
            {
                if (Utility.StrictKeyCheck(triggerKey.Value))
                {
                    ToggleSheet(CreatureGuid.Empty, null, null);
                }
                if (Utility.StrictKeyCheck(triggerReset.Value))
                {
                    SystemMessage.DisplayInfoText("Embedded Character Sheet:\r\nYou Know Nothing, Jon Snow");
                    layouts.Clear();
                    data.Clear();
                }
            }
        }

        private void ToggleSheet(CreatureGuid arg1, string arg2, MapMenuItem arg3)
        {
            characterSheetShowing = !characterSheetShowing;
            if (characterSheetShowing)
            {
                Debug.Log("Embedded Character Sheet Plugin: Toggling Embedded Character Sheet View To On");
                GetCharacterSheet();
            }
            else
            {
                Debug.Log("Embedded Character Sheet Plugin: Toggling Embedded Character Sheet View To Off");
                currentData = null;
                currentLayout = null;
            }
        }

        void OnGUI()
        {
            if(characterSheetShowing)
            {
                try
                {
                    if (currentLayout._background != null)
                    {
                        GUI.DrawTexture(new Rect(currentLayout.position.x, currentLayout.position.y, currentLayout.size.w, currentLayout.size.h), currentLayout._background, ScaleMode.StretchToFill);
                    }
                    foreach (GuiElement el in currentLayout.elements)
                    {
                        switch (el.type)
                        {
                            case GuiElementType.label:
                                ClickableLabel(el, currentData);
                                break;
                            case GuiElementType.button:
                                ClickableButton(el, currentData);
                                break;
                        }
                    }
                }
                catch
                {
                    // Character Sheet closed during render
                }
            }
        }

        private void ClickableLabel(GuiElement el, CharacterData data)
        {
            if (Input.mousePosition.x >= (currentLayout.position.x + el.position.x) && Input.mousePosition.x <= (currentLayout.position.x + el.position.x + el.size.w) &&
               Input.mousePosition.y >= (Screen.height - (currentLayout.position.y + el.position.y + el.size.h)) && Input.mousePosition.y <= (Screen.height - (currentLayout.position.y + el.position.y)) &&
               Input.GetMouseButtonUp(0))
            {
                Roll(el.content, data.Evaluate(el.roll));
            }
            if(el._texture!=null)
            {
                GUI.Label(new Rect(currentLayout.position.x + el.position.x, currentLayout.position.y + el.position.y, el.size.w, el.size.h), el._texture, el._style);
            }
            else
            {
                GUI.Label(new Rect(currentLayout.position.x + el.position.x, currentLayout.position.y + el.position.y, el.size.w, el.size.h), el.content, el._style);
            }
        }

        private void ClickableButton(GuiElement el, CharacterData data)
        {
            bool clicked = false;
            if (el._texture != null)
            {
                if(GUI.Button(new Rect(currentLayout.position.x + el.position.x, currentLayout.position.y + el.position.y, el.size.w, el.size.h), el._texture, el._style)) { clicked = true; }
            }
            else
            {
                if (GUI.Button(new Rect(currentLayout.position.x + el.position.x, currentLayout.position.y + el.position.y, el.size.w, el.size.h), el.content, el._style)) { clicked = true; }
            }
            if (clicked)
            {
                Roll(el.content, data.Evaluate(el.roll));
            }
        }

        private void Roll(string content, string roll)
        {
            switch (rollMethod.Value)
            {
                case RollMethod.chat_roll:
                    RollChatRoll(content, roll);
                    break;
                default:
                    RollTalespire(content, roll);
                    break;
            }
            if (closeAfteRoll.Value)
            {
                characterSheetShowing = false;
                currentData = null;
                currentLayout = null;
            }
        }

        private void RollChatRoll(string content, string rolls)
        {
            foreach(string roll in rolls.Split('/'))
            {
                ChatManager.SendChatMessage("/rn " + content + " " + roll, LocalClient.SelectedCreatureId.Value);
            }
        }

        private void RollTalespire(string content, string roll)
        {
            if (!roll.ToUpper().Contains("D"))
            {
                Debug.Log("Embedded Character Sheet Plugin: Chat Roll Rolling '" + roll + "'");
                SystemMessage.DisplayInfoText("Roll '" + roll + "' Has No Dice.");
                return;
            }
            else
            {
                string[] rolls = roll.Split('/');
                for(int r=0; r<rolls.Length; r++)
                {
                    rolls[r] = rolls[r] + "+0";
                    int pos = rolls[r].ToUpper().IndexOf("D") + 1;
                    while ("0123456789".Contains(rolls[r].Substring(pos, 1))) { pos++; }
                    DataTable dt = new DataTable();
                    int modifier = int.Parse(dt.Compute(rolls[r].Substring(pos), "").ToString());
                    if (modifier < 0)
                    {
                        rolls[r] = rolls[r].Substring(0, pos) + modifier;
                    }
                    else
                    {
                        rolls[r] = rolls[r].Substring(0, pos) + "+" + modifier;
                    }
                }
                roll = String.Join("/", rolls);
                Debug.Log("Embedded Character Sheet Plugin: Talespire Rolling '" + roll + "'");
                CreatureBoardAsset asset = null;
                CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out asset);
                if (asset != null) { content = "[" + Utility.GetCreatureName(asset.Name) + "]" + content; }
                new System.Diagnostics.Process()
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = "talespire://dice/" + content + ":" + roll,
                        Arguments = "",
                        CreateNoWindow = true
                    }
                }.Start();
            }
        }

        private void GetCharacterSheet()
        {
            CreatureBoardAsset asset = null;
            CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out asset);
            if (asset != null)
            {
                string json = "";
                try
                {
                    string assetName = Utility.GetCreatureName(asset.Name);
                    if (data.ContainsKey(assetName))
                    {
                        // Use cached character data
                        currentData = data[assetName];
                        if (logDiagnostics.Value) { Debug.Log("Embedded Character Sheet Plugin: Using Cached Data For '" + assetName + "'"); }
                    }
                    else
                    {
                        if (FileAccessPlugin.File.Exists("EmbeddedCharacterSheet.Data." + assetName + ".csd"))
                        {
                            json = FileAccessPlugin.File.ReadAllText("EmbeddedCharacterSheet.Data." + assetName + ".csd");
                            currentData = JsonConvert.DeserializeObject<CharacterData>(json);
                            currentData._id = assetName;
                            if (logDiagnostics.Value) { Debug.Log("Embedded Character Sheet Plugin: Loaded Data For '" + assetName + "'"); }
                            // Cache character data
                            data.Add(assetName, currentData);
                        }
                        else
                        {
                            if (logDiagnostics.Value) { Debug.Log("Embedded Character Sheet Plugin: No Data File For '" + assetName + "' Name 'EmbeddedCharacterSheet.Data." + assetName + ".csd'"); }
                            characterSheetShowing = false;
                            currentData = null;
                            currentLayout = null;
                            return;
                        }
                    }
                    if (layouts.ContainsKey(currentData.layout))
                    {
                        // Use cached layout
                        currentLayout = layouts[currentData.layout];
                        if (logDiagnostics.Value) { Debug.Log("Embedded Character Sheet Plugin: Using Cached Layout For '" + currentData.layout + "'"); }
                    }
                    else
                    {
                        if (FileAccessPlugin.File.Exists("EmbeddedCharacterSheet.Layout." + currentData.layout + ".csl"))
                        {
                            json = FileAccessPlugin.File.ReadAllText("EmbeddedCharacterSheet.Layout." + currentData.layout + ".csl");
                            currentLayout = JsonConvert.DeserializeObject<GuiLayout>(json);
                            if (logDiagnostics.Value) { Debug.Log("Embedded Character Sheet Plugin: Loaded Layout '" + currentData.layout + "'"); }
                            // Load background image
                            if (currentLayout.background != "")
                            {
                                currentLayout._background = FileAccessPlugin.Image.LoadTexture(currentLayout.background);
                            }
                            foreach (GuiElement el in currentLayout.elements)
                            {
                                // Load element images
                                if (el.content.StartsWith("#") && el.content.EndsWith("#"))
                                {
                                    el._texture = FileAccessPlugin.Image.LoadTexture(el.content.Replace("#", ""));
                                }
                                // Update custom styles
                                if (el.style != null && el.style.ToString().Trim() != "")
                                {
                                    ReflectionObjectModifier.ApplyStyleCustomization(el._style, el.style);
                                }
                                // Evaluate content
                                if (el.content.Contains("{") || el.content.Contains("+") || el.content.Contains("-"))
                                {
                                    el.content = currentData.Evaluate(el.content);
                                    el.content = el.content.Replace(" ", " "); // Space to ALT+255
                                }
                            }
                            // Cache layout
                            layouts.Add(currentData.layout, currentLayout);
                        }
                        else
                        {
                            Debug.LogWarning("Embedded Character Sheet Plugin: No Layout File For 'EmbeddedCharacterSheet.Layout." + currentData.layout + ".csl'");
                            characterSheetShowing = false;
                            currentData = null;
                            currentLayout = null;
                            return;
                        }
                    }
                }
                catch(Exception x)
                {
                    Debug.LogWarning("Embedded Character Sheet Plugin: Exception In GetCharacterSheet");
                    Debug.LogException(x);
                }
            }
        }
    }
}

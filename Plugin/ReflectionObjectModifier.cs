using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LordAshes
{
    public static class ReflectionObjectModifier
    {
        public static void ApplyStyleCustomization(object obj, string mods)
        {
            string[] modifications = mods.Split('|');
            if (modifications.Length > 0)
            {
                foreach (string mod in modifications)
                {
                    Debug.Log("Embedded Characater Sheet Plugin: Applying Style Customizations '"+mod+"'");
                    string key = mod.Substring(0, mod.IndexOf("="));
                    string value = mod.Substring(mod.IndexOf("=") + 1);
                    Modify(obj, key, value);
                }
            }
        }

        public static void Modify(object baseObj, string key, object value)
        {
            string prop = "";
            if(key.Contains("."))
            {
                prop = key.Substring(key.LastIndexOf(".") + 1);
                key = key.Substring(0, key.LastIndexOf("."));
            }
            else
            {
                prop = key;
                key = "";
            }
            object obj = baseObj;
            if (key != "")
            {
                foreach (string part in key.Split('.'))
                {
                    obj = GetObject(obj, part);
                }
            }
            SetValue(obj, prop, value);
        }

        public static object GetObject(object parent, string childName)
        {
            try
            {
                Type type = parent.GetType();
                foreach (FieldInfo fi in type.GetRuntimeFields())
                {
                    if (fi.Name == childName) { return fi.GetValue(parent); }
                }
                foreach (PropertyInfo pi in type.GetRuntimeProperties())
                {
                    if (pi.Name == childName) { return pi.GetValue(parent); }
                }
                return null;
            }
            catch
            {
                Debug.LogWarning("Embedded Characater Sheet Plugin: Error Getting '" + childName + "' Of '" + parent.ToString() + "'");
                return null;
            }
        }

        public static void SetValue(object parent, string prop, object value)
        {
            try
            {
                Type type = parent.GetType();
                foreach (FieldInfo fi in type.GetRuntimeFields())
                {
                    if (fi.Name == prop)
                    {
                        if (TaleSpireEmbeddedCharacterSheetPlugin.logDiagnostics.Value) { Debug.Log("Embedded Characater Sheet Plugin: Setting Field (Type " + fi.FieldType.Name + ") To " + value); }
                        fi.SetValue(parent, ConvertToType(value, fi.FieldType));
                        return;
                    }
                }
                foreach (PropertyInfo pi in type.GetRuntimeProperties())
                {
                    if (pi.Name == prop)
                    {
                        if (TaleSpireEmbeddedCharacterSheetPlugin.logDiagnostics.Value) { Debug.Log("Embedded Characater Sheet Plugin: Setting Property (Type " + pi.PropertyType.Name + ") To " + value); }
                        pi.SetValue(parent, ConvertToType(value, pi.PropertyType));
                        return;
                    }
                }
            }
            catch(Exception x)
            {
                Debug.LogWarning("Embedded Characater Sheet Plugin: Error Setting Property '" + prop + "' To '" + value + "'");
                Debug.LogException(x);
            }
        }

        private static object ConvertToType(object value, Type type)
        {
            switch(type.Name)
            {
                case "String":
                    return value.ToString();
                case "Color":
                    Color unityEngineColour = Color.clear;
                    if(!value.ToString().Contains(","))
                    {
                        ColorUtility.TryParseHtmlString(value.ToString(), out unityEngineColour);
                        return unityEngineColour;
                    }
                    else
                    {
                        string[] parts = value.ToString().Split(',');
                        switch(parts.Length)
                        {
                            case 1:
                                return new UnityEngine.Color(float.Parse(parts[0]) / 255f, 0f, 0f);
                            case 2:
                                return new UnityEngine.Color(float.Parse(parts[0]) / 255f, float.Parse(parts[1]) / 255f, 0f);
                            case 3:
                                return new UnityEngine.Color(float.Parse(parts[0]) / 255f, float.Parse(parts[1]) / 255f, float.Parse(parts[2]) / 255f);
                            case 4:
                                return new UnityEngine.Color(float.Parse(parts[0]) / 255f, float.Parse(parts[1]) / 255f, float.Parse(parts[2]) / 255f, float.Parse(parts[3]) / 255f);
                        }
                        return null;
                    }
                case "Int16":
                    return Int16.Parse(value.ToString());
                case "Int32":
                    return Int32.Parse(value.ToString());
                case "Int64":
                    return Int64.Parse(value.ToString());
                case "UInt16":
                    return UInt16.Parse(value.ToString());
                case "UInt32":
                    return UInt32.Parse(value.ToString());
                case "UInt64":
                    return UInt64.Parse(value.ToString());
                case "Texture2D":
                    return FileAccessPlugin.Image.LoadTexture(value.ToString());
                default:
                    try
                    {
                        foreach (MethodInfo mi in type.GetRuntimeMethods())
                        {
                            if (mi.Name == "Parse")
                            {
                                if (TaleSpireEmbeddedCharacterSheetPlugin.logDiagnostics.Value) { Debug.Log("Embedded Characater Sheet Plugin: Attempting Conversion Of '" + value.ToString() + "' Using Parse Method Of '" + type.Name + "'"); }
                                return mi.Invoke(null, new object[] { value.ToString() });
                            }
                        }
                        int enumValue = 0;
                        if(int.TryParse(value.ToString(), out enumValue))
                        {
                            if (TaleSpireEmbeddedCharacterSheetPlugin.logDiagnostics.Value) { Debug.Log("Embedded Characater Sheet Plugin: Attempting Return As An Enum Int"); }
                            return enumValue;
                        }
                        return value.ToString();
                    }
                    catch (Exception x)
                    {
                        Debug.LogWarning("Embedded Characater Sheet Plugin: Error In Parse Conversion");
                        Debug.LogException(x);
                    }
                    return value.ToString();
            }
        }
    }
}


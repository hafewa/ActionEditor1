﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    static class EditorEX
    {
        private static readonly Dictionary<Type, Texture2D> _iconDictionary = new Dictionary<Type, Texture2D>();
        private static readonly Dictionary<Type, Color> _colorDictionary = new Dictionary<Type, Color>();
        private static readonly Dictionary<Type, string> _nameDictionary = new Dictionary<Type, string>();

        public static Texture2D GetIcon(this IDirectable track)
        {
            var type = track.GetType();
            if (_iconDictionary.TryGetValue(type, out var icon))
            {
                return icon;
            }

            var att = track.GetType().GetCustomAttribute<TrackIconAttribute>(true);

            if (att != null)
            {

                if (!string.IsNullOrEmpty(att.iconPath))
                {
                    if (att.iconPath.StartsWith("Assets/"))
                        icon = AssetDatabase.LoadAssetAtPath<Texture2D>(att.iconPath);
                    else
                        icon = Resources.Load(att.iconPath) as Texture2D;
                    if (icon == null)
                        icon = EditorGUIUtility.FindTexture(att.iconPath);
                }
                else if (icon == null)
                    icon = AssetPreview.GetMiniTypeThumbnail(att.fromType);

            }

            if (icon != null)
                _iconDictionary[type] = icon;
            return icon;
        }

        public static Color GetColor(this IDirectable track)
        {
            var type = track.GetType();
            if (_colorDictionary.TryGetValue(type, out var color))
                return color;

            var colorAttribute = track.GetType().GetCustomAttribute<ColorAttribute>();
            _colorDictionary[type] = colorAttribute != null ? colorAttribute.Color : Color.gray;
            return _colorDictionary[type];
        }

        public static string GetName(this IDirectable track)
        {
            var type = track.GetType();
            if (_nameDictionary.TryGetValue(type, out var name))
                return name;
            var nameAttribute = track.GetType().GetCustomAttribute<NameAttribute>();
            _nameDictionary[type] = nameAttribute != null ? nameAttribute.name : track.GetType().Name;
            return _nameDictionary[type];
        }

        public static bool CanAddTrack(this Group group, Track track)
        {

            if (track == null) return false;
            var type = track.GetType();
            if (type == null || !type.IsSubclassOf(typeof(Track)) || type.IsAbstract) return false;
            if (type.IsDefined(typeof(UniqueTrackAttribute), true) &&
                group.ExistSameTypeTrack(type))
                return false;
            var attachAtt = type.GetCustomAttribute<AttachableAttribute>(true);
            if (attachAtt == null || attachAtt.Types == null || attachAtt.Types.All(t => t != group.GetType())) return false;

            return true;
        }


        public static float SnapTime(this IDirector asset, float time) => Mathf.Round(time / Prefs.SnapInterval) * Prefs.SnapInterval;

        public static float TimeToPos(this IDirector asset, float time, float width) => (time - asset.ViewTimeMin) / asset.ViewTime * width;

        public static float PosToTime(this IDirector asset, float pos, float width) => pos / width * asset.ViewTime + asset.ViewTimeMin;

        public static float WidthToTime(this IDirector asset, float pos, float width) => pos / width * asset.ViewTime;




        public static void DrawDashedLine(float x, float startY, float endY, Color color)
        {
            Handles.BeginGUI();
            Handles.color = color;

            var totalLength = Mathf.Abs(endY - startY);
            var dashes = Mathf.FloorToInt(totalLength / 10); // 每段长度为10

            for (var i = 0; i < dashes; i++)
            {
                var t1 = (float)i / dashes;
                var t2 = (i + 0.5f) / dashes;
                var point1Y = Mathf.Lerp(startY, endY, t1);
                var point2Y = Mathf.Lerp(startY, endY, t2);

                Handles.DrawLine(new Vector2(x, point1Y), new Vector2(x, point2Y));
            }

            Handles.EndGUI();
        }

        public static Color WithAlpha(this Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static IEnumerable<Type> GetAllTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes());

        }


        /// <summary>
        /// 获取类型所有子类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type[] GetImplementationsOf(Type type)
        {
            if (type.IsInterface)
                return GetAllTypes().Where(x => x.GetInterfaces().Count(y => y == type) != 0).Where(x => !x.IsAbstract).ToArray();
            return GetAllTypes().Where(x => type.IsAssignableFrom(x)).Where(x => !x.IsAbstract).ToArray();

        }



        public struct TypeMetaInfo
        {
            public Type type;
            public string name;
            //public string category;
            public Type[] attachableTypes;
            public bool isUnique;
        }

        public static void BoldSeparator()
        {
            var tex = Styles.WhiteTexture;
            var lastRect = GUILayoutUtility.GetLastRect();
            GUILayout.Space(14);
            GUI.color = new Color(0, 0, 0, 0.25f);
            GUI.DrawTexture(new Rect(0, lastRect.yMax + 6, Screen.width, 4), tex);
            GUI.DrawTexture(new Rect(0, lastRect.yMax + 6, Screen.width, 1), tex);
            GUI.DrawTexture(new Rect(0, lastRect.yMax + 9, Screen.width, 1), tex);
            GUI.color = Color.white;
        }




        /// <summary>
        /// 用于选择列表中任何元素而不添加NONE的通用弹出窗口
        /// </summary>
        public static T CleanPopup<T>(string prefix, T selected, List<T> options, params GUILayoutOption[] GUIOptions)
        {
            var index = -1;
            if (options.Contains(selected))
            {
                index = options.IndexOf(selected);
            }

            var stringedOptions = options.Select(o => o != null ? o.ToString() : "NONE");

            GUI.enabled = options.Count > 0;
            if (!string.IsNullOrEmpty(prefix))
                index = EditorGUILayout.Popup(prefix, index, stringedOptions.ToArray(), GUIOptions);
            else index = EditorGUILayout.Popup(index, stringedOptions.ToArray(), GUIOptions);
            GUI.enabled = true;

            return index == -1 ? default(T) : options[index];
        }

        /// <summary>
        /// 获取当前加载的集合中基类型的所有非抽象派生类
        /// </summary>
        /// <param name="baseType"></param>
        /// <returns></returns>
        public static List<TypeMetaInfo> GetTypeMetaDerivedFrom(Type baseType)
        {
            var infos = new List<TypeMetaInfo>();
            foreach (var type in EditorEX.GetImplementationsOf(baseType))
            {
                if (type.GetCustomAttributes(typeof(System.ObsoleteAttribute), true).FirstOrDefault() != null)
                {
                    continue;
                }

                var info = new TypeMetaInfo
                {
                    type = type,
                    name =
                        type.GetCustomAttributes(typeof(NameAttribute), true).FirstOrDefault() is NameAttribute nameAtt
                            ? nameAtt.name
                            : type.Name
                };



                if (type.GetCustomAttributes(typeof(AttachableAttribute), true).FirstOrDefault() is AttachableAttribute
                    attachAtt)
                {
                    info.attachableTypes = attachAtt.Types;
                }

                info.isUnique = type.IsDefined(typeof(UniqueTrackAttribute), true);

                infos.Add(info);
            }

            infos = infos.OrderBy(i => i.name).ToList();
            return infos;
        }


        /// <summary>
        /// 查找类型的最后子类
        /// </summary>
        /// <param name="type"></param>
        /// <param name="dictionary"></param>
        public static void GetTypeLastSubclass(Type type, Dictionary<Type, Type> dictionary)
        {
            var children = GetTypeMetaDerivedFrom(type);
            foreach (var t in children)
            {
                var iT = t.type;
                //如果不是抽象类就更新
                if (!iT.IsAbstract)
                {
                    dictionary[type] = iT;
                }
            }
        }




        public static readonly Dictionary<string, Type> AssetTypes = new Dictionary<string, Type>();
        public static readonly List<string> AssetNames = new List<string>();

        public static void InitializeAssetTypes()
        {
            AssetTypes.Clear();
            AssetNames.Clear();
            var types = EditorEX.GetImplementationsOf(typeof(Asset));
            foreach (var t in types)
            {
                var typeName = t.GetCustomAttributes(typeof(NameAttribute), false).FirstOrDefault() is NameAttribute nameAtt ? nameAtt.name : t.Name;
                AssetTypes[typeName] = t;
                AssetNames.Add(typeName);
            }
        }

        public static string GetAssetTypeName(Type type)
        {
            foreach (var key in AssetTypes.Keys)
            {
                var v = AssetTypes[key];
                if (v == type)
                {
                    return key;
                }
            }

            return string.Empty;
        }
    }
}
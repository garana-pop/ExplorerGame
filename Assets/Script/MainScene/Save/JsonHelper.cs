using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// JsonUtility でのDictionary型シリアライズをサポートするヘルパークラス
/// </summary>
public static class JsonHelper
{
    /// <summary>
    /// Dictionary をシリアライズ可能な形式に変換するための内部クラス
    /// </summary>
    [Serializable]
    public class Serialization<TKey, TValue>
    {
        [SerializeField] private List<KeyValuePair<TKey, TValue>> items;

        /// <summary>
        /// キーと値のペアのリストを取得します
        /// </summary>
        public List<KeyValuePair<TKey, TValue>> Items => items;

        /// <summary>
        /// 指定されたDictionaryからシリアライズ可能なオブジェクトを生成します
        /// </summary>
        /// <param name="dictionary">変換元のDictionary</param>
        /// <param name="initialCapacity">初期リストサイズ（最適化用）</param>
        public Serialization(Dictionary<TKey, TValue> dictionary, int? initialCapacity = null)
        {
            int capacity = initialCapacity ?? (dictionary?.Count ?? 4);
            items = new List<KeyValuePair<TKey, TValue>>(capacity);

            if (dictionary != null)
            {
                foreach (var kvp in dictionary)
                {
                    items.Add(new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value));
                }
            }
        }

        /// <summary>
        /// シリアライズされた内容からDictionaryを再構築します
        /// </summary>
        /// <returns>復元されたDictionary</returns>
        public Dictionary<TKey, TValue> ToDictionary()
        {
            Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>(items.Count);

            foreach (var kvp in items)
            {
                try
                {
                    // 重複キーのエラー処理
                    if (!dictionary.ContainsKey(kvp.Key))
                    {
                        dictionary[kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        Debug.LogWarning($"重複するキーがあります: {kvp.Key}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Dictionary変換中にエラーが発生しました: {ex.Message}");
                }
            }

            return dictionary;
        }
    }

    /// <summary>
    /// キーと値のペアを保存するためのシリアライズ可能なクラス
    /// </summary>
    [Serializable]
    public class KeyValuePair<TKey, TValue>
    {
        [SerializeField] private TKey key;
        [SerializeField] private TValue value;

        /// <summary>キーを取得します</summary>
        public TKey Key => key;

        /// <summary>値を取得します</summary>
        public TValue Value => value;

        /// <summary>
        /// 新しいキーと値のペアを生成します
        /// </summary>
        public KeyValuePair(TKey key, TValue value)
        {
            this.key = key;
            this.value = value;
        }
    }

    /// <summary>
    /// Dictionary をJSON文字列に変換します
    /// </summary>
    /// <typeparam name="TKey">Dictionary のキーの型</typeparam>
    /// <typeparam name="TValue">Dictionary の値の型</typeparam>
    /// <param name="dictionary">変換するDictionary</param>
    /// <param name="prettyPrint">整形出力を行うかどうか</param>
    /// <returns>JSON文字列</returns>
    public static string ToJson<TKey, TValue>(Dictionary<TKey, TValue> dictionary, bool prettyPrint = false)
    {
        try
        {
            Serialization<TKey, TValue> serialization = new Serialization<TKey, TValue>(dictionary);
            return JsonUtility.ToJson(serialization, prettyPrint);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Dictionary のJSON変換中にエラーが発生しました: {ex.Message}");
            return "{}";
        }
    }

    /// <summary>
    /// JSON文字列からDictionaryを復元します
    /// </summary>
    /// <typeparam name="TKey">Dictionary のキーの型</typeparam>
    /// <typeparam name="TValue">Dictionary の値の型</typeparam>
    /// <param name="json">JSON文字列</param>
    /// <returns>復元されたDictionary</returns>
    public static Dictionary<TKey, TValue> FromJson<TKey, TValue>(string json)
    {
        try
        {
            if (string.IsNullOrEmpty(json))
            {
                return new Dictionary<TKey, TValue>();
            }

            Serialization<TKey, TValue> serialization =
                JsonUtility.FromJson<Serialization<TKey, TValue>>(json);

            return serialization?.ToDictionary() ?? new Dictionary<TKey, TValue>();
        }
        catch (Exception ex)
        {
            Debug.LogError($"JSON からDictionary への変換中にエラーが発生しました: {ex.Message}");
            return new Dictionary<TKey, TValue>();
        }
    }
}
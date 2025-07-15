using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// JsonUtility �ł�Dictionary�^�V���A���C�Y���T�|�[�g����w���p�[�N���X
/// </summary>
public static class JsonHelper
{
    /// <summary>
    /// Dictionary ���V���A���C�Y�\�Ȍ`���ɕϊ����邽�߂̓����N���X
    /// </summary>
    [Serializable]
    public class Serialization<TKey, TValue>
    {
        [SerializeField] private List<KeyValuePair<TKey, TValue>> items;

        /// <summary>
        /// �L�[�ƒl�̃y�A�̃��X�g���擾���܂�
        /// </summary>
        public List<KeyValuePair<TKey, TValue>> Items => items;

        /// <summary>
        /// �w�肳�ꂽDictionary����V���A���C�Y�\�ȃI�u�W�F�N�g�𐶐����܂�
        /// </summary>
        /// <param name="dictionary">�ϊ�����Dictionary</param>
        /// <param name="initialCapacity">�������X�g�T�C�Y�i�œK���p�j</param>
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
        /// �V���A���C�Y���ꂽ���e����Dictionary���č\�z���܂�
        /// </summary>
        /// <returns>�������ꂽDictionary</returns>
        public Dictionary<TKey, TValue> ToDictionary()
        {
            Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>(items.Count);

            foreach (var kvp in items)
            {
                try
                {
                    // �d���L�[�̃G���[����
                    if (!dictionary.ContainsKey(kvp.Key))
                    {
                        dictionary[kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        Debug.LogWarning($"�d������L�[������܂�: {kvp.Key}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Dictionary�ϊ����ɃG���[���������܂���: {ex.Message}");
                }
            }

            return dictionary;
        }
    }

    /// <summary>
    /// �L�[�ƒl�̃y�A��ۑ����邽�߂̃V���A���C�Y�\�ȃN���X
    /// </summary>
    [Serializable]
    public class KeyValuePair<TKey, TValue>
    {
        [SerializeField] private TKey key;
        [SerializeField] private TValue value;

        /// <summary>�L�[���擾���܂�</summary>
        public TKey Key => key;

        /// <summary>�l���擾���܂�</summary>
        public TValue Value => value;

        /// <summary>
        /// �V�����L�[�ƒl�̃y�A�𐶐����܂�
        /// </summary>
        public KeyValuePair(TKey key, TValue value)
        {
            this.key = key;
            this.value = value;
        }
    }

    /// <summary>
    /// Dictionary ��JSON������ɕϊ����܂�
    /// </summary>
    /// <typeparam name="TKey">Dictionary �̃L�[�̌^</typeparam>
    /// <typeparam name="TValue">Dictionary �̒l�̌^</typeparam>
    /// <param name="dictionary">�ϊ�����Dictionary</param>
    /// <param name="prettyPrint">���`�o�͂��s�����ǂ���</param>
    /// <returns>JSON������</returns>
    public static string ToJson<TKey, TValue>(Dictionary<TKey, TValue> dictionary, bool prettyPrint = false)
    {
        try
        {
            Serialization<TKey, TValue> serialization = new Serialization<TKey, TValue>(dictionary);
            return JsonUtility.ToJson(serialization, prettyPrint);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Dictionary ��JSON�ϊ����ɃG���[���������܂���: {ex.Message}");
            return "{}";
        }
    }

    /// <summary>
    /// JSON�����񂩂�Dictionary�𕜌����܂�
    /// </summary>
    /// <typeparam name="TKey">Dictionary �̃L�[�̌^</typeparam>
    /// <typeparam name="TValue">Dictionary �̒l�̌^</typeparam>
    /// <param name="json">JSON������</param>
    /// <returns>�������ꂽDictionary</returns>
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
            Debug.LogError($"JSON ����Dictionary �ւ̕ϊ����ɃG���[���������܂���: {ex.Message}");
            return new Dictionary<TKey, TValue>();
        }
    }
}
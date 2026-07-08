using System;
using Unity.Collections;
using Unity.Netcode;

namespace Sadalmalik.TotalShooter.Architecture
{
    // Одна запись реплицируемого blackboard: ключ + вариант-значение (string/double/bool). Всё
    // unmanaged (FixedString), чтобы ложиться в NetworkList без кастомной managed-сериализации.
    // Кап длины: ключ ≤ 63 байта, строковое значение ≤ 511 байт.
    public struct KvEntry : INetworkSerializable, IEquatable<KvEntry>
    {
        public enum Kind : byte
        {
            String,
            Number,
            Bool,
        }

        public FixedString64Bytes Key;
        public Kind Type;
        public FixedString512Bytes Str;
        public double Number;
        public bool Bool;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            var type = (byte) Type;
            serializer.SerializeValue(ref Key);
            serializer.SerializeValue(ref type);
            serializer.SerializeValue(ref Str);
            serializer.SerializeValue(ref Number);
            serializer.SerializeValue(ref Bool);
            Type = (Kind) type;
        }

        public bool Equals(KvEntry other)
        {
            return Key.Equals(other.Key)
                && Type == other.Type
                && Str.Equals(other.Str)
                && Number.Equals(other.Number)
                && Bool == other.Bool;
        }
    }

    // Доступ по ключу (индексатор) к реплицируемому NetworkList<KvEntry>, как к словарю. Сам
    // NetworkList живёт полем в NetworkBehaviour (NGO находит его рефлексией) — сюда передаётся
    // ссылкой. Запись идёт через NetworkList, права записи (Owner/Server) энфорсит NGO. Значения
    // string/double/bool — нативные типы Lua, так что из скриптов читается/пишется естественно.
    public class Blackboard
    {
        private readonly NetworkList<KvEntry> m_List;

        public Blackboard(NetworkList<KvEntry> list)
        {
            m_List = list;
        }

        public object this[string key]
        {
            get => TryGet(key, out var value) ? value : null;
            set => Set(key, value);
        }

        public bool TryGet(string key, out object value)
        {
            var target = new FixedString64Bytes(key);
            for (var i = 0; i < m_List.Count; i++)
            {
                var entry = m_List[i];
                if (!entry.Key.Equals(target))
                    continue;
                value = FromEntry(entry);
                return true;
            }

            value = null;
            return false;
        }

        private void Set(string key, object value)
        {
            var entry = ToEntry(key, value);
            for (var i = 0; i < m_List.Count; i++)
            {
                if (!m_List[i].Key.Equals(entry.Key))
                    continue;
                m_List[i] = entry; // обновление существующего ключа — реплицируется
                return;
            }

            m_List.Add(entry); // новый ключ — реплицируется
        }

        private static object FromEntry(KvEntry entry)
        {
            return entry.Type switch
            {
                KvEntry.Kind.String => entry.Str.ToString(),
                KvEntry.Kind.Number => entry.Number,
                KvEntry.Kind.Bool => entry.Bool,
                _ => null,
            };
        }

        private static KvEntry ToEntry(string key, object value)
        {
            var entry = new KvEntry { Key = new FixedString64Bytes(key) };
            switch (value)
            {
                case string s:
                    entry.Type = KvEntry.Kind.String;
                    entry.Str = new FixedString512Bytes(s);
                    break;
                case bool b:
                    entry.Type = KvEntry.Kind.Bool;
                    entry.Bool = b;
                    break;
                default:
                    entry.Type = KvEntry.Kind.Number;
                    entry.Number = Convert.ToDouble(value);
                    break;
            }

            return entry;
        }
    }
}

﻿#if UNITY_STANDALONE && !CLIENT_BUILD
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MySqlConnector;

namespace MultiplayerARPG.MMO
{
    public partial class MySQLDatabase
    {
        private List<int> ReadSockets(string sockets)
        {
            List<int> result = new List<int>();
            string[] splitTexts = sockets.Split(';');
            foreach (string text in splitTexts)
            {
                if (string.IsNullOrEmpty(text))
                    continue;
                result.Add(int.Parse(text));
            }
            return result;
        }

        private string WriteSockets(List<int> sockets)
        {
            string result = "";
            foreach (int socket in sockets)
            {
                result += socket + ";";
            }
            return result;
        }

        private async UniTask CreateCharacterItem(MySqlConnection connection, MySqlTransaction transaction, int idx, string characterId, InventoryType inventoryType, CharacterItem characterItem)
        {
            if (string.IsNullOrEmpty(characterItem.id))
                return;

            await ExecuteNonQuery(connection, transaction, "INSERT INTO characteritem (id, idx, inventoryType, characterId, dataId, level, amount, equipSlotIndex, durability, exp, lockRemainsDuration, ammo, sockets) VALUES (@id, @idx, @inventoryType, @characterId, @dataId, @level, @amount, @equipSlotIndex, @durability, @exp, @lockRemainsDuration, @ammo, @sockets)",
                new MySqlParameter("@id", characterItem.id),
                new MySqlParameter("@idx", idx),
                new MySqlParameter("@inventoryType", (byte)inventoryType),
                new MySqlParameter("@characterId", characterId),
                new MySqlParameter("@dataId", characterItem.dataId),
                new MySqlParameter("@level", characterItem.level),
                new MySqlParameter("@amount", characterItem.amount),
                new MySqlParameter("@equipSlotIndex", characterItem.equipSlotIndex),
                new MySqlParameter("@durability", characterItem.durability),
                new MySqlParameter("@exp", characterItem.exp),
                new MySqlParameter("@lockRemainsDuration", characterItem.lockRemainsDuration),
                new MySqlParameter("@ammo", characterItem.ammo),
                new MySqlParameter("@sockets", WriteSockets(characterItem.sockets)));
        }

        private bool ReadCharacterItem(MySqlDataReader reader, out CharacterItem result)
        {
            if (reader.Read())
            {
                result = new CharacterItem();
                result.id = reader.GetString(0);
                result.dataId = reader.GetInt32(1);
                result.level = reader.GetInt16(2);
                result.amount = reader.GetInt16(3);
                result.equipSlotIndex = reader.GetByte(4);
                result.durability = reader.GetFloat(5);
                result.exp = reader.GetInt32(6);
                result.lockRemainsDuration = reader.GetFloat(7);
                result.ammo = reader.GetInt16(8);
                result.sockets = ReadSockets(reader.GetString(9));
                return true;
            }
            result = CharacterItem.Empty;
            return false;
        }

        private async UniTask<List<CharacterItem>> ReadCharacterItems(string characterId, InventoryType inventoryType, List<CharacterItem> result = null)
        {
            if (result == null)
                result = new List<CharacterItem>();
            await ExecuteReader((reader) =>
            {
                CharacterItem tempInventory;
                while (ReadCharacterItem(reader, out tempInventory))
                {
                    result.Add(tempInventory);
                }
            }, "SELECT id, dataId, level, amount, equipSlotIndex, durability, exp, lockRemainsDuration, ammo, sockets FROM characteritem WHERE characterId=@characterId AND inventoryType=@inventoryType ORDER BY idx ASC",
                new MySqlParameter("@characterId", characterId),
                new MySqlParameter("@inventoryType", (byte)inventoryType));
            return result;
        }

        public async UniTask<List<EquipWeapons>> ReadCharacterEquipWeapons(string characterId, List<EquipWeapons> result = null)
        {
            if (result == null)
                result = new List<EquipWeapons>();
            await ExecuteReader((reader) =>
            {
                CharacterItem tempInventory;
                byte equipWeaponSet;
                InventoryType inventoryType;
                while (ReadCharacterItem(reader, out tempInventory))
                {
                    equipWeaponSet = reader.GetByte(10);
                    inventoryType = (InventoryType)reader.GetByte(11);
                    // Fill weapon sets if needed
                    while (result.Count <= equipWeaponSet)
                        result.Add(new EquipWeapons());
                    // Get equip weapon set
                    if (inventoryType == InventoryType.EquipWeaponRight)
                        result[equipWeaponSet].rightHand = tempInventory;
                    if (inventoryType == InventoryType.EquipWeaponLeft)
                        result[equipWeaponSet].leftHand = tempInventory;
                }
            }, "SELECT id, dataId, level, amount, equipSlotIndex, durability, exp, lockRemainsDuration, ammo, sockets, idx, inventoryType FROM characteritem WHERE characterId=@characterId AND (inventoryType=@inventoryType1 OR inventoryType=@inventoryType2) ORDER BY idx ASC",
                new MySqlParameter("@characterId", characterId),
                new MySqlParameter("@inventoryType1", (byte)InventoryType.EquipWeaponRight),
                new MySqlParameter("@inventoryType2", (byte)InventoryType.EquipWeaponLeft));
            return result;
        }

        public async UniTask CreateCharacterEquipWeapons(MySqlConnection connection, MySqlTransaction transaction, byte equipWeaponSet, string characterId, EquipWeapons equipWeapons)
        {
            await CreateCharacterItem(connection, transaction, equipWeaponSet, characterId, InventoryType.EquipWeaponRight, equipWeapons.rightHand);
            await CreateCharacterItem(connection, transaction, equipWeaponSet, characterId, InventoryType.EquipWeaponLeft, equipWeapons.leftHand);
        }

        public async UniTask CreateCharacterEquipItem(MySqlConnection connection, MySqlTransaction transaction, int idx, string characterId, CharacterItem characterItem)
        {
            await CreateCharacterItem(connection, transaction, idx, characterId, InventoryType.EquipItems, characterItem);
        }

        public async UniTask<List<CharacterItem>> ReadCharacterEquipItems(string characterId, List<CharacterItem> result = null)
        {
            return await ReadCharacterItems(characterId, InventoryType.EquipItems, result);
        }

        public async UniTask CreateCharacterNonEquipItem(MySqlConnection connection, MySqlTransaction transaction, int idx, string characterId, CharacterItem characterItem)
        {
            await CreateCharacterItem(connection, transaction, idx, characterId, InventoryType.NonEquipItems, characterItem);
        }

        public async UniTask<List<CharacterItem>> ReadCharacterNonEquipItems(string characterId, List<CharacterItem> result = null)
        {
            return await ReadCharacterItems(characterId, InventoryType.NonEquipItems, result);
        }

        public async UniTask DeleteCharacterItems(MySqlConnection connection, MySqlTransaction transaction, string characterId)
        {
            await ExecuteNonQuery(connection, transaction, "DELETE FROM characteritem WHERE characterId=@characterId", new MySqlParameter("@characterId", characterId));
        }
    }
}
#endif
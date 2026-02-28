using PangyaAPI.BinaryModels;
using Connector.DataBase;
using Connector.Table;
using Game.Defines;
using Game.Client.Inventory.Data.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Client.Inventory.Collection
{
    public class CharacterCollection : List<PlayerCharacterData>
    {
        public uint UID;
        CardEquipCollection fCard = null;
        public CardEquipCollection Card
        {
            get
            {
                return fCard;
            }
            set
            {
                fCard = value;
            }
        }


        public CharacterCollection(int PlayerUID)
        {
            UID = (uint)PlayerUID;
            Build(PlayerUID);
        }
        

        void Build(int UID)
        {
            using (var dbChar = new DB_pangya_character())
            {
                // Load characters using DB_pangya_character
                var characters = dbChar.SelectByUID(UID);

                foreach (var info in characters)
                {
                    var character = new PlayerCharacterData()
                    {
                        TypeID = (uint)info.TYPEID,
                        Index = (uint)info.CID,
                        HairColour = (ushort)(info.HAIR_COLOR ?? 0),
                        GiftFlag = (ushort)(info.GIFT_FLAG ?? 0),
                        Power = (byte)(info.POWER ?? 0),
                        Control = (byte)(info.CONTROL ?? 0),
                        Impact = (byte)(info.IMPACT ?? 0),
                        Spin = (byte)(info.SPIN ?? 0),
                        Curve = (byte)(info.CURVE ?? 0),
                        FCutinIndex = (uint)(info.CUTIN ?? 0),
                        NEEDUPDATE = false,
                        AuxPart = (uint)(info.AuxPart ?? 0),
                        AuxPart2 = (uint)(info.AuxPart2 ?? 0),
                    };

                    // Map PART_TYPEID (all nullable, default to 0)
                    character.EquipTypeID[0] = (uint)(info.PART_TYPEID_1 ?? 0);
                    character.EquipTypeID[1] = (uint)(info.PART_TYPEID_2 ?? 0);
                    character.EquipTypeID[2] = (uint)(info.PART_TYPEID_3 ?? 0);
                    character.EquipTypeID[3] = (uint)(info.PART_TYPEID_4 ?? 0);
                    character.EquipTypeID[4] = (uint)(info.PART_TYPEID_5 ?? 0);
                    character.EquipTypeID[5] = (uint)(info.PART_TYPEID_6 ?? 0);
                    character.EquipTypeID[6] = (uint)(info.PART_TYPEID_7 ?? 0);
                    character.EquipTypeID[7] = (uint)(info.PART_TYPEID_8 ?? 0);
                    character.EquipTypeID[8] = (uint)(info.PART_TYPEID_9 ?? 0);
                    character.EquipTypeID[9] = (uint)(info.PART_TYPEID_10 ?? 0);
                    character.EquipTypeID[10] = (uint)(info.PART_TYPEID_11 ?? 0);
                    character.EquipTypeID[11] = (uint)(info.PART_TYPEID_12 ?? 0);
                    character.EquipTypeID[12] = (uint)(info.PART_TYPEID_13 ?? 0);
                    character.EquipTypeID[13] = (uint)(info.PART_TYPEID_14 ?? 0);
                    character.EquipTypeID[14] = (uint)(info.PART_TYPEID_15 ?? 0);
                    character.EquipTypeID[15] = (uint)(info.PART_TYPEID_16 ?? 0);
                    character.EquipTypeID[16] = (uint)(info.PART_TYPEID_17 ?? 0);
                    character.EquipTypeID[17] = (uint)(info.PART_TYPEID_18 ?? 0);
                    character.EquipTypeID[18] = (uint)(info.PART_TYPEID_19 ?? 0);
                    character.EquipTypeID[19] = (uint)(info.PART_TYPEID_20 ?? 0);
                    character.EquipTypeID[20] = (uint)(info.PART_TYPEID_21 ?? 0);
                    character.EquipTypeID[21] = (uint)(info.PART_TYPEID_22 ?? 0);
                    character.EquipTypeID[22] = (uint)(info.PART_TYPEID_23 ?? 0);
                    character.EquipTypeID[23] = (uint)(info.PART_TYPEID_24 ?? 0);

                    // Map PART_IDX (all nullable, default to 0)
                    character.EquipIndex[0] = (uint)(info.PART_IDX_1 ?? 0);
                    character.EquipIndex[1] = (uint)(info.PART_IDX_2 ?? 0);
                    character.EquipIndex[2] = (uint)(info.PART_IDX_3 ?? 0);
                    character.EquipIndex[3] = (uint)(info.PART_IDX_4 ?? 0);
                    character.EquipIndex[4] = (uint)(info.PART_IDX_5 ?? 0);
                    character.EquipIndex[5] = (uint)(info.PART_IDX_6 ?? 0);
                    character.EquipIndex[6] = (uint)(info.PART_IDX_7 ?? 0);
                    character.EquipIndex[7] = (uint)(info.PART_IDX_8 ?? 0);
                    character.EquipIndex[8] = (uint)(info.PART_IDX_9 ?? 0);
                    character.EquipIndex[9] = (uint)(info.PART_IDX_10 ?? 0);
                    character.EquipIndex[10] = (uint)(info.PART_IDX_11 ?? 0);
                    character.EquipIndex[11] = (uint)(info.PART_IDX_12 ?? 0);
                    character.EquipIndex[12] = (uint)(info.PART_IDX_13 ?? 0);
                    character.EquipIndex[13] = (uint)(info.PART_IDX_14 ?? 0);
                    character.EquipIndex[14] = (uint)(info.PART_IDX_15 ?? 0);
                    character.EquipIndex[15] = (uint)(info.PART_IDX_16 ?? 0);
                    character.EquipIndex[16] = (uint)(info.PART_IDX_17 ?? 0);
                    character.EquipIndex[17] = (uint)(info.PART_IDX_18 ?? 0);
                    character.EquipIndex[18] = (uint)(info.PART_IDX_19 ?? 0);
                    character.EquipIndex[19] = (uint)(info.PART_IDX_20 ?? 0);
                    character.EquipIndex[20] = (uint)(info.PART_IDX_21 ?? 0);
                    character.EquipIndex[21] = (uint)(info.PART_IDX_22 ?? 0);
                    character.EquipIndex[22] = (uint)(info.PART_IDX_23 ?? 0);
                    character.EquipIndex[23] = (uint)(info.PART_IDX_24 ?? 0);

                    Add(character);
                }
            }
        }

        public int CharacterAdd(PlayerCharacterData Value)
        {
            Value.NEEDUPDATE = true;
            foreach (var chars in this)
            {
                if (chars.AuxPart > 0 && chars.AuxPart2 > 0)
                {
                    Value.AuxPart = chars.AuxPart;
                    Value.AuxPart2 = chars.AuxPart;
                    break;
                }
            }
            Value = CharacterPartDefault(Value);
            Add(Value);
            return Count;
        }

        private PlayerCharacterData CharacterPartDefault(PlayerCharacterData character)
        {
            //var _db = new PangyaEntities();

            //foreach (var info in _db.Pangya_Character_Part_Default.Where(c=> c.Char_TypeID == character.TypeID).ToList())
            //{
            //    for (int i = 0; i < 24; i++)
            //    {
            //        var valorPropriedade = info.GetType().GetProperty($"Parts_{i + 1}").GetValue(info, null);
            //        character.EquipTypeID[i] = Convert.ToUInt32(valorPropriedade);
            //    }
            //}
            return character;
        }


        public void UpdateCharacter(PlayerCharacterData character)
        {
            foreach (var Char in this)
            {
                if (Char.Index == character.Index && Char.TypeID == character.TypeID)
                {
                    Char.Update(character);
                }
            }
        }

        public byte[] CreateChar(PlayerCharacterData CharData, byte[] CardMap)
        {
            PangyaBinaryWriter Packet;

            Packet = new PangyaBinaryWriter();
            try
            {
                Packet.Write(CharData.TypeID);
                Packet.Write(CharData.Index);
                Packet.Write(CharData.HairColour);
                Packet.Write(CharData.GiftFlag);
                for (var Index = 0; Index < 24; Index++)
                {
                    Packet.Write(CharData.EquipTypeID[Index]);
                }
                for (var Index = 0; Index < 24; Index++)
                {
                    Packet.Write(CharData.EquipIndex[Index]);
                }
                Packet.WriteZero(216);
                Packet.Write(CharData.AuxPart);// anel da esquerda
                Packet.Write(CharData.AuxPart2);// anel da direita
                Packet.Write(CharData.AuxPart3);
                Packet.Write(CharData.AuxPart4);
                Packet.Write(CharData.AuxPart5);
                Packet.WriteUInt32(CharData.FCutinIndex); // CUTIN IDX
                Packet.WriteZero(12);
                Packet.WriteByte(CharData.Power);
                Packet.WriteByte(CharData.Control);
                Packet.WriteByte(CharData.Impact);
                Packet.WriteByte(CharData.Spin);
                Packet.WriteByte(CharData.Curve);
                Packet.WriteInt32(CharData.MasteryPoint);
                Packet.Write(CardMap, 40);
                Packet.WriteUInt32(0);
                Packet.WriteUInt32(0);
                return Packet.GetBytes();
            }
            finally
            {
                Packet.Dispose();
            }
        }

        public PlayerCharacterData GetCharByType(byte charType)
        {
            switch ((CharTypeByHairColor)charType)
            {
                case CharTypeByHairColor.Nuri:
                    return GetChar(67108864, CharType.bTypeID);
                case CharTypeByHairColor.Hana:
                    return GetChar(67108865, CharType.bTypeID);
                case CharTypeByHairColor.Azer:
                    return GetChar(67108866, CharType.bTypeID);
                case CharTypeByHairColor.Cecilia:
                    return GetChar(67108867, CharType.bTypeID);
                case CharTypeByHairColor.Max:
                    return GetChar(67108868, CharType.bTypeID);
                case CharTypeByHairColor.Kooh:
                    return GetChar(67108869, CharType.bTypeID);
                case CharTypeByHairColor.Arin:
                    return GetChar(67108870, CharType.bTypeID);
                case CharTypeByHairColor.Kaz:
                    return GetChar(67108871, CharType.bTypeID);
                case CharTypeByHairColor.Lucia:
                    return GetChar(67108872, CharType.bTypeID);
                case CharTypeByHairColor.Nell:
                    return GetChar(67108873, CharType.bTypeID);
                case CharTypeByHairColor.Spika:
                    return GetChar(67108874, CharType.bTypeID);
                case CharTypeByHairColor.NR:
                    return GetChar(67108875, CharType.bTypeID);
                case CharTypeByHairColor.HR:
                    return GetChar(67108876, CharType.bTypeID);
                case CharTypeByHairColor.CR:
                    return GetChar(67108878, CharType.bTypeID);
            }
            return null;
        }

        public PlayerCharacterData GetChar(UInt32 ID, CharType GetType)
        {
            switch (GetType)
            {
                case CharType.bTypeID:
                    foreach (PlayerCharacterData Char in this)
                    {
                        if (Char.TypeID == ID)
                        {
                            return Char;
                        }
                    }
                    return null;
                case CharType.bIndex:
                    foreach (PlayerCharacterData Char in this)
                    {
                        if (Char.Index == ID)
                        {
                            return Char;
                        }
                    }
                    return null;
            }
            return null;
        }

        public byte[] GetCharData(UInt32 CID)
        {
            foreach (PlayerCharacterData Char in this)
            {
                if (Char.Index == CID)
                {
                    return CreateChar(Char, Card.MapCard(CID));
                }
            }
            return new byte[513];
        }

        public byte[] Build()
        {
            PangyaBinaryWriter Packet;
            Packet = new PangyaBinaryWriter();
            try
            {
                Packet.Write(new byte[] { 0x70, 0x00 });
                Packet.WriteUInt16((ushort)this.Count);
                Packet.WriteUInt16((ushort)this.Count);
                foreach (PlayerCharacterData Char in this)
                {
                    Packet.Write(CreateChar(Char, Card.MapCard(Char.Index)));
                }
                return Packet.GetBytes();
            }
            finally
            {
                Packet.Dispose();
            }
        }
        /// <summary>
        /// String usada para salvar dados do Character, Status + Equipamentos
        /// </summary>
        /// <param name="UID">Player UID</param>
        /// <returns></returns>
        public string GetSqlUpdateCharacter()
        {
            StringBuilder SQLString;
            SQLString = new StringBuilder();
            try
            {
                foreach (PlayerCharacterData Char in this)
                {
                    if (Char.NEEDUPDATE)
                    {
                        SQLString.Append(Char.GetStringCharInfo());//string com informações do equipmento do char  
                    }
                    Char.SaveChar(UID);
                    Char.NEEDUPDATE = false;//seta como falso, para não causa erros ao salvar item
                }
                return SQLString.ToString();
            }
            finally
            {
                SQLString.Clear();
            }
        }
    }
}

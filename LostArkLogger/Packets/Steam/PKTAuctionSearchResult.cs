using AccessoryOptimizer.Models;
using static AccessoryOptimizer.Services.PermutationService;

namespace LostArkLogger
{
    public partial class PKTAuctionSearchResult
    {
        private BitReader _reader;

        public void SteamDecode(BitReader reader)
        {
            _reader = reader;
            byte[] bytes = _reader.ReadBytes(_reader.Data.Length);

            int numberOfItems = bytes.Take(2).ToArray()[0];
            bytes = RemoveFromByteArray(bytes, 2);

            List<Accessory> accessories = new List<Accessory>();
            for (int i = 0; i < numberOfItems; i++)
            {
                var itemIdBytes = bytes.Take(49).Reverse().Take(4).ToArray();
                var itemId2Bytes = bytes.Take(45).Reverse().Take(4).ToArray();
                int itemId = GetInt32Value(itemIdBytes);
                int item2Id = GetInt32Value(itemId2Bytes);

                (AccessoryType? accessoryType1, AccessoryRank accessoryRank1) = GetAccessoryType(itemId);
                (AccessoryType? accessoryType2, AccessoryRank accessoryRank2) = GetAccessoryType(item2Id);

                AccessoryType? accessoryType = null;
                AccessoryRank? accessoryRank = null;

                if (accessoryType1 != null)
                {
                    accessoryType = accessoryType1;
                    accessoryRank = accessoryRank1;
                }
                else if (accessoryType2 != null)
                {
                    accessoryType = accessoryType2;
                    accessoryRank = accessoryRank2;
                }

                if (accessoryType != null && accessoryType != null)
                {
                    switch (accessoryType)
                    {
                        case AccessoryType.Ring:
                        case AccessoryType.Earring:
                            {
                                Accessory? accessory = GetRingOrEarringAccessory(ref bytes, (AccessoryType)accessoryType, (AccessoryRank)accessoryRank);
                                if (accessory != null)
                                {
                                    accessories.Add(accessory);
                                }
                                break;
                            }
                        case AccessoryType.Necklace:
                            {
                                Accessory? accessory = GetNecklaceAccessory(ref bytes, (AccessoryType)accessoryType!, (AccessoryRank)accessoryRank);
                                if (accessory != null)
                                {
                                    accessories.Add(accessory);
                                }

                                break;
                            }

                    }
                }
                else
                {
                    var sus = true;
                }
            }

            Accessories = accessories;
        }

        private static (AccessoryType?, AccessoryRank) GetAccessoryType(int itemId)
        {
            AccessoryType? accessoryType = null;
            AccessoryRank accessoryRank = AccessoryRank.Legendary;

            switch ((EarringItemIds)itemId)
            {
                case EarringItemIds.Radiant_Destroyer_Earrings:
                case EarringItemIds.Radiant_Inquirer_Earrings:
                case EarringItemIds.Corrupted_Space_Earrings:
                case EarringItemIds.Corrupted_Time_Earrings:
                case EarringItemIds.Wailing_Chaos_Earrings:
                case EarringItemIds.Wailing_Aeon_Earrings:
                    {
                        accessoryType = AccessoryType.Earring;
                        accessoryRank = AccessoryRank.Relic;
                    }
                    break;
            }

            switch ((EarringItemIds)itemId)
            {
                case EarringItemIds.Splendid_Destroyer_Earring:
                case EarringItemIds.Splendid_Inquirer_Earring:
                case EarringItemIds.Twisted_Space_Earring:
                case EarringItemIds.Twisted_Time_Earring:
                case EarringItemIds.Fallen_Chaos_Earring:
                case EarringItemIds.Fallen_Aeon_Earring:
                    {
                        accessoryType = AccessoryType.Earring;
                        accessoryRank = AccessoryRank.Legendary;
                    }
                    break;
            }

            switch ((RingItemIds)itemId)
            {
                case RingItemIds.Radiant_Destroyer_Ring:
                case RingItemIds.Radiant_Inquirer_Ring:
                case RingItemIds.Corrupted_Space_Ring:
                case RingItemIds.Corrupted_Time_Ring:
                case RingItemIds.Wailing_Chaos_Ring:
                case RingItemIds.Wailing_Aeon_Ring:
                    {
                        accessoryType = AccessoryType.Ring;
                        accessoryRank = AccessoryRank.Relic;
                    }
                    break;
            }

            switch ((RingItemIds)itemId)
            {
                case RingItemIds.Splendid_Destroyer_Ring:
                case RingItemIds.Splendid_Inquirer_Ring:
                case RingItemIds.Twisted_Space_Ring:
                case RingItemIds.Twisted_Time_Ring:
                case RingItemIds.Fallen_Chaos_Ring:
                case RingItemIds.Fallen_Aeon_Ring:
                    {
                        accessoryType = AccessoryType.Ring;
                        accessoryRank = AccessoryRank.Legendary;
                    }
                    break;
            }

            switch ((NecklaceItemIds)itemId)
            {
                case NecklaceItemIds.Radiant_Inquirer_Necklace:
                case NecklaceItemIds.Corrupted_Time_Necklace:
                case NecklaceItemIds.Wailing_Chaos_Necklace:
                    {
                        accessoryType = AccessoryType.Necklace;
                        accessoryRank = AccessoryRank.Relic;
                    }
                    break;
            }

            switch ((NecklaceItemIds)itemId)
            {
                case NecklaceItemIds.Splendid_Inquirer_Necklace:
                case NecklaceItemIds.Twisted_Time_Necklace:
                case NecklaceItemIds.Fallen_Chaos_Necklace:
                    {
                        accessoryType = AccessoryType.Necklace;
                        accessoryRank = AccessoryRank.Legendary;
                    }
                    break;
            }

            return (accessoryType, accessoryRank);
        }

        private Accessory? GetNecklaceAccessory(ref byte[] bytes, AccessoryType accessoryType, AccessoryRank accessoryRank)
        {
            var earringBytes = bytes.Take(303).ToArray();
            bytes = RemoveFromByteArray(bytes, 303);

            int buyOutPrice = GetInt32Value(earringBytes.Take(4).Reverse().ToArray());

            if (buyOutPrice == 0)
            {
                return null;
            }

            int bidPrice = GetInt32Value(earringBytes.Skip(25).Take(4).Reverse().ToArray());

            int stat1Quantity = GetInt32Value(earringBytes.Skip(152).Take(4).Reverse().ToArray());
            int stat1Type = earringBytes.Skip(139).Take(1).ToArray()[0];

            int stat2Quantity = GetInt32Value(earringBytes.Skip(123).Take(4).Reverse().ToArray());
            int stat2Type = earringBytes.Skip(110).Take(1).ToArray()[0];

            int negEngravingAmount = earringBytes.Skip(181).Take(1).ToArray()[0];
            int negEngravingType = GetInt32Value(earringBytes.Skip(168).Take(4).Reverse().ToArray());

            int engraving1Amount = GetInt32Value(earringBytes.Skip(193).Take(4).Reverse().ToArray());
            int engraving1Type = GetInt32Value(earringBytes.Skip(197).Take(4).Reverse().ToArray());
            Engraving engraving1 = new Engraving(engraving1Type, engraving1Amount);

            int engraving2Amount = GetInt32Value(earringBytes.Skip(222).Take(4).Reverse().ToArray());
            int engraving2Type = GetInt32Value(earringBytes.Skip(226).Take(4).Reverse().ToArray());

            Engraving engraving2 = new Engraving(engraving2Type, engraving2Amount);

            Stats stats = new Stats(stat1Type, stat1Quantity, stat2Type, stat2Quantity);

            Stat_Type desiredStatType1 = PSO.DesiredStatType1;
            Stat_Type desiredStatType2 = PSO.DesiredStatType2;

            if ((stats.StatType1 != desiredStatType1 && stats.StatType1 != desiredStatType2) || (stats.StatType2 != desiredStatType1 && stats.StatType2 != desiredStatType2))
            {
                return null;
            }

            Accessory accessory = new Accessory(accessoryType, accessoryRank, GetStatQuality(accessoryType, accessoryRank, stat1Quantity, stat2Quantity), bidPrice, buyOutPrice, new List<Engraving>() { engraving1, engraving2 }, new Engraving(negEngravingType, negEngravingAmount), stats);
            return accessory;
        }

        private Accessory? GetRingOrEarringAccessory(ref byte[] bytes, AccessoryType accessoryType, AccessoryRank accessoryRank)
        {
            var earringBytes = bytes.Take(274).ToArray();
            bytes = RemoveFromByteArray(bytes, 274);

            int buyOutPrice = GetInt32Value(earringBytes.Take(4).Reverse().ToArray());

            if (buyOutPrice == 0)
            {
                return null;
            }

            int bidPrice = GetInt32Value(earringBytes.Skip(25).Take(4).Reverse().ToArray());
            int statQuantity = GetInt32Value(earringBytes.Skip(123).Take(4).Reverse().ToArray());
            int statType = earringBytes.Skip(110).Take(1).ToArray()[0];
            int negEngravingAmount = earringBytes.Skip(152).Take(1).ToArray()[0];
            int negEngravingType = GetInt32Value(earringBytes.Skip(139).Take(4).Reverse().ToArray());

            int engraving1Amount = GetInt32Value(earringBytes.Skip(164).Take(4).Reverse().ToArray());
            int engraving1Type = GetInt32Value(earringBytes.Skip(168).Take(4).Reverse().ToArray());
            Engraving engraving1 = new Engraving(engraving1Type, engraving1Amount);

            int engraving2Amount = GetInt32Value(earringBytes.Skip(193).Take(4).Reverse().ToArray());
            int engraving2Type = GetInt32Value(earringBytes.Skip(197).Take(4).Reverse().ToArray());
            Engraving engraving2 = new Engraving(engraving2Type, engraving2Amount);

            Accessory accessory = new Accessory(accessoryType, accessoryRank, GetStatQuality(accessoryType, accessoryRank, statQuantity), bidPrice, buyOutPrice, new List<Engraving>() { engraving1, engraving2 }, new Engraving(negEngravingType, negEngravingAmount), new Stats(statType, statQuantity));
            return accessory;
        }

        private byte[] RemoveFromByteArray(byte[] src, int amountToDelete)
        {
            byte[] dst = new byte[src.Length - amountToDelete];

            Array.Copy(src, amountToDelete, dst, 0, dst.Length);

            return dst;
        }

        private int GetStatQuality(AccessoryType accessoryType, AccessoryRank accessoryRank, int statQuantity, int stat2Quantity = 0)
        {
            decimal real = 0;
            int actualStat;

            switch (accessoryType)
            {
                case AccessoryType.Ring:
                    if (accessoryRank == AccessoryRank.Legendary)
                    {
                        actualStat = statQuantity - 130;
                        real = (decimal)(actualStat / 0.5);
                    }
                    else if (accessoryRank == AccessoryRank.Relic)
                    {
                        actualStat = statQuantity - 160;
                        real = (decimal)(actualStat / 0.4);
                    }

                    break;
                case AccessoryType.Earring:
                    if (accessoryRank == AccessoryRank.Legendary)
                    {
                        actualStat = statQuantity - 195;
                        real = (decimal)(actualStat / 0.75);
                    }
                    else if (accessoryRank == AccessoryRank.Relic)
                    {
                        actualStat = statQuantity - 240;
                        real = (decimal)(actualStat / 0.6);
                    }

                    break;
                case AccessoryType.Necklace:
                    if (accessoryRank == AccessoryRank.Legendary)
                    {
                        actualStat = statQuantity + stat2Quantity - 650;
                        real = (decimal)(actualStat / 2.5);
                    }
                    else if (accessoryRank == AccessoryRank.Relic)
                    {
                        actualStat = statQuantity + stat2Quantity - 800;
                        real = (decimal)(actualStat / 2);
                    }

                    break;
            }

            real = Math.Round(real, MidpointRounding.ToZero);

            if (real > 100 || real < 0)
            {
                bool sus = true;
            }

            return (int)real;
        }

        private static int GetInt32Value(byte[] bytes)
        {
            try
            {
                Array.Reverse(bytes);
                uint intValue = BitConverter.ToUInt32(bytes, 0);
                return Convert.ToInt32(intValue);

            }
            catch (Exception ex)
            {

                throw;
            }

        }
    }

    public enum EarringItemIds
    {
        // relic
        Radiant_Destroyer_Earrings = 213300061,
        Radiant_Inquirer_Earrings = 213300051,
        Corrupted_Space_Earrings = 213300021,
        Corrupted_Time_Earrings = 213300011,
        Wailing_Chaos_Earrings = 213300031,
        Wailing_Aeon_Earrings = 213300041,

        // legendary
        Splendid_Destroyer_Earring = 213200061,
        Splendid_Inquirer_Earring = 213200051,
        Twisted_Space_Earring = 213200021,
        Twisted_Time_Earring = 213200011,
        Fallen_Chaos_Earring = 213200031,
        Fallen_Aeon_Earring = 213200041
    }

    public enum RingItemIds
    {
        // relic
        Radiant_Destroyer_Ring = 213300062,
        Radiant_Inquirer_Ring = 213300052,
        Corrupted_Space_Ring = 213300022,
        Corrupted_Time_Ring = 213300012,
        Wailing_Chaos_Ring = 213300032,
        Wailing_Aeon_Ring = 213300042,

        // legendary
        Splendid_Destroyer_Ring = 213200062,
        Splendid_Inquirer_Ring = 213200052,
        Twisted_Space_Ring = 213200022,
        Twisted_Time_Ring = 213200012,
        Fallen_Chaos_Ring = 213200032,
        Fallen_Aeon_Ring = 213200042
    }

    public enum NecklaceItemIds
    {
        // relic
        Radiant_Inquirer_Necklace = 213300050,
        Corrupted_Time_Necklace = 213300010,
        Wailing_Chaos_Necklace = 213300030,

        // legendary
        Splendid_Inquirer_Necklace = 213200050,
        Twisted_Time_Necklace = 213200010,
        Fallen_Chaos_Necklace = 213200030,
    }
}

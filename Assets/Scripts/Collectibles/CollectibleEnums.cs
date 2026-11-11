using UnityEngine;

namespace Collector.Collectibles
{
    public enum CollectibleCategory
    {
        Robot,
        Taso,
        ToyCar,
        Promotional,
        MiniFigure,
        RetroHandheld,
        ActionFigure,
        BoxedToy
    }

    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum ItemCondition
    {
        Poor,
        Used,
        Good,
        VeryGood,
        Mint
    }

    /// <summary>Enum değerlerinin oyunda görünen Türkçe adları ve nadirlik renkleri.</summary>
    public static class CollectibleText
    {
        public static string ToDisplayName(this CollectibleCategory category) => category switch
        {
            CollectibleCategory.Robot => "Robot",
            CollectibleCategory.Taso => "Taso",
            CollectibleCategory.ToyCar => "Oyuncak Araba",
            CollectibleCategory.Promotional => "Promosyon",
            CollectibleCategory.MiniFigure => "Mini Figür",
            CollectibleCategory.RetroHandheld => "Retro Oyun Cihazı",
            CollectibleCategory.ActionFigure => "Aksiyon Figürü",
            CollectibleCategory.BoxedToy => "Kutulu Oyuncak",
            _ => category.ToString()
        };

        public static string ToDisplayName(this Rarity rarity) => rarity switch
        {
            Rarity.Common => "Yaygın",
            Rarity.Uncommon => "Az Bulunur",
            Rarity.Rare => "Nadir",
            Rarity.Epic => "Epik",
            Rarity.Legendary => "Efsanevi",
            _ => rarity.ToString()
        };

        public static string ToDisplayName(this ItemCondition condition) => condition switch
        {
            ItemCondition.Poor => "Kötü",
            ItemCondition.Used => "Kullanılmış",
            ItemCondition.Good => "İyi",
            ItemCondition.VeryGood => "Çok İyi",
            ItemCondition.Mint => "Kusursuz",
            _ => condition.ToString()
        };

        /// <summary>Arayüzde nadirliği gösteren renk (gri, yeşil, mavi, mor, turuncu).</summary>
        public static Color32 ToColor(this Rarity rarity) => rarity switch
        {
            Rarity.Common => new Color32(0xB8, 0xB4, 0xA8, 0xFF),
            Rarity.Uncommon => new Color32(0x7B, 0xC4, 0x7F, 0xFF),
            Rarity.Rare => new Color32(0x6F, 0xA8, 0xE8, 0xFF),
            Rarity.Epic => new Color32(0xB9, 0x7C, 0xE0, 0xFF),
            Rarity.Legendary => new Color32(0xF2, 0xA1, 0x4E, 0xFF),
            _ => new Color32(0xFF, 0xFF, 0xFF, 0xFF)
        };
    }
}

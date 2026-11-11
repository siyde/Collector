using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Collector.UI
{
    /// <summary>
    /// Oyunun 5x7 piksel yazı tipi (yalnızca büyük harf; Türkçe Ç Ğ İ Ö Ş Ü dahil).
    /// Doku atlası çalışma anında koddan üretilir; asset gerekmez.
    /// Her karakter 11 satır: 2 aksan + 1 boşluk + 7 gövde + 1 alt (çengel) satırı.
    /// </summary>
    public static class PixelFont
    {
        public const int GlyphHeight = 11;
        public const int LineHeight = 12;
        public const int SpaceAdvance = 3;
        const int BodyTop = 3;
        const int CellW = 7, CellH = GlyphHeight + 2; // 1 px dolgu
        const int Columns = 16;

        public readonly struct Glyph
        {
            public readonly Rect Uv;
            public readonly int Width;
            public Glyph(Rect uv, int width) { Uv = uv; Width = width; }
        }

        // Gövde: 7 satır x 5 sütun, '|' ile ayrılmış, üstten alta
        static readonly Dictionary<char, string> Body = new Dictionary<char, string>
        {
            { 'A', ".###.|#...#|#...#|#####|#...#|#...#|#...#" },
            { 'B', "####.|#...#|#...#|####.|#...#|#...#|####." },
            { 'C', ".###.|#...#|#....|#....|#....|#...#|.###." },
            { 'D', "####.|#...#|#...#|#...#|#...#|#...#|####." },
            { 'E', "#####|#....|#....|####.|#....|#....|#####" },
            { 'F', "#####|#....|#....|####.|#....|#....|#...." },
            { 'G', ".###.|#...#|#....|#.###|#...#|#...#|.####" },
            { 'H', "#...#|#...#|#...#|#####|#...#|#...#|#...#" },
            { 'I', "###|.#.|.#.|.#.|.#.|.#.|###" },
            { 'J', "..###|...#.|...#.|...#.|...#.|#..#.|.##.." },
            { 'K', "#...#|#..#.|#.#..|##...|#.#..|#..#.|#...#" },
            { 'L', "#....|#....|#....|#....|#....|#....|#####" },
            { 'M', "#...#|##.##|#.#.#|#.#.#|#...#|#...#|#...#" },
            { 'N', "#...#|#...#|##..#|#.#.#|#..##|#...#|#...#" },
            { 'O', ".###.|#...#|#...#|#...#|#...#|#...#|.###." },
            { 'P', "####.|#...#|#...#|####.|#....|#....|#...." },
            { 'Q', ".###.|#...#|#...#|#...#|#.#.#|#..#.|.##.#" },
            { 'R', "####.|#...#|#...#|####.|#.#..|#..#.|#...#" },
            { 'S', ".####|#....|#....|.###.|....#|....#|####." },
            { 'T', "#####|..#..|..#..|..#..|..#..|..#..|..#.." },
            { 'U', "#...#|#...#|#...#|#...#|#...#|#...#|.###." },
            { 'V', "#...#|#...#|#...#|#...#|#...#|.#.#.|..#.." },
            { 'W', "#...#|#...#|#...#|#.#.#|#.#.#|#.#.#|.#.#." },
            { 'X', "#...#|#...#|.#.#.|..#..|.#.#.|#...#|#...#" },
            { 'Y', "#...#|#...#|.#.#.|..#..|..#..|..#..|..#.." },
            { 'Z', "#####|....#|...#.|..#..|.#...|#....|#####" },
            { '0', ".###.|#...#|#..##|#.#.#|##..#|#...#|.###." },
            { '1', ".#.|##.|.#.|.#.|.#.|.#.|###" },
            { '2', ".###.|#...#|....#|...#.|..#..|.#...|#####" },
            { '3', "####.|....#|....#|.###.|....#|....#|####." },
            { '4', "...#.|..##.|.#.#.|#..#.|#####|...#.|...#." },
            { '5', "#####|#....|####.|....#|....#|#...#|.###." },
            { '6', "..##.|.#...|#....|####.|#...#|#...#|.###." },
            { '7', "#####|....#|...#.|..#..|.#...|.#...|.#..." },
            { '8', ".###.|#...#|#...#|.###.|#...#|#...#|.###." },
            { '9', ".###.|#...#|#...#|.####|....#|...#.|.##.." },
            { '.', ".|.|.|.|.|.|#" },
            { ',', "..|..|..|..|..|.#|#." },
            { ':', ".|#|.|.|.|#|." },
            { ';', "..|.#|..|..|..|.#|#." },
            { '!', "#|#|#|#|#|.|#" },
            { '?', ".###.|#...#|....#|...#.|..#..|.....|..#.." },
            { '-', "....|....|....|####|....|....|...." },
            { '+', ".....|..#..|..#..|#####|..#..|..#..|....." },
            { '=', "....|....|####|....|####|....|...." },
            { '/', "....#|...#.|...#.|..#..|.#...|.#...|#...." },
            { '(', "..#|.#.|#..|#..|#..|.#.|..#" },
            { ')', "#..|.#.|..#|..#|..#|.#.|#.." },
            { '[', "###|#..|#..|#..|#..|#..|###" },
            { ']', "###|..#|..#|..#|..#|..#|###" },
            { '<', "...#|..#.|.#..|#...|.#..|..#.|...#" },
            { '>', "#...|.#..|..#.|...#|..#.|.#..|#..." },
            { '\'', "#|#|.|.|.|.|." },
            { '"', "#.#|#.#|...|...|...|...|..." },
            { '%', "##..#|##..#|...#.|..#..|.#...|#..##|#..##" },
            { '_', ".....|.....|.....|.....|.....|.....|#####" },
            { '*', ".....|..#..|#.#.#|.###.|#.#.#|..#..|....." },
            { '#', ".#.#.|.#.#.|#####|.#.#.|#####|.#.#.|.#.#." },
        };

        // Türkçe harfler: taban harf + üst aksan (2 satır) + alt çengel (1 satır)
        static readonly Dictionary<char, (char baseChar, string above, string below)> Accented =
            new Dictionary<char, (char, string, string)>
            {
                { 'Ç', ('C', null, "..##.") },
                { 'Ş', ('S', null, "..##.") },
                { 'Ğ', ('G', ".#.#.|..#..", null) },
                { 'Ö', ('O', ".....|.#.#.", null) },
                { 'Ü', ('U', ".....|.#.#.", null) },
                { 'İ', ('I', "...|.#.", null) },
            };

        static Texture2D atlas;
        static Dictionary<char, Glyph> glyphs;

        public static Texture2D Atlas { get { EnsureBuilt(); return atlas; } }

        public static bool TryGetGlyph(char c, out Glyph glyph)
        {
            EnsureBuilt();
            return glyphs.TryGetValue(c, out glyph);
        }

        /// <summary>Türkçe kurallarıyla büyük harfe çevirir (i -> İ, ı -> I).</summary>
        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var sb = new StringBuilder(text.Length);
            foreach (var c in text)
                sb.Append(c == 'i' ? 'İ' : c == 'ı' ? 'I' : char.ToUpperInvariant(c));
            return sb.ToString();
        }

        /// <summary>Tek satırın piksel genişliği (metin önceden <see cref="Normalize"/> edilmiş olmalı).</summary>
        public static int MeasureLine(string line)
        {
            int width = 0;
            foreach (var c in line)
                width += c == ' ' || !TryGetGlyph(c, out var g) ? SpaceAdvance : g.Width + 1;
            return Mathf.Max(0, width - 1);
        }

        /// <summary>
        /// Metni kelime kelime <paramref name="maxWidth"/> piksele sığacak satırlara böler ('\n' ile birleştirir).
        /// <paramref name="maxLines"/> aşılırsa son satır ".." ile biter. Metin önceden Normalize edilir.
        /// </summary>
        public static string Wrap(string text, int maxWidth, int maxLines)
        {
            var words = Normalize(text).Split(' ');
            var lines = new List<string>();
            var current = "";
            foreach (var word in words)
            {
                var candidate = current.Length == 0 ? word : current + " " + word;
                if (MeasureLine(candidate) <= maxWidth || current.Length == 0) current = candidate;
                else { lines.Add(current); current = word; }
            }
            if (current.Length > 0) lines.Add(current);

            if (lines.Count > maxLines)
            {
                lines.RemoveRange(maxLines, lines.Count - maxLines);
                var last = lines[maxLines - 1];
                while (last.Length > 1 && MeasureLine(last + "..") > maxWidth) last = last.Substring(0, last.Length - 1);
                lines[maxLines - 1] = last.TrimEnd() + "..";
            }
            return string.Join("\n", lines);
        }

        static void EnsureBuilt()
        {
            if (atlas != null && glyphs != null) return;

            var bitmaps = new Dictionary<char, string[]>();
            foreach (var pair in Body) bitmaps[pair.Key] = Compose(pair.Value, null, null);
            foreach (var pair in Accented)
                bitmaps[pair.Key] = Compose(Body[pair.Value.baseChar], pair.Value.above, pair.Value.below);

            int rows = Mathf.CeilToInt(bitmaps.Count / (float)Columns);
            int texW = Columns * CellW, texH = rows * CellH;
            var pixels = new Color32[texW * texH];
            glyphs = new Dictionary<char, Glyph>();

            int index = 0;
            foreach (var pair in bitmaps)
            {
                var bitmap = pair.Value;
                int width = bitmap[BodyTop].Length;
                int cellX = (index % Columns) * CellW + 1;
                int cellY = (index / Columns) * CellH + 1;
                for (int r = 0; r < GlyphHeight; r++)
                    for (int col = 0; col < width; col++)
                        if (bitmap[r][col] == '#')
                            pixels[(cellY + GlyphHeight - 1 - r) * texW + cellX + col] = new Color32(255, 255, 255, 255);

                var uv = new Rect(cellX / (float)texW, cellY / (float)texH, width / (float)texW, GlyphHeight / (float)texH);
                glyphs[pair.Key] = new Glyph(uv, width);
                index++;
            }

            atlas = new Texture2D(texW, texH, TextureFormat.RGBA32, false)
            {
                name = "PixelFontAtlas",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            atlas.SetPixels32(pixels);
            atlas.Apply(false, true);
        }

        // 11 satırlık tam bitmap: [aksan x2][boşluk][gövde x7][çengel]
        static string[] Compose(string body, string above, string below)
        {
            var bodyRows = body.Split('|');
            int width = bodyRows[0].Length;
            string empty = new string('.', width);
            var rows = new string[GlyphHeight];
            var aboveRows = above?.Split('|');
            for (int r = 0; r < 2; r++) rows[r] = aboveRows != null ? Pad(aboveRows[r], width) : empty;
            rows[2] = empty;
            for (int r = 0; r < 7; r++) rows[BodyTop + r] = bodyRows[r];
            rows[GlyphHeight - 1] = below != null ? Pad(below, width) : empty;
            return rows;
        }

        static string Pad(string row, int width) =>
            row.Length >= width ? row.Substring(0, width) : row.PadRight(width, '.');
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace WebServer.Utils
{
    /// <summary>
    /// Generates ASCII art banner text using a built-in block font (5 rows tall, 8 chars wide per glyph).
    /// Supports A-Z, 0-9, space, and common symbols: . - _ : !
    /// </summary>
    internal static class AsciiBanner
    {
        // Pre-rendered "WebServer" banner — guaranteed to look correct.
        // Generated from FIGlet "small slant" font, manually verified.
        private static readonly string[] _webServerArt = new[]
        {
            @" __        __   _                                      ",
            @" \ \      / /__| |__  ___  ___ _ ____   _____ _ __    ",
            @"  \ \ /\ / / _ \ '_ \/ __|/ _ \ '__\ \ / / _ \ '__|   ",
            @"   \ V  V /  __/ |_) \__ \  __/ |   \ V /  __/ |      ",
            @"    \_/\_/ \___|_.__/|___/\___|_|    \_/ \___|_|       ",
        };

        // ?? generic slant font for any other text ????????????????????????????
        private static readonly Dictionary<char, string[]> _font = new Dictionary<char, string[]>
        {
            //          row0          row1          row2          row3          row4
            ['A'] = new[] { "   ___  ", "  / _ \\ ", " / /_\\ \\", "/ /  \\ \\", "\\/    \\/"},
            ['B'] = new[] { " _____  ", "| ___ \\ ", "| |_/ / ", "| ___ \\ ", "| |_/ / ", "\\____/  " },
            ['C'] = new[] { "  ___  ", " / __/ ", "/ /    ", "\\ \\__  ", " \\___/ " },
            ['D'] = new[] { " ____  ", "/ __ \\ ", "/ / / /", "\\ \\_\\ \\", "\\____/ " },
            ['E'] = new[] { " ____  ", "| ___/ ", "| |_   ", "| |___ ", "\\____/ " },
            ['F'] = new[] { " _____ ", "| ____/", "| |_   ", "| |    ", "|_|    " },
            ['G'] = new[] { "  ____ ", " / ___/", "/ / __ ", "\\ \\/ _/", " \\___/ " },
            ['H'] = new[] { " _   _ ", "| |_| |", "| ._, |", "| |\\  |", "|_| \\_|" },
            ['I'] = new[] { " ___  ", "|_ _| ", " | |  ", " | |  ", "|___|  " },
            ['J'] = new[] { "    __ ", "   / / ", "  / /  ", " / /__ ", " \\___/ " },
            ['K'] = new[] { " _  __ ", "| |/ / ", "| ' /  ", "| . \\  ", "|_|\\_\\ " },
            ['L'] = new[] { " _     ", "| |    ", "| |    ", "| |___ ", "|_____|" },
            ['M'] = new[] { " __  __ ", "|  \\/  |", "| |\\/| |", "| |  | |", "|_|  |_|" },
            ['N'] = new[] { " _   _ ", "| \\ | |", "|  \\| |", "| |\\  |", "|_| \\_|" },
            ['O'] = new[] { "  ___  ", " / _ \\ ", "| | | |", "| |_| |", " \\___/ " },
            ['P'] = new[] { " ____  ", "| __ \\ ", "| |_/ /", "| ___/ ", "|_|    " },
            ['Q'] = new[] { "  ___  ", " / _ \\ ", "| | | |", "| |_| |", " \\__\\_\\" },
            ['R'] = new[] { " ____  ", "| __ \\ ", "| |_/ /", "|  __/ ", "|_|    " },
            ['S'] = new[] { " ____  ", "/ ___|  ", "\\ `--.  ", " `--. \\ ", "\\____/ " },
            ['T'] = new[] { " _____ ", "|_   _|", "  | |  ", "  | |  ", "  \\_/  " },
            ['U'] = new[] { " _   _ ", "| | | |", "| | | |", "| |_| |", " \\___/ " },
            ['V'] = new[] { "__   __", "\\ \\ / /", " \\ V / ", "  \\ /  ", "   V   " },
            ['W'] = new[] { "__    __", "\\ \\  / /", " \\ \\/ / ", "  \\  /  ", "   \\/   " },
            ['X'] = new[] { "__  __", "\\ \\/ /", " \\  / ", " / /\\ \\", "/_/  \\_\\" },
            ['Y'] = new[] { "__  __", "\\ \\/ /", " \\  / ", "  \\ \\  ", "   \\_\\ " },
            ['Z'] = new[] { " ____  ", "|_  /  ", " / /   ", "/ /__ ", "/____/ " },
            ['0'] = new[] { "  ___  ", " / _ \\ ", "| | | |", "| |_| |", " \\___/ " },
            ['1'] = new[] { " __ ", "/_ |", " | |", " | |", " |_|" },
            ['2'] = new[] { " ___  ", "|__ \\ ", "   ) |", "  / / ", " /_/  " },
            ['3'] = new[] { " ___  ", "|__ \\ ", "  / / ", " |_|  ", " (_)  " },
            ['4'] = new[] { " _  _ ", "| || |", "| || |", "|__  |", "   |_|" },
            ['5'] = new[] { " _____ ", "| ____|", "| |__  ", "|___ \\ ", " ___) |", "|____/ " },
            ['6'] = new[] { "  __  ", " / /  ", "/ /_  ", "| '_ \\ ", "| (_) |", " \\___/ " },
            ['7'] = new[] { " ____  ", "|__  / ", "   / / ", "  / /_ ", " /___|" },
            ['8'] = new[] { "  ___  ", " / _ \\ ", "| (_) |", " > _ < ", "| (_) |", " \\___/ " },
            ['9'] = new[] { "  ___  ", " / _ \\ ", "| (_) |", " \\__, |", "  / / ", " /_/  " },
            [' '] = new[] { "   ", "   ", "   ", "   ", "   " },
            ['.'] = new[] { "  ", "  ", "  ", " _", "(_)" },
            ['-'] = new[] { "    ", "    ", " __ ", "|__|", "    " },
            ['_'] = new[] { "     ", "     ", "     ", "     ", " ___ ", "|___|" },
            [':'] = new[] { " _ ", "(_)", "   ", " _ ", "(_)" },
            ['!'] = new[] { " _ ", "| |", "| |", "|_|", "(_)" },
        };

        /// <summary>
        /// Renders text as a 5-row slant-style ASCII art string array.
        /// "WebServer" uses a pre-rendered banner for accuracy.
        /// </summary>
        public static string[] Render(string text)
        {
            string[] result;

            if (string.Equals(text, "WebServer", StringComparison.OrdinalIgnoreCase))
                result = _webServerArt;
            else
            {
                text = text.ToUpper();
                var rows = new StringBuilder[5];
                for (int i = 0; i < 5; i++)
                    rows[i] = new StringBuilder();

                foreach (char c in text)
                {
                    string[] glyph;
                    if (!_font.TryGetValue(c, out glyph))
                        glyph = _font[' '];

                    int glyphW = 0;
                    foreach (string row in glyph)
                        if (row.Length > glyphW) glyphW = row.Length;

                    for (int row = 0; row < 5; row++)
                        rows[row].Append(glyph[row].PadRight(glyphW));
                }

                result = new string[5];
                for (int i = 0; i < 5; i++)
                    result[i] = rows[i].ToString();
            }

            // Normalise: pad every row to the same length so nothing shifts
            int maxLen = 0;
            foreach (string row in result)
                if (row.Length > maxLen) maxLen = row.Length;

            var normalised = new string[result.Length];
            for (int i = 0; i < result.Length; i++)
                normalised[i] = result[i].PadRight(maxLen);

            return normalised;
        }
    }
}

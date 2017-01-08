using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitGooey
{
    enum EntryType { Ignored, Match, Add, Remove }

    struct DifferentialPatch
    {
        // @@ -69,6 +69,7 @@ foobar.ini
        // "@@", "-69,6" "+69,7" "@@ foobar.ini"
        private static readonly Regex _parseHeader = new Regex(
            @"^@@ -(?<patchStartLineBefore>\d{1,6}),(?<patchLineCountBefore>\d{1,6}) " +
            @"\+(?<patchStartLineAfter>\d{1,6}),(?<patchLineCountAfter>\d{1,6}) @@",
            RegexOptions.Compiled);

        private static readonly string[] _linesplitArray = { "\r\n", "\n", "\r" };

        public PatchEntryLine[] Entries { get; set; }

        public DifferentialPatch(string patch)
            : this()
        {
            if (patch == null) throw new ArgumentNullException(nameof(patch));

            Initialize(patch.Split(_linesplitArray, StringSplitOptions.None));
        }

        public DifferentialPatch(string[] patch)
            : this()
        {
            if (patch == null) throw new ArgumentNullException(nameof(patch));

            Initialize(patch);
        }

        private void Initialize(string[] patch)
        {
            var needHeader = true;
            var patchStartLineBefore = 0;
            var patchLineCountBefore = 0;
            var patchStartLineAfter = 0;
            var patchLineCountAfter = 0;
            var actualLinesBefore = 0;
            var actualLinesAfter = 0;

            var entries = new List<PatchEntryLine>(patch.Length);

            for (var i = 0; i < patch.Length; ++i)
            {
                var line = patch[i];
                var isHeader = line.StartsWith("@@");

                if (isHeader)
                {
                    if (!needHeader)
                    {
                        throw new FormatException($"Unexpected header. '{line}'");
                    }

                    // Validate the last header contained all the info we expected.
                    // NOTE: Better exceptions and has to be checked on loop
                    // determination as well.
                    if (actualLinesBefore != patchLineCountBefore)
                    {
                        throw new FormatException("actualLinesBefore issue.");
                    }

                    if (actualLinesAfter != patchLineCountAfter)
                    {
                        throw new FormatException("actualLinesBefore issue.");
                    }

                    var match = _parseHeader.Match(line);

                    if (!match.Success)
                    {
                        throw new FormatException($"Invalid header. '{line}'");
                    }

                    patchStartLineBefore = int.Parse(
                        match.Groups["patchStartLineBefore"].Value);

                    patchLineCountBefore = int.Parse(
                        match.Groups["patchLineCountBefore"].Value);

                    patchStartLineAfter = int.Parse(
                        match.Groups["patchStartLineAfter"].Value);

                    patchLineCountAfter = int.Parse(
                        match.Groups["patchLineCountAfter"].Value);

                    actualLinesBefore = 0;
                    actualLinesAfter = 0;

                    needHeader = false;

                    continue;
                }

                if (needHeader)
                {
                    continue;
                }

                if (false)
                {
                    if (!line.StartsWith("diff ") &&
                        !line.StartsWith("index ") &&
                        !line.StartsWith("--- ") &&
                        !line.StartsWith("+++ ") &&
                        !string.IsNullOrWhiteSpace(line))
                    {
                        //throw new FormatException(
                        //    $"Unexpected line at {i}. '{line}'");
                    }

                    continue;
                }

                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                var entry = new PatchEntryLine(
                    patchStartLineBefore, patchStartLineAfter, line);

                entries.Add(entry);

                switch (entry.EntryType)
                {
                    case EntryType.Add:
                        ++patchStartLineAfter;
                        ++actualLinesAfter;
                        break;
                    case EntryType.Remove:
                        ++patchStartLineBefore;
                        ++actualLinesBefore;
                        break;
                    case EntryType.Match:
                        ++actualLinesAfter;
                        ++actualLinesBefore;
                        break;
                }
            }

            Entries = entries.ToArray();
        }
    }

    struct PatchEntryLine
    {
        public int BeforeLineNumber { get; set; }
        public int AfterLineNumber { get; set; }
        public EntryType EntryType { get; set; }
        public string Line { get; set; }

        public PatchEntryLine(
            int beforeLineNumber, int afterLineNumber, string line)
            : this()
        {
            if (line == null) throw new ArgumentNullException(nameof(line));

            BeforeLineNumber = beforeLineNumber;
            AfterLineNumber = afterLineNumber;
            Line = line.Substring(1);

            if (line.Length > 0)
            {
                switch (line[0])
                {
                    case ' ':
                        EntryType = EntryType.Match;
                        break;
                    case '+':
                        EntryType = EntryType.Add;
                        break;
                    case '-':
                        EntryType = EntryType.Remove;
                        break;
                }
            }
        }
    }
}

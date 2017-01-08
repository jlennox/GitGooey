using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using GitGooey.History;
using LibGit2Sharp;

namespace GitGooey
{
    enum DiffDisplayMode
    {
        Diff,
        Contents
    }

    class GitGooeyHistory
    {
        public Repository Repository { get; private set; }
        public string Filename { get; private set; }
        public bool ShowAll { get; set; }

        public GitGooeyHistory(Repository repository, string filename)
        {
            Repository = repository;
            Filename = filename;
        }
    }

    public partial class MainWindow : Window
    {
        public ObservableCollection<PatchDisplay> Patches { get; } = new ObservableCollection<PatchDisplay>();
        private readonly BlameHunk[] _blameHunks;
        private readonly Repository _repo;
        private readonly Paragraph _viewerParagraph = new Paragraph();
        private readonly GitGooeyOptions _options;
        private readonly GitGooeyHistory _history;
        private DiffDisplayMode _diffDisplayMode;
        private PatchDisplay _lastPatchDisplayed;
        private readonly Dictionary<DiffDisplayMode, IPatchDisplay>
            _patchDisplays = new Dictionary<DiffDisplayMode, IPatchDisplay> {
                { DiffDisplayMode.Diff, new DiffPatchDisplay() },
                { DiffDisplayMode.Contents, new ContentPatchDisplay() }
            };

        public MainWindow()
        {
            var bo = new BlameOptions();

            _options = GitGooeyOptions.Load();

            AppDomain.CurrentDomain.ProcessExit += (e, s) => {
                _options.Save();
            };

            var args = Environment.GetCommandLineArgs();

            var filename = args[1];

            InitializeComponent();

            filename = Path.IsPathRooted(filename)
                ? filename
                : Path.Combine(Directory.GetCurrentDirectory(), filename);

            var directoryHistory = new List<string>(
                Path.GetDirectoryName(filename)
                    .Split(Path.DirectorySeparatorChar));

            string gitDir = null;

            while (directoryHistory.Count > 0)
            {
                var testDir = string.Join(
                    Path.DirectorySeparatorChar + "", directoryHistory);

                if (Directory.GetDirectories(testDir, ".git").Any())
                {
                    gitDir = testDir;
                    break;
                }

                directoryHistory.RemoveAt(directoryHistory.Count - 1);
            }

            if (gitDir == null)
            {
                throw new Exception("File not in git.");
            }

            // +1 to eat the directory char.
            filename = filename.Substring(gitDir.Length + 1);

            Title = $"GitGooey - {filename}";

            _repo = new Repository(gitDir);
            _history = new GitGooeyHistory(_repo, filename);

            var m = _repo.Blame(filename, bo);
            _blameHunks = m.ToArray();

            var existingChangesetShas = new HashSet<string>();

            foreach (var asd in PatchDisplay
                .FromBlameHunks(filename, _blameHunks, existingChangesetShas))
            {
                Patches.Add(asd);
            }

            UXPatchDisplay.Document = new FlowDocument(_viewerParagraph);

            DataContext = this;

            if (_options.WindowWidth > 0 && _options.WindowHeight > 0)
            {
                Width = _options.WindowWidth;
                Height = _options.WindowHeight;
            }
        }

        private void PatchList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var patchDisplay = PatchList.SelectedItem as PatchDisplay;

            if (patchDisplay == null)
            {
                return;
            }

            DisplayDiff(patchDisplay, _viewerParagraph);
        }

        private void DisplayDiff(PatchDisplay patchDisplay, Paragraph paragraph)
        {
            _lastPatchDisplayed = patchDisplay;
            var displayMode = _diffDisplayMode;

            paragraph.Inlines.Clear();

            IPatchDisplay display;

            if (!_patchDisplays.TryGetValue(displayMode, out display))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(displayMode), displayMode.ToString(),
                    "Internal error. Unknown type display type.");
            }

            display.Display(_history, patchDisplay, paragraph);
        }

        private void RedisplayDiff()
        {
            var lastPatchDisplayed = _lastPatchDisplayed;

            if (lastPatchDisplayed == null)
            {
                return;
            }

            DisplayDiff(lastPatchDisplayed, _viewerParagraph);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _options.WindowWidth = (int)Width;
            _options.WindowHeight = (int)Height;
        }

        private void DiffDisplayMode_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;

            _diffDisplayMode = (DiffDisplayMode)int.Parse((string)button.Tag);

            RedisplayDiff();
        }
    }

    public class CommitCollection
    {
    }

    public class PatchDisplay
    {
        public int Index { get; set; }
        public BlameHunk BlameHunk { get; set; }
        public TreeEntry TreeEntry { get; set; }

        public static IEnumerable<PatchDisplay> FromBlameHunks(string filename,
            IEnumerable<BlameHunk> collection, HashSet<string> existing)
        {
            var index = 0;
            foreach (var hunk in collection)
            {
                if (hunk.LineCount == 0 ||
                    existing.Contains(hunk.InitialCommit.Sha))
                {
                    continue;
                }

                var treeEntry = hunk.InitialCommit.Tree
                    .FirstOrDefault(t => t.Name == filename);

                if (treeEntry != null)
                {
                    if (existing.Contains(treeEntry.Target.Sha))
                    {
                        continue;
                    }

                    existing.Add(treeEntry.Target.Sha);
                }

                yield return new PatchDisplay {
                    Index = index++,
                    BlameHunk = hunk,
                    TreeEntry = treeEntry
                };
            }
        }
    }
}

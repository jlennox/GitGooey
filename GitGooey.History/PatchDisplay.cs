using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using LibGit2Sharp;

namespace GitGooey.History
{
    interface IPatchDisplay
    {
        void Display(
            GitGooeyHistory history,
            PatchDisplay patchDisplay,
            Paragraph paragraph);
    }

    class ContentPatchDisplay : IPatchDisplay
    {
        public void Display(
            GitGooeyHistory history,
            PatchDisplay patchDisplay,
            Paragraph paragraph)
        {
            var tree = patchDisplay.BlameHunk.InitialCommit.Tree;
            var file = tree.FirstOrDefault(t => t.Name == history.Filename);

            if (file == null)
            {
                return;
            }

            var gitObject = history.Repository.Lookup(file.Target.Id);

            if (gitObject == null)
            {
                return;
            }

            var blob = gitObject as Blob;

            if (blob == null)
            {
                paragraph.Inlines.Add(
                    new Run("Not a file changeset:\n" + gitObject));

                return;
            }

            if (blob.IsBinary)
            {
                paragraph.Inlines.Add(new Run("Not display binary content."));
                return;
            }

            var blobTextContent = blob.GetContentText();

            paragraph.Inlines.Add(new Run(blobTextContent));
        }
    }

    class DiffPatchDisplay : IPatchDisplay
    {
        public void Display(
            GitGooeyHistory history,
            PatchDisplay patchDisplay,
            Paragraph paragraph)
        {
            var inlines = paragraph.Inlines;

            if (patchDisplay.Index > 0)
            {
                var commit = patchDisplay.BlameHunk.InitialCommit;

                // TODO: Make enumerate parents correctly.
                var parent = commit.Parents.FirstOrDefault();

                if (parent == null)
                {
                    inlines.Add(new Run("Commit does not have a parent."));
                    return;
                }

                var patches = history.Repository.Diff.Compare<Patch>(
                    parent.Tree,
                    commit.Tree,
                    new[] { history.Filename });

                foreach (var patch in patches)
                {
                    var patchText = (patch.Patch ?? "").Split('\n');

                    foreach (var line in patchText)
                    {
                        if (!history.ShowAll && (line.StartsWith("+++") ||
                            line.StartsWith("---") ||
                            line.StartsWith("diff ") ||
                            line.StartsWith("index ")) ||
                            line.Length == 0)
                        {
                            continue;
                        }

                        switch (line[0])
                        {
                            case '+':
                                inlines.Add(new Run(line + "\n") {
                                    Foreground = Brushes.Green
                                });
                                break;
                            case '-':
                                inlines.Add(new Run(line + "\n") {
                                    Foreground = Brushes.Red
                                });
                                break;
                            case '@':
                                inlines.Add(new Run(line + "\n" + "\n") {
                                    Foreground = Brushes.DeepPink,
                                    Background = Brushes.LightGreen
                                });
                                break;
                            default:
                                inlines.Add(new Run(line + "\n"));
                                break;
                        }
                    }

                    inlines.Add(new Run("\n"));
                }
            }
        }
    }
}

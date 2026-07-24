namespace PersonalAi.Notes;

static class MarkdownNoteReader
{
    public static IEnumerable<(string FileName, string Text)> ReadAll(string directory)
    {
        foreach (var path in Directory.EnumerateFiles(directory, "*.md"))
            yield return (Path.GetFileName(path), File.ReadAllText(path));
    }
}

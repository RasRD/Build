using System.Net;
using System.Text;

namespace PersonalAi.Evaluation;

static class RunLogHtmlReport
{
    public static string Render(RunLog log)
    {
        var sb = new StringBuilder();
        sb.Append("""
            <!doctype html>
            <html>
            <head>
            <meta charset="utf-8">
            <title>PersonalAi run report</title>
            <style>
                body { font-family: -apple-system, sans-serif; margin: 2rem; color: #1a1a1a; }
                h1 { font-size: 1.3rem; }
                .meta { color: #555; margin-bottom: 1.5rem; }
                .meta span { margin-right: 1.5rem; }
                .query { border: 1px solid #ddd; border-radius: 6px; padding: 1rem; margin-bottom: 1.5rem; }
                .query h2 { font-size: 1rem; margin: 0 0 0.3rem; }
                .badge { display: inline-block; padding: 0.1rem 0.5rem; border-radius: 4px; font-size: 0.85rem; }
                .badge.yes { background: #d7f5dd; color: #1a7431; }
                .badge.no { background: #fbdada; color: #a11d1d; }
                table { border-collapse: collapse; width: 100%; margin-top: 0.7rem; }
                th, td { border: 1px solid #eee; padding: 0.4rem 0.6rem; text-align: left; vertical-align: top; font-size: 0.9rem; }
                th { background: #fafafa; }
                .preview { color: #444; font-size: 0.85rem; }
            </style>
            </head>
            <body>
            """);

        sb.Append($"<h1>PersonalAi run report</h1>");
        sb.Append("<div class=\"meta\">");
        sb.Append($"<span>Started: {log.StartedAtUtc:u}</span>");
        sb.Append($"<span>Completed: {log.CompletedAtUtc:u}</span>");
        sb.Append($"<span>Chunks: {log.ChunkCount}</span>");
        sb.Append($"<span>Top-K: {log.Parameters.TopK}</span>");
        sb.Append($"<span>Judge model: {Escape(log.Parameters.JudgeModel)}</span>");
        sb.Append("</div>");

        foreach (var query in log.Queries)
        {
            var badgeClass = query.ExpectedInTopK ? "yes" : "no";
            var badgeText = query.ExpectedInTopK ? "expected in top-K" : "expected NOT in top-K";

            sb.Append("<div class=\"query\">");
            sb.Append($"<h2>{Escape(query.Query)}</h2>");
            sb.Append($"<div><span class=\"badge {badgeClass}\">{badgeText}</span> ");
            sb.Append($"expected note: <code>{Escape(query.ExpectedNoteFile)}</code></div>");

            sb.Append("<table><thead><tr>");
            sb.Append("<th>Score</th><th>Source file</th><th>Judge</th><th>Reasoning</th><th>Chunk preview</th>");
            sb.Append("</tr></thead><tbody>");
            foreach (var chunk in query.Chunks)
            {
                var judgeBadgeClass = chunk.JudgeRelevant ? "yes" : "no";
                var judgeBadgeText = chunk.JudgeRelevant ? "relevant" : "not relevant";
                sb.Append("<tr>");
                sb.Append($"<td>{chunk.Score:F3}</td>");
                sb.Append($"<td>{Escape(chunk.SourceFile)}</td>");
                sb.Append($"<td><span class=\"badge {judgeBadgeClass}\">{judgeBadgeText}</span></td>");
                sb.Append($"<td>{Escape(chunk.JudgeReasoning)}</td>");
                sb.Append($"<td class=\"preview\">{Escape(chunk.ChunkPreview)}</td>");
                sb.Append("</tr>");
            }
            sb.Append("</tbody></table>");
            sb.Append("</div>");
        }

        sb.Append("</body></html>");
        return sb.ToString();
    }

    static string Escape(string text) => WebUtility.HtmlEncode(text);
}

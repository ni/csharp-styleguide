using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Text;
using NationalInstruments.Analyzers.TestUtilities.Markers;

namespace NationalInstruments.Analyzers.TestUtilities
{
    /// <summary>
    /// Helper class that parses markup for markers.
    /// </summary>
    public class TestMarkup
    {
        private const string MissingSyntaxTemplate = "Saw {0} without matching {1}";
        private const string DiagnosticPositionSyntax = "<|>";
        private const string DiagnosticArgumentSyntax = "<?>";
        private const string DiagnosticTextStartSyntax = "<|";
        private const string DiagnosticTextEndSyntax = "|>";

        private enum MarkupType
        {
            Diagnostic,
        }

        private interface IMarkupDefinition
        {
            MarkupType Type { get; }
        }

        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1306:FieldNamesMustBeginWithLowerCaseLetter", Justification = "Collection is read-only")]
        private ReadOnlyCollection<PositionMarkupDefinition> PositionMarkupDefinitions { get; } = new ReadOnlyCollection<PositionMarkupDefinition>(
            new[]
            {
                new PositionMarkupDefinition(MarkupType.Diagnostic, DiagnosticPositionSyntax),
                new PositionMarkupDefinition(MarkupType.Diagnostic, DiagnosticArgumentSyntax),
            });

        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1306:FieldNamesMustBeginWithLowerCaseLetter", Justification = "Collection is read-only")]
        private ReadOnlyCollection<CaptureMarkupDefinition> CaptureMarkupDefinitions { get; } = new ReadOnlyCollection<CaptureMarkupDefinition>(
            new[]
            {
                new CaptureMarkupDefinition(MarkupType.Diagnostic, DiagnosticTextStartSyntax, DiagnosticTextEndSyntax),
            });

        /// <summary>
        /// Extracts <see cref="SourceMarker"/> instances from the provided <paramref name="markup"/> and returns
        /// the original <paramref name="source"/>.
        /// </summary>
        /// <param name="markup">
        /// Source code with special markers that denote positions and, optionally, additional information.
        /// </param>
        /// <param name="source"><paramref name="markup"/> minus the special markers.</param>
        /// <returns>A list of <see cref="SourceMarker"/>s that were extracted from the <paramref name="markup"/>.</returns>
        public IList<SourceMarker> Parse(string markup, out string source)
        {
            if (!ContainsMarkers(markup))
            {
                source = markup;
                return new SourceMarker[] { };
            }

            var markupBySpan = new Dictionary<TextSpan, Markup?>();

            for (var i = 0; i < markup.Length; ++i)
            {
                // Look for markup that captures a position
                foreach (var markupDefinition in PositionMarkupDefinitions)
                {
                    if (TryGetPositionMarkup(markup, i, markupDefinition, out var positionMarkup, out var span))
                    {
                        markupBySpan.Add(span, positionMarkup);
                    }
                }

                // Look for markup that captures a position and text
                foreach (var markupDefinition in CaptureMarkupDefinitions)
                {
                    if (TryGetCaptureMarkup(markup, i, markupDefinition, out var captureMarkup, out var startSpan, out var endSpan))
                    {
                        if (markupBySpan.Keys.Any(x => x.OverlapsWith(startSpan)))
                        {
                            // Prevent treating the matched characters as capture markup if they've already been recognized
                            // as position markup. For example, if currentMarkupString is <|>, then the position markup
                            // syntax of <|> and the capture markup ending syntax of |> will match. In this case, we only
                            // want the former to match.
                            continue;
                        }

                        markupBySpan.Add(startSpan, captureMarkup);
                        markupBySpan.Add(endSpan, captureMarkup);
                    }

                    if (i == markup.Length - 1)
                    {
                        // We're at the very last character of the markup; do we have any unclosed instances of capture markup?
                        if (markupDefinition.StartSpans.Count > 0)
                        {
                            markupDefinition.StartSpans.Clear();    // reset for the next caller
                            throw new InvalidOperationException(
                                string.Format(
                                    CultureInfo.CurrentCulture,
                                    MissingSyntaxTemplate,
                                    markupDefinition.StartSyntax,
                                    markupDefinition.EndSyntax));
                        }
                    }
                }
            }

            source = GetSourceFromMarkup(markup, markupBySpan);

            // We must call Distinct() because the same capture markup is added twice:
            // once for the syntax start and once for the syntax finish spans
            return CreateSourceMarkers(markupBySpan.Values.Distinct(), source)
                .OrderBy(x => x.Line)
                .ThenBy(x => x.Column).ToList();
        }

        private bool ContainsMarkers(string markup)
        {
            return PositionMarkupDefinitions.Any(x => markup.Contains(x.Syntax))
                || CaptureMarkupDefinitions.Any(x => markup.Contains(x.StartSyntax) || markup.Contains(x.EndSyntax));
        }

        private bool TryGetPositionMarkup(
            string markup,
            int index,
            PositionMarkupDefinition markupDefinition,
            out PositionMarkup? positionMarkup,
            out TextSpan span)
        {
            var currentMarkup = markup.Substring(0, index + 1);

            // Note: syntax with regex currently does not exist, but allow it in the future
            Match match;
            if ((match = Regex.Match(currentMarkup, $"{Regex.Escape(markupDefinition.Syntax)}$")).Success)
            {
                span = GetTextSpanForSyntax(index, match.Value);
                positionMarkup = new PositionMarkup(markupDefinition, span.Start, match.Groups);
                return true;
            }

            // Use defaults (values shouldn't be used)
            span = default;
            positionMarkup = null;

            return false;
        }

        private bool TryGetCaptureMarkup(
            string markup,
            int index,
            CaptureMarkupDefinition markupDefinition,
            out CaptureMarkup? captureMarkup,
            out TextSpan startSpan,
            out TextSpan endSpan)
        {
            var currentMarkup = markup.Substring(0, index + 1);

            if (currentMarkup.EndsWith(markupDefinition.StartSyntax, false, CultureInfo.InvariantCulture))
            {
                // Found starting syntax
                var span = GetTextSpanForSyntax(index, markupDefinition.StartSyntax);
                markupDefinition.StartSpans.Push(span);
            }

            if (currentMarkup.EndsWith(markupDefinition.EndSyntax, false, CultureInfo.InvariantCulture))
            {
                // Found ending syntax
                if (markupDefinition.StartSpans.Count > 0)
                {
                    startSpan = markupDefinition.StartSpans.Pop();
                    endSpan = GetTextSpanForSyntax(index, markupDefinition.EndSyntax);
                    captureMarkup = new CaptureMarkup(markupDefinition, startSpan.Start, endSpan.Start);
                    return true;
                }
                else
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            MissingSyntaxTemplate,
                            markupDefinition.EndSyntax,
                            markupDefinition.StartSyntax));
                }
            }

            // Use defaults (values shouldn't be used)
            startSpan = default;
            endSpan = default;
            captureMarkup = null;

            return false;
        }

        private TextSpan GetTextSpanForSyntax(int i, string syntax)
        {
            return new TextSpan(i - (syntax.Length - 1), syntax.Length);
        }

        private string GetSourceFromMarkup(string markup, Dictionary<TextSpan, Markup?> markupBySpan)
        {
            var removed = 0;
            var output = new StringBuilder(markup);

            // Iterate through all spans start to finish so that they can be removed from the markup to produce the source
            foreach (var span in markupBySpan.Keys.OrderBy(x => x.Start))
            {
                // Remove syntax from markup
                output.Remove(span.Start - removed, span.Length);

                var markupInstance = markupBySpan[span];

                // Adjust markup indexes so that they're correct for the source
                if (markupInstance is PositionMarkup positionMarkup)
                {
                    positionMarkup.Index -= removed;
                }
                else if (markupInstance is CaptureMarkup captureMarkup)
                {
                    if (span.Start == captureMarkup.StartIndex)
                    {
                        captureMarkup.StartIndex -= removed;
                    }
                    else
                    {
                        captureMarkup.EndIndex -= removed;
                    }
                }

                removed += span.Length;
            }

            return output.ToString();
        }

        private IEnumerable<SourceMarker> CreateSourceMarkers(IEnumerable<Markup?> markups, string source)
        {
            foreach (var markup in markups)
            {
                switch (markup?.Type)
                {
                    case MarkupType.Diagnostic:
                        if (markup is PositionMarkup positionMarkup)
                        {
                            var point = GetLineAndColumnFromIndex(positionMarkup.Index, source);

                            switch (positionMarkup.Syntax)
                            {
                                case DiagnosticArgumentSyntax:
                                    yield return new DiagnosticArgumentMarker(point.Item1, point.Item2);
                                    break;

                                case DiagnosticPositionSyntax:
                                    yield return new DiagnosticPositionMarker(point.Item1, point.Item2);
                                    break;

                                default:
                                    throw new InvalidOperationException($"Unknown syntax: {positionMarkup.Syntax}");
                            }
                        }
                        else
                        {
                            var captureMarkup = (CaptureMarkup)markup;
                            var point = GetLineAndColumnFromIndex(captureMarkup.StartIndex, source);

                            yield return new DiagnosticTextMarker(
                                point.Item1,
                                point.Item2,
                                source.Substring(captureMarkup.StartIndex, captureMarkup.EndIndex - captureMarkup.StartIndex));
                        }

                        break;

                    default:
                        throw new InvalidOperationException($"Unknown markup type: {markup?.Type}");
                }
            }
        }

        private Tuple<int, int> GetLineAndColumnFromIndex(int index, string source)
        {
            var line = 0;
            var column = 0;
            var seen = 0;

            foreach (var row in Regex.Split(source, "(\r\n|\r|\n)"))
            {
                if (row.Length > 0 && row.Trim().Length == 0)
                {
                    // Since we capture the line ending in the Regex.Split above to know how many
                    // characters we've "seen", some rows will be nothing but line ending characters.
                    // These special rows shouldn't increase our line count as they really belong to the
                    // previous line. In the case where the source contains multiple blank lines, one
                    // of the split entries will be "". This does not match the criteria above and will
                    // therefore correctly increase the line count.
                    seen += row.Length;
                    continue;
                }

                line++;

                if (seen + row.Length >= index)
                {
                    column = index - seen + 1;
                    break;
                }

                seen += row.Length;
            }

            return Tuple.Create(line, column);
        }

        private struct PositionMarkupDefinition : IMarkupDefinition
        {
            public PositionMarkupDefinition(MarkupType type, string syntax)
            {
                Type = type;
                Syntax = syntax;
            }

            public MarkupType Type { get; }

            public string Syntax { get; }
        }

        private struct CaptureMarkupDefinition : IMarkupDefinition
        {
            public CaptureMarkupDefinition(MarkupType type, string startSyntax, string endSyntax)
            {
                Type = type;
                StartSyntax = startSyntax;
                EndSyntax = endSyntax;
                StartSpans = new Stack<TextSpan>();
            }

            public MarkupType Type { get; }

            public string StartSyntax { get; }

            public string EndSyntax { get; }

            public Stack<TextSpan> StartSpans { get; }
        }

        private abstract class Markup
        {
            public Markup(IMarkupDefinition definition)
            {
                Type = definition.Type;
            }

            public MarkupType Type { get; }
        }

        private class PositionMarkup : Markup
        {
            public PositionMarkup(PositionMarkupDefinition definition, int index, GroupCollection groups)
                : base(definition)
            {
                Syntax = definition.Syntax;
                Index = index;
                CapturedValues = groups.Cast<Group>().Skip(1).Select(x => x.Value);
            }

            public string Syntax { get; }

            public int Index { get; set; }

            public IEnumerable<string> CapturedValues { get; }
        }

        private class CaptureMarkup : Markup
        {
            public CaptureMarkup(CaptureMarkupDefinition definition, int startIndex, int endIndex)
                : base(definition)
            {
                StartSyntax = definition.StartSyntax;
                EndSyntax = definition.EndSyntax;

                StartIndex = startIndex;
                EndIndex = endIndex;
            }

            public string StartSyntax { get; }

            public string EndSyntax { get; }

            public int StartIndex { get; set; }

            public int EndIndex { get; set; }
        }
    }
}

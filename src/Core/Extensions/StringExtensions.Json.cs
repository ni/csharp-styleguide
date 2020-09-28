using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NationalInstruments.Tools.Extensions
{
    public static partial class StringExtensions
    {
        private const string DefaultIndention = "    ";

        private enum ParserState
        {
            RawText,
            OpeningDelimiter,
            SecondDelimiter,
            ReadingTemplateName,
        }

        public static string FormatJson(this string json, string indention = DefaultIndention)
        {
            var state = new FormatState();

            for (var i = 0; i < json.Length; i++)
            {
                var ch = json[i];
                switch (ch)
                {
                    case '{':
                    case '[':
                    {
                        OpenObjectOrArray(i, ch, state, indention);
                        break;
                    }

                    case '}':
                    case ']':
                    {
                        CloseObjectOrArray(ch, state, indention);
                        break;
                    }

                    case '"':
                    {
                        Quote(json, i, state);
                        break;
                    }

                    case ',':
                    {
                        Comma(state, indention);
                        break;
                    }

                    case ':':
                    {
                        Colon(state);
                        break;
                    }

                    default:
                    {
                        Default(ch, state);
                        break;
                    }
                }
            }

            return Regex.Replace(state.Builder.ToString(), @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);
        }

        private static void OpenObjectOrArray(int index, char currentCharacter, FormatState state, string indention)
        {
            if (index == 0)
            {
                state.Builder.Append(currentCharacter);
                state.Builder.AppendLine();

                foreach (var item in Enumerable.Range(0, ++state.Indent))
                {
                    state.Builder.Append(indention);
                }

                state.JustOpened = true;
                state.JustClosed = false;
                return;
            }

            if (!state.Quoted)
            {
                state.Builder.AppendLine();
                foreach (var item in Enumerable.Range(0, state.Indent))
                {
                    state.Builder.Append(indention);
                }

                state.Builder.Append(currentCharacter);
                state.Builder.AppendLine();
                foreach (var item in Enumerable.Range(0, ++state.Indent))
                {
                    state.Builder.Append(indention);
                }

                state.JustOpened = true;
                state.JustClosed = false;
            }
            else
            {
                state.Builder.Append(currentCharacter);
            }
        }

        private static void CloseObjectOrArray(char currentCharacter, FormatState state, string indention)
        {
            if (!state.Quoted)
            {
                if (!state.JustOpened || state.JustClosed)
                {
                    state.Builder.AppendLine();

                    foreach (var item in Enumerable.Range(0, --state.Indent))
                    {
                        state.Builder.Append(indention);
                    }
                }
                else
                {
                    state.Builder.Remove(state.Builder.Length - indention.Length, indention.Length);
                    state.Indent--;
                }

                state.JustClosed = true;
            }

            state.Builder.Append(currentCharacter);
        }

        private static void Quote(string json, int index, FormatState state)
        {
            state.JustOpened = false;
            state.JustClosed = false;
            state.Builder.Append('"');
            var escaped = false;

            while (index > 0 && json[--index] == '\\')
            {
                escaped = !escaped;
            }

            if (!escaped)
            {
                state.Quoted = !state.Quoted;
            }
        }

        private static void Comma(FormatState state, string indention)
        {
            state.JustOpened = false;
            state.JustClosed = false;
            state.Builder.Append(',');

            if (!state.Quoted)
            {
                state.Builder.AppendLine();

                foreach (var item in Enumerable.Range(0, state.Indent))
                {
                    state.Builder.Append(indention);
                }
            }
        }

        private static void Colon(FormatState state)
        {
            state.JustOpened = false;
            state.JustClosed = false;
            state.Builder.Append(':');

            if (!state.Quoted)
            {
                state.Builder.Append(" ");
            }
        }

        private static void Default(char currentCharacter, FormatState state)
        {
            if (!state.Quoted && char.IsWhiteSpace(currentCharacter))
            {
                return;
            }

            state.JustOpened = false;
            state.JustClosed = false;
            state.Builder.Append(currentCharacter);
        }

        private class FormatState
        {
            public StringBuilder Builder { get; } = new StringBuilder();

            public int Indent { get; set; }

            public bool Quoted { get; set; }

            public bool JustOpened { get; set; }

            public bool JustClosed { get; set; }
        }
    }
}

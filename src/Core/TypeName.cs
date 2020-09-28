using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace NationalInstruments.Tools
{
    public class TypeName
    {
        public TypeName(string fullTypeName)
        {
            var match = Regex.Match(fullTypeName, @"^(?<TypeName>\w*(`\d+)?((\.|\+)\w+(`\d+)?)*)(\[(?<GenericTypes>\[.*\])\])?(?<ArraySpecifiers>\[(\[|\]|,)*\])?,\s?(?<Assembly>.*)$");
            if (!match.Success)
            {
                Type = fullTypeName;
                return;
            }

            Type = match.Groups["TypeName"].Value;
            Assembly = match.Groups["Assembly"].Value;
            if (match.Groups["ArraySpecifiers"].Success)
            {
                ArraySpecifiers = match.Groups["ArraySpecifiers"].Value;
            }

            if (match.Groups["GenericTypes"].Success)
            {
                GenericParameters = new List<TypeName>();
                foreach (var nestedType in ExtractNestedTypes(match.Groups["GenericTypes"].Value))
                {
                    GenericParameters.Add(new TypeName(nestedType));
                }
            }
        }

        public string Type { get; set; }

        public string ArraySpecifiers { get; set; }

        public string Assembly { get; set; }

        public IList<TypeName> GenericParameters { get; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Type);
            if (GenericParameters != null && GenericParameters.Count > 0)
            {
                sb.Append("[");
                foreach (var type in GenericParameters)
                {
                    sb.Append("[");
                    sb.Append(type);
                    sb.Append("]");
                    sb.Append(",");
                }

                sb.Length--;
                sb.Append("]");
            }

            if (!string.IsNullOrEmpty(ArraySpecifiers))
            {
                sb.Append(ArraySpecifiers);
            }

            if (!string.IsNullOrEmpty(Assembly))
            {
                sb.Append(", ");
                sb.Append(Assembly);
            }

            return sb.ToString();
        }

        private static IEnumerable<string> ExtractNestedTypes(string typeList)
        {
            var sb = new StringBuilder();
            var identation = 0;
            foreach (var character in typeList)
            {
                switch (character)
                {
                    case '[':
                        ++identation;
                        if (identation > 1)
                        {
                            sb.Append(character);
                        }

                        break;
                    case ']':
                        --identation;
                        if (identation == 0)
                        {
                            yield return sb.ToString();
                            sb.Clear();
                        }
                        else
                        {
                            sb.Append(character);
                        }

                        break;
                    default:
                        if (identation > 0)
                        {
                            sb.Append(character);
                        }

                        break;
                }
            }
        }
    }
}

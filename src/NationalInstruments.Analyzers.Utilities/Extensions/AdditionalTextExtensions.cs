using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace NationalInstruments.Analyzers.Utilities.Extensions
{
    public static class AdditionalTextExtensions
    {
        private static readonly Encoding _utf8bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        private static readonly SourceText _emptySourceText = SourceText.From(string.Empty, _utf8bom, SourceHashAlgorithm.Sha256);

        public static SourceText GetTextOrEmpty(this AdditionalText text, CancellationToken cancellationToken)
            => text.GetText(cancellationToken) ?? _emptySourceText;
    }
}

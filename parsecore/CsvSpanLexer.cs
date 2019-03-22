using System;
using System.Runtime.CompilerServices;

namespace parsecore
{
    sealed class CsvSpanLexer
    {
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public CsvTokenType TryParse(ref ReadOnlySpan<byte> sequence, bool isComplete, out ReadOnlySpan<byte> token)
        {
            ReadOnlySpan<byte> span = sequence;

            for (int i = 0; i < span.Length; ++i)
            {
                byte x = span[i];

                if (x == ',')
                {
                    token = span.Slice(0, i);
                    sequence = span.Slice(i + 1);

                    return CsvTokenType.ColumnEnd;
                }

                if (x == '\r')
                {
                    ReadOnlySpan<byte> tokenSequence = span.Slice(0, i);

                    if ((i + 1) == span.Length)
                    {
                        // ran out of data, not sure if \r\n or not.

                        if (i == 0)
                        {
                            token = default;
                            return CsvTokenType.NeedMore;
                        }

                        // return a partial token.
                        token = tokenSequence;
                        sequence = span.Slice(i);

                        return CsvTokenType.ColumnPart;
                    }

                    x = span[i + 1];
                    if (x == '\n')
                    {
                        // found \r\n.
                        token = tokenSequence;
                        sequence = span.Slice(i + 2);

                        return CsvTokenType.RowEnd;
                    }

                    throw new Exception("Unexpected CR not followed by LF in column.");
                }

                if (x == '\n')
                {
                    token = span.Slice(0, i);
                    sequence = span.Slice(i + 1);

                    return CsvTokenType.RowEnd;
                }
            }

            if (isComplete)
            {
                token = span;
                sequence = default;
                return CsvTokenType.DocumentEnd;
            }

            if (span.Length == 0)
            {
                token = default;
                return CsvTokenType.NeedMore;
            }

            token = span;
            sequence = default;

            return CsvTokenType.ColumnPart;
        }
    }
}

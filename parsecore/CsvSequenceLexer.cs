using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace parsecore
{
    sealed class CsvSequenceLexer
    {
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public CsvTokenType TryParse(ref SequenceReader<byte> reader, bool isComplete, out ReadOnlySequence<byte> token)
        {
            if (reader.Remaining == 0)
            {
                token = default;
                return CsvTokenType.NeedMore;
            }

            SequencePosition start = reader.Position;
            SequencePosition end;
            CsvTokenType tokenType;
            int advanceLen;

            while (reader.TryPeek(out byte x))
            {
                if (x == ',')
                {
                    end = reader.Position;
                    tokenType = CsvTokenType.ColumnEnd;
                    advanceLen = 1;
                    goto returnTokenWithAdvance;
                }

                if (x == '\r')
                {
                    end = reader.Position;

                    reader.Advance(1);
                    if (!reader.TryRead(out x))
                    {
                        // ran out of data, not sure if \r\n or not. if we have data, return a partial token.

                        reader.Rewind(1);

                        if (end.Equals(start))
                        {
                            token = default;
                            return CsvTokenType.NeedMore;
                        }

                        // return a partial token.

                        tokenType = CsvTokenType.ColumnPart;
                        goto returnToken;
                    }

                    if (x == '\n')
                    {
                        // found \r\n.
                        tokenType = CsvTokenType.RowEnd;
                        goto returnToken;
                    }

                    throw new Exception("Unexpected CR not followed by LF in column.");
                }

                if (x == '\n')
                {
                    end = reader.Position;
                    tokenType = CsvTokenType.RowEnd;
                    advanceLen = 1;
                    goto returnTokenWithAdvance;
                }

                reader.Advance(1);
            }

            token = reader.Sequence.Slice(start);
            return isComplete ? CsvTokenType.DocumentEnd : CsvTokenType.ColumnPart;

        returnTokenWithAdvance:
            reader.Advance(advanceLen);

        returnToken:
            token = reader.Sequence.Slice(start, end);
            return tokenType;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public CsvTokenType TryParse(ref ReadOnlySequence<byte> sequence, bool isComplete, out ReadOnlySequence<byte> token)
        {
            SequencePosition nextPosition = sequence.Start;

            while(TryAdvance(sequence, ref nextPosition, out SequencePosition curPosition, out ReadOnlyMemory<byte> memory))
            {
                ReadOnlySpan<byte> span = memory.Span;

                for (int i = 0; i < span.Length; ++i)
                {
                    byte x = span[i];

                    if (x == ',')
                    {
                        token = sequence.Slice(sequence.Start, GetPositionFast(i, curPosition));
                        sequence = sequence.Slice(sequence.GetPosition(i + 1, curPosition));

                        return CsvTokenType.ColumnEnd;
                    }

                    if (x == '\r')
                    {
                        SequencePosition crlfPosition = GetPositionFast(i, curPosition);

                        if (!TryAdvance(sequence, ref nextPosition, ref curPosition, ref memory, ref span, ref i))
                        {
                            // ran out of data, not sure if \r\n or not.

                            if (crlfPosition.Equals(sequence.Start))
                            {
                                token = default;
                                return CsvTokenType.NeedMore;
                            }

                            // return a partial token.
                            token = sequence.Slice(sequence.Start, crlfPosition);
                            sequence = sequence.Slice(crlfPosition);

                            return CsvTokenType.ColumnPart;
                        }

                        if (span[i] == '\n')
                        {
                            // found \r\n.
                            token = sequence.Slice(sequence.Start, crlfPosition);
                            sequence = sequence.Slice(sequence.GetPosition(i + 1, curPosition));

                            return CsvTokenType.RowEnd;
                        }

                        throw new Exception("Unexpected CR not followed by LF in column.");
                    }

                    if (x == '\n')
                    {
                        token = sequence.Slice(sequence.Start, GetPositionFast(i, curPosition));
                        sequence = sequence.Slice(sequence.GetPosition(i + 1, curPosition));

                        return CsvTokenType.RowEnd;
                    }
                }
            }

            if (isComplete)
            {
                token = sequence;
                sequence = default;
                return CsvTokenType.DocumentEnd;
            }

            if (sequence.Length == 0)
            {
                token = default;
                return CsvTokenType.NeedMore;
            }

            token = sequence;
            sequence = default;

            return CsvTokenType.ColumnPart;
        }

        /// <summary>
        /// This is equivalent to <see cref="ReadOnlySequence{T}.GetPosition(long, SequencePosition)"/>, but faster. It is only valid when <paramref name="offset"/> will not exceed the length of the segment that <paramref name="origin"/> is referring to.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static SequencePosition GetPositionFast(int offset, in SequencePosition origin)
        {
            return new SequencePosition(origin.GetObject(), offset + (origin.GetInteger() & ~(1 << 31)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool TryAdvance(in ReadOnlySequence<byte> sequence, ref SequencePosition nextPosition, ref SequencePosition curPosition, ref ReadOnlyMemory<byte> memory, ref ReadOnlySpan<byte> span, ref int i)
        {
            if (++i != span.Length)
            {
                return true;
            }

            if (TryAdvance(sequence, ref nextPosition, out curPosition, out memory))
            {
                span = memory.Span;
                i = 0;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool TryAdvance(in ReadOnlySequence<byte> sequence, ref SequencePosition nextPosition, out SequencePosition curPosition, out ReadOnlyMemory<byte> memory)
        {
            curPosition = nextPosition;
            return sequence.TryGet(ref nextPosition, out memory);
        }
    }
}

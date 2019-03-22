using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Buffers;
using System.Text;

namespace parsecore
{
    [CoreJob, RPlotExporter, RankColumn]
    public class MyBench
    {
        ReadOnlyMemory<byte> memory;
        ReadOnlySequence<byte> sequence;

        [GlobalSetup]
        public void Setup()
        {
            memory = MappedMemoryManager.Open("C:\\dev\\star2002-full.csv").Memory;
            sequence = new ReadOnlySequence<byte>(memory);
        }

        [Benchmark]
        public void Span()
        {
            var parser = new CsvSpanLexer();
            var csv = memory.Span;

            while (parser.TryParse(ref csv, true, out ReadOnlySpan<byte> token) != CsvTokenType.DocumentEnd)
            {
            }
        }

        [Benchmark]
        public void Memory()
        {
            var parser = new CsvMemoryLexer();
            var csv = memory;

            while (parser.TryParse(ref csv, true, out ReadOnlyMemory<byte> token) != CsvTokenType.DocumentEnd)
            {
            }
        }

        [Benchmark]
        public void Sequence()
        {
            var parser = new CsvSequenceLexer();
            var sequence = this.sequence;

            while (parser.TryParse(ref sequence, true, out ReadOnlySequence<byte> token) != CsvTokenType.DocumentEnd)
            {
            }
        }

        [Benchmark]
        public void SequenceReader()
        {
            var parser = new CsvSequenceLexer();
            var reader = new SequenceReader<byte>(sequence);

            while (parser.TryParse(ref reader, true, out ReadOnlySequence<byte> token) != CsvTokenType.DocumentEnd)
            {
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //TestAllReaders(memory);
            BenchmarkRunner.Run<MyBench>();
        }

        static void TestAllReaders()
        {
            ReadOnlyMemory<byte> memory = MappedMemoryManager.Open("C:\\dev\\star2002-full.csv").Memory;
            ReadOnlySpan<byte> span = memory.Span;
            ReadOnlySequence<byte> sequence = new ReadOnlySequence<byte>(memory);
            ReadOnlySequence<byte> stupidSequence = memory.MakeStupid(4096);
            SequenceReader<byte> sequenceReader = new SequenceReader<byte>(sequence);
            SequenceReader<byte> stupidSequenceReader = new SequenceReader<byte>(stupidSequence);

            var spanLexer = new CsvSpanLexer();
            var memoryLexer = new CsvMemoryLexer();
            var sequenceLexer = new CsvSequenceLexer();

            long i = 0;

            while (true)
            {
                CsvTokenType spanTokenType = spanLexer.TryParse(ref span, true, out ReadOnlySpan<byte> spanToken);
                CsvTokenType memoryTokenType = memoryLexer.TryParse(ref memory, true, out ReadOnlyMemory<byte> memoryToken);
                CsvTokenType sequenceTokenType = sequenceLexer.TryParse(ref sequence, true, out ReadOnlySequence<byte> sequenceToken);
                CsvTokenType stupidSequenceTokenType = sequenceLexer.TryParse(ref stupidSequence, true, out ReadOnlySequence<byte> stupidSequenceToken);
                CsvTokenType sequenceReaderTokenType = sequenceLexer.TryParse(ref sequenceReader, true, out ReadOnlySequence<byte> sequenceReaderToken);
                CsvTokenType stupidSequenceReaderTokenType = sequenceLexer.TryParse(ref stupidSequenceReader, true, out ReadOnlySequence<byte> stupidSequenceReaderToken);

                if (spanTokenType != memoryTokenType) throw new Exception($"{nameof(spanTokenType)} != {nameof(memoryTokenType)}");
                if (spanTokenType != sequenceTokenType) throw new Exception($"{nameof(spanTokenType)} != {nameof(sequenceTokenType)}");
                if (spanTokenType != stupidSequenceTokenType) throw new Exception($"{nameof(spanTokenType)} != {nameof(stupidSequenceTokenType)}");
                if (spanTokenType != sequenceReaderTokenType) throw new Exception($"{nameof(spanTokenType)} != {nameof(sequenceReaderTokenType)}");
                if (spanTokenType != stupidSequenceReaderTokenType) throw new Exception($"{nameof(spanTokenType)} != {nameof(stupidSequenceReaderTokenType)}");

                if (!spanToken.SequenceEqual(memoryToken.Span)) throw new Exception($"{nameof(spanToken)} != {nameof(memoryToken)}");
                if (!spanToken.SequenceEqual(sequenceToken.ToArray())) throw new Exception($"{nameof(spanToken)} != {nameof(sequenceToken)}");
                if (!spanToken.SequenceEqual(stupidSequenceToken.ToArray())) throw new Exception($"{nameof(spanToken)} != {nameof(stupidSequenceToken)}");
                if (!spanToken.SequenceEqual(sequenceReaderToken.ToArray())) throw new Exception($"{nameof(spanToken)} != {nameof(sequenceReaderToken)}");
                if (!spanToken.SequenceEqual(stupidSequenceReaderToken.ToArray())) throw new Exception($"{nameof(spanToken)} != {nameof(stupidSequenceReaderToken)}");

                if ((++i % 1000000) == 0)
                {
                    string data = Encoding.UTF8.GetString(spanToken);
                    Console.WriteLine($"{spanTokenType}: {data} ({span.Length:N0} left)");
                }

                if (spanTokenType == CsvTokenType.NeedMore || spanTokenType == CsvTokenType.DocumentEnd)
                {
                    break;
                }
            }
        }

        static void TestSpan()
        {
            ReadOnlySpan<byte> csv = Encoding.UTF8.GetBytes("abc,def,ghi\r\njkl,mno,pqr\nstu,vwx,yz");

            var lexer = new CsvSpanLexer();

            CsvTokenType tokenType;
            while ((tokenType = lexer.TryParse(ref csv, true, out ReadOnlySpan<byte> token)) != CsvTokenType.NeedMore)
            {
                string data = string.Empty;

                if (!token.IsEmpty)
                {
                    data = Encoding.UTF8.GetString(token);
                }

                Console.WriteLine($"{tokenType}: {data}");

                if (tokenType == CsvTokenType.DocumentEnd)
                {
                    break;
                }
            }
        }
    }
}

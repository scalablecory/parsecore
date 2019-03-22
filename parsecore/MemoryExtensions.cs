using System;
using System.Buffers;

namespace parsecore
{
    static class MemoryExtensions
    {
        public static ReadOnlySequence<byte> MakeStupid(this ReadOnlyMemory<byte> memory, int segmentLength)
        {
            MySequenceSegment first = null, last = null;
            long runningIndex = 0;

            while (memory.Length != 0)
            {
                int partLen = Math.Min(segmentLength, memory.Length);

                MySequenceSegment part = new MySequenceSegment(memory.Slice(0, partLen), runningIndex);
                runningIndex += partLen;
                memory = memory.Slice(partLen);

                if (last == null)
                {
                    first = last = part;
                }
                else
                {
                    last.SetNext(part);
                    last = part;
                }
            }

            return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
        }

        sealed class MySequenceSegment : ReadOnlySequenceSegment<byte>
        {
            public MySequenceSegment(ReadOnlyMemory<byte> memory, long runningIndex)
            {
                base.Memory = memory;
                base.RunningIndex = runningIndex;
            }

            public void SetNext(MySequenceSegment next)
            {
                base.Next = next;
            }
        }
    }
}

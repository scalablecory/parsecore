using System;
using System.Buffers;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace parsecore
{
    sealed unsafe class MappedMemoryManager : MemoryManager<byte>
    {
        MemoryMappedFile mmf;
        MemoryMappedViewAccessor mmv;
        readonly byte* ptr;
        readonly int len;

        public static IMemoryOwner<byte> Open(string filePath)
        {
            return new MappedMemoryManager(filePath);
        }

        MappedMemoryManager(string filePath)
        {
            mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            mmv = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            mmv.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
            len = checked((int)mmv.SafeMemoryMappedViewHandle.ByteLength);
        }

        public override Span<byte> GetSpan()
        {
            return new Span<byte>(ptr, len);
        }

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            throw new NotImplementedException();
        }

        public override void Unpin()
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && mmf != null)
            {
                if (mmv != null)
                {
                    mmv.SafeMemoryMappedViewHandle.ReleasePointer();
                    mmv.Dispose();
                    mmv = null;
                }

                mmf.Dispose();
                mmf = null;
            }
        }
    }
}

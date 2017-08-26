namespace PacketApp {
    using System;

    public abstract class ByteBufferBase {
        protected byte[] buf;
        protected int capacity;
        protected int readIndex = 0;
        protected int writeIndex = 0;
        protected int markReadIndex = 0;
        protected int markWriteIndex = 0;
        protected ByteBufferBase (int capacity) {
            buf = new byte[capacity];
            this.capacity = capacity;
        }
        protected ByteBufferBase (byte[] bytes) {
            buf = bytes;
            this.capacity = bytes.Length;
        }

        public int ReadableBytes => writeIndex - readIndex;
        public int GetCapacity => this.capacity;

        public void setReaderIndex (int index) {
            if (index < 0) return;
            readIndex = index;
        }
        //
        public void markReaderIndex () => markReadIndex = readIndex;
        public void markWriterIndex () => markWriteIndex = writeIndex;
        public void resetReaderIndex () => readIndex = markReadIndex;
        public void resetWriterIndex () => writeIndex = markWriteIndex;

        //write ReadableBytes to this from ByteBuffer
        public abstract void write (ByteBuffer buffer);
        public abstract void writeByte (byte value);
        public void writeBytes (byte[] bytes) => writeBytes (bytes, bytes.Length);
        public void writeBytes (byte[] bytes, int length) => writeBytes (bytes, 0, length);
        public abstract void writeBytes (byte[] bytes, int startIndex, int length);

        public void writeShort (short value) => writeBytes (BitConverter.GetBytes (value).flip ());
        public void writeUshort (ushort value) => writeBytes (BitConverter.GetBytes (value).flip ());
        //byte[] array = new byte[4];
        //for (int i = 3; i >= 0; i--)
        //{
        //    array[i] = (byte)(value & 0xff);
        //    value = value >> 8;
        //}
        //Array.Reverse(array);
        //Write(array);
        public void writeInt (int value) => writeBytes ((BitConverter.GetBytes (value)).flip ());
        public void writeUint (uint value) => writeBytes (BitConverter.GetBytes (value).flip ());
        public void writeLong (long value) => writeBytes (BitConverter.GetBytes (value).flip ());
        public void writeUlong (ulong value) => writeBytes (BitConverter.GetBytes (value).flip ());
        public void writeFloat (float value) => writeBytes (BitConverter.GetBytes (value).flip ());
        public void writeDouble (double value) => writeBytes (BitConverter.GetBytes (value).flip ());

        protected abstract byte[] read (int size);
        public abstract byte readByte ();
        public abstract void readBytes (byte[] disbytes, int disstart, int len);
        public ushort readUshort () => read (2).toUInt16 ();
        public short readShort () => read (2).toInt16 ();
        public uint readUint () => read (4).toUInt32 ();
        public int readInt () => read (4).toInt32 ();
        public ulong readUlong () => read (8).toUInt64 ();
        public long readLong () => read (8).toInt64 ();
        public float readFloat () => read (4).toFloat ();
        public double readDouble () => read (8).toDouble ();

        public abstract void discardReadBytes ();

        public abstract void clear ();

        public abstract byte[] toArray ();
    }

    public class ByteBuffer : ByteBufferBase {
        private object locker = new object();
        private ByteBuffer (int capacity) : base (capacity) { }
        private ByteBuffer (byte[] bytes) : base (bytes) { }

        public static ByteBuffer allocate (int capacity) {
            return new ByteBuffer (capacity);
        }

        public static ByteBuffer allocate (byte[] bytes) {
            return new ByteBuffer (bytes);
        }

        //write ReadableBytes to this from ByteBuffer
        public override void write (ByteBuffer buffer) {
            if (buffer == null) return;
            if (buffer.ReadableBytes <= 0) return;
            writeBytes (buffer.toArray ());
        }

        public override void writeByte (byte value) {
            lock (locker) {
                int afterLen = writeIndex + 1;
                int len = buf.Length;
                fixSizeAndReset (len, afterLen);
                buf[writeIndex] = value;
                writeIndex = afterLen;
            }
        }
        public override void writeBytes (byte[] bytes, int startIndex, int length) {
            lock (locker) {
                int offset = length - startIndex;
                if (offset <= 0) return;
                int total = offset + writeIndex;
                int len = buf.Length;
                fixSizeAndReset (len, total);
                for (int i = writeIndex, j = startIndex; i < total; i++, j++) {
                    buf[i] = bytes[j];
                }
                writeIndex = total;
            }
        }

        //read from readIndex to size
        protected override byte[] read (int size) {
            byte[] bytes = new byte[size];
            Array.Copy (buf, readIndex, bytes, 0, size);
            if (BitConverter.IsLittleEndian) {
                Array.Reverse (bytes);
            }
            readIndex += size;
            return bytes;
        }
        public override byte readByte () {
            return buf[readIndex++];
        }
        public override void readBytes (byte[] disbytes, int disstart, int len) {
            int size = disstart + len;
            for (int i = disstart; i < size; i++) {
                disbytes[i] = this.readByte ();
            }
        }

        public override void discardReadBytes () {
            if (readIndex <= 0) return;
            int len = buf.Length - readIndex;
            byte[] bufTemp = new byte[len];
            Array.Copy (buf, readIndex, bufTemp, 0, len);
            buf = bufTemp;
            writeIndex -= readIndex;
            markReadIndex -= readIndex;
            if (markReadIndex < 0) {
                markReadIndex = readIndex;
            }
            markWriteIndex -= readIndex;
            if (markWriteIndex < 0 ||
                markWriteIndex < readIndex ||
                markWriteIndex < markReadIndex) {
                markWriteIndex = writeIndex;
            }
            readIndex = 0;
        }

        public override void clear () {
            buf = new byte[buf.Length];
            readIndex = 0;
            writeIndex = 0;
            markReadIndex = 0;
            markWriteIndex = 0;
        }

        public override byte[] toArray () {
            byte[] bytes = new byte[writeIndex];
            Array.Copy (buf, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        //Fix cache capacity
        private int fixSizeAndReset (int currLen, int futureLen) {
            if (futureLen > currLen) {
                //Find a min number which is the power of two and large than the origin size
                //Ensure the size base on the twice of this number
                int size = minNumIsPowerOfTwoAndNear (currLen) * 2;
                if (futureLen > size) {
                    //Ensure inner byte cache size base on the twice of future length
                    size = minNumIsPowerOfTwoAndNear (futureLen) * 2;
                }
                byte[] bufTemp = new byte[size];
                Array.Copy (buf, 0, bufTemp, 0, currLen);
                buf = bufTemp;
                capacity = bufTemp.Length;
            }
            return futureLen;
        }

        private int minNumIsPowerOfTwoAndNear (int num) {
            int n = 2, b = 2;
            while (b < num) {
                b = 2 << n;
                n++;
            }
            return b;
        }

    }
    public static class BitConverterHelper {
        public static byte[] flip (this byte[] bytes) {
            if (BitConverter.IsLittleEndian) {
                Array.Reverse (bytes);
            }
            return bytes;
        }
        public static short toInt16 (this byte[] data, int index = 0) {
            return BitConverter.ToInt16 (data, index);
        }
        public static ushort toUInt16 (this byte[] data, int index = 0) {
            return BitConverter.ToUInt16 (data, index);
        }
        public static int toInt32 (this byte[] data, int index = 0) {
            return BitConverter.ToInt32 (data, index);
        }
        public static uint toUInt32 (this byte[] data, int index = 0) {
            return BitConverter.ToUInt32 (data, index);
        }
        public static long toInt64 (this byte[] data, int index = 0) {
            return BitConverter.ToInt64 (data, index);
        }
        public static ulong toUInt64 (this byte[] data, int index = 0) {
            return BitConverter.ToUInt64 (data, index);
        }
        public static float toFloat (this byte[] data, int index = 0) {
            return BitConverter.ToSingle (data, index);
        }
        public static double toDouble (this byte[] data, int index = 0) {
            return BitConverter.ToDouble (data, index);
        }
    }
}
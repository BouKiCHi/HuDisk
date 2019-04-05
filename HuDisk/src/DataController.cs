using System;

namespace Disk {
    public class DataController {
        private byte[] Buffer;

        public DataController(byte[] buffer) {
            SetBuffer(buffer);
        }

        public void SetBuffer(byte[] buffer) {
            this.Buffer = buffer;
        }

        public byte[] Copy(int pos, int len) {
            byte[] result = new byte[len];
            Array.Copy(Buffer, pos, result, 0, len);
            return result;
        }

        public byte[] GetData() {
            return Buffer;
        }
        public ulong GetLong(int pos) {
            ulong result = Buffer[pos];
            result |= ((ulong)Buffer[pos + 1] << 8);
            result |= ((ulong)Buffer[pos + 2] << 16);
            result |= ((ulong)Buffer[pos + 3] << 24);
            return result;
        }

        public ushort GetWord(int pos) {
            ulong result = Buffer[pos];
            result |= ((ulong)Buffer[pos + 1] << 8);
            return (ushort)result;
        }

        public byte GetByte(int pos) {
            return Buffer[pos];
        }

        public void SetLong(int pos, ulong value) {
            Buffer[pos] = (byte)(value & 0xff);
            Buffer[pos + 1] = (byte)((value >> 8) & 0xff);
            Buffer[pos + 2] = (byte)((value >> 16) & 0xff);
            Buffer[pos + 3] = (byte)((value >> 24) & 0xff);
        }

        public void SetWord(int pos, int value) {
            Buffer[pos] = (byte)(value & 0xff);
            Buffer[pos + 1] = (byte)((value >> 8) & 0xff);
        }

        public void SetByte(int pos, int value) {
            Buffer[pos] = (byte)(value & 0xff);
        }

        public void SetCopy(int pos, byte[] data, int length = -1) {
            if (length < 0 || data.Length < length) length = data.Length;
            for (var i = 0; i < length; i++) Buffer[pos + i] = data[i];
        }
        public void Fill(int value, int pos, int length) {
            for (var i = 0; i < length; i++) Buffer[pos + i] = (byte)value;
        }
        public void Fill(int value) {
            for (var i = 0; i < Buffer.Length; i++) Buffer[i] = (byte)value;
        }

    }

}

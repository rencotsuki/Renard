using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Renard.AssetBundleUniTask
{
    public class SeekableAesStream : Stream
    {
        private Stream _baseStream;
        private AesManaged _aes;
        private ICryptoTransform _cryptor;
        public bool AutoDisposeBaseStream { get; set; } = true;

        public SeekableAesStream(Stream baseStream, string password, byte[] salt)
        {
            _baseStream = baseStream;
            Synchronized(_baseStream);

            using (var key = new PasswordDeriveBytes(password, salt))
            {
                _aes = new AesManaged();
                _aes.KeySize   = 256;
                _aes.BlockSize = 128;
                _aes.Mode      = CipherMode.CBC;
                _aes.Padding   = PaddingMode.PKCS7;
                _aes.Key       = GetKeyFromPassword(password, salt, _aes.KeySize);
                _aes.IV        = GetIVFromPassword(password, _aes.BlockSize);
            }
        }

        private byte[] GetKeyFromPassword(string password, byte[] salt, int keySize)
        {
            var deriveBytes = new Rfc2898DeriveBytes(password, salt);
            deriveBytes.IterationCount = 1000;
            return deriveBytes.GetBytes(keySize / 8);
        }

        private byte[] GetIVFromPassword(string password, int blockSize)
        {
            var deriveBytes = new Rfc2898DeriveBytes(password + DateTime.Now.Ticks.ToString(), 100);
            deriveBytes.IterationCount = 1000;
            return deriveBytes.GetBytes(blockSize / 8);
        }

        private void Cipher(byte[] buffer, int offset, int count, long streamPos, bool isEncryptor)
        {
            var blockSizeInByte = _aes.BlockSize / 8;
            var blockNumber = (streamPos / blockSizeInByte) + 1;
            var keyPos = streamPos % blockSizeInByte;

            var outBuffer = new byte[blockSizeInByte];
            var nonce = new byte[blockSizeInByte];
            var init = false;

            if (isEncryptor)
            {
                _cryptor = _aes.CreateEncryptor(_aes.Key, _aes.IV);
            }
            else
            {
                _cryptor = _aes.CreateDecryptor(_aes.Key, _aes.IV);
            }

            for (int i = offset; i < count; i++)
            {
                if (!init || (keyPos % blockSizeInByte) == 0)
                {
                    BitConverter.GetBytes(blockNumber).CopyTo(nonce, 0);
                    _cryptor.TransformBlock(nonce, 0, nonce.Length, outBuffer, 0);

                    if (init) keyPos = 0;

                    init = true;
                    blockNumber++;
                }

                buffer[i] ^= outBuffer[keyPos];
                keyPos++;
            }
        }
        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => _baseStream.CanWrite;
        public override long Length => _baseStream.Length;
        public override long Position
        {
            get => _baseStream.Position;
            set => _baseStream.Position = value;
        }
        public override void Flush() => _baseStream.Flush();
        public override void SetLength(long value) => _baseStream.SetLength(value);
        public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);
        public override void Close() => _baseStream.Close();

        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                var streamPos = Position;
                var ret = _baseStream.Read(buffer, offset, count);
                Cipher(buffer, offset, count, streamPos, false);
                return ret;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log($"SeekableAesStream::Read {_baseStream.ToString()} - {ex.Message}");
            }

            return offset;
        }

        public async override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                var streamPos = Position;
                var ret = await _baseStream.ReadAsync(buffer, offset, count, cancellationToken);
                Cipher(buffer, offset, count, streamPos, false);
                return ret;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log($"SeekableAesStream::ReadAsync {_baseStream.ToString()} - {ex.Message}");
            }

            return offset;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            try
            {
                Cipher(buffer, offset, count, Position, true);
                _baseStream.Write(buffer, offset, count);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log($"SeekableAesStream::Write {_baseStream.ToString()} - {ex.Message}");
            }
        }

        public async override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                Cipher(buffer, offset, count, Position, true);
                await _baseStream.WriteAsync(buffer, offset, count, cancellationToken);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log($"SeekableAesStream::WriteAsync {_baseStream.ToString()} - {ex.Message}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cryptor?.Dispose();
                _aes?.Dispose();
                if (AutoDisposeBaseStream)
                    _baseStream?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

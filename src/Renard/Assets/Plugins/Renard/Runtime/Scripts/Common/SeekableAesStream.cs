using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Renard
{
    using Debuger;

    public class SeekableAesStream : Stream
    {
        private Stream _baseStream = null;
        private Aes _aesAlg = null;
        private ICryptoTransform _cryptor = default;
        public bool AutoDisposeBaseStream { get; set; } = true;

        private bool _isDebugLog = false;

        protected void Log(DebugerLogType logType, string methodName, string message)
        {
            if (!_isDebugLog)
            {
                if (logType == DebugerLogType.Info)
                    return;
            }

            DebugLogger.Log(typeof(SeekableAesStream), logType, methodName, message);
        }

        public SeekableAesStream(Stream baseStream, string key, string iv, bool isDebugLog = false)
        {
            _isDebugLog = isDebugLog;

            try
            {
                _baseStream = baseStream;
                Synchronized(_baseStream);

                _aesAlg = Aes.Create();
                _aesAlg.Key = Encoding.UTF8.GetBytes(key);
                _aesAlg.IV = Encoding.UTF8.GetBytes(iv);
                _aesAlg.Mode = CipherMode.CBC;
                _aesAlg.Padding = PaddingMode.PKCS7;

                _cryptor = _aesAlg.CreateEncryptor(_aesAlg.Key, _aesAlg.IV);
            }
            catch (Exception ex)
            {
                Log(DebugerLogType.Warning, "SeekableAesStream", $"{ex.Message}");
            }
        }

        private void Cipher(byte[] buffer, int offset, int count, long streamPos, bool isEncryptor)
        {
            var blockSizeInByte = _aesAlg.BlockSize / 8;
            var blockNumber = (streamPos / blockSizeInByte) + 1;
            var keyPos = streamPos % blockSizeInByte;

            var outBuffer = new byte[blockSizeInByte];
            var nonce = new byte[blockSizeInByte];
            var init = false;

            if (isEncryptor)
            {
                _cryptor = _aesAlg.CreateEncryptor(_aesAlg.Key, _aesAlg.IV);
            }
            else
            {
                _cryptor = _aesAlg.CreateDecryptor(_aesAlg.Key, _aesAlg.IV);
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
                Log(DebugerLogType.Warning, "Read", $"{ex.Message}");
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
                Log(DebugerLogType.Warning, "ReadAsync", $"{_baseStream.ToString()} - {ex.Message}");
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
                Log(DebugerLogType.Warning, "Write", $"{_baseStream.ToString()} - {ex.Message}");
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
                Log(DebugerLogType.Warning, "WriteAsync", $"{_baseStream.ToString()} - {ex.Message}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cryptor?.Dispose();
                _aesAlg?.Dispose();
                if (AutoDisposeBaseStream)
                    _baseStream?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

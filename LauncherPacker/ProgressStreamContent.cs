using LauncherPacker;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
namespace LauncherPacker;
public class ProgressStreamContent : HttpContent
{
    private readonly Stream _stream;
    private readonly long _totalBytes;
    private readonly Action<ProgressArgs> _onProgress;
    private readonly int _bufferSize;

    public ProgressStreamContent(Stream stream, Action<ProgressArgs> onProgress, int bufferSize = 4096)
    {
        _stream = stream;
        _totalBytes = stream.Length;
        _onProgress = onProgress;
        _bufferSize = bufferSize;
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        var buffer = new byte[_bufferSize];
        long uploaded = 0;

        // 初始化进度 (0%)
        _onProgress?.Invoke(new ProgressArgs { Transferred = 0, Total = _totalBytes });

        using (_stream)
        {
            while (true)
            {
                int length = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (length <= 0) break;

                uploaded += length;
                await stream.WriteAsync(buffer, 0, length);

                // 核心：每读取一次缓冲区，就通知一次 UI
                _onProgress?.Invoke(new ProgressArgs { Transferred = uploaded, Total = _totalBytes });
            }
        }
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _totalBytes;
        return true;
    }
}
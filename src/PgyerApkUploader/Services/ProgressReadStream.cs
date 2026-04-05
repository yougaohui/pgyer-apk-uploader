namespace PgyerApkUploader.Services;

/// <summary>
/// Wraps a read-only stream and reports cumulative bytes read after each read.
/// Used so multipart upload progress reflects bytes sent from the APK file.
/// </summary>
internal sealed class ProgressReadStream : Stream
{
    private readonly Stream _inner;
    private readonly IProgress<(long BytesRead, long TotalBytes)>? _progress;
    private readonly long _totalBytes;
    private long _bytesRead;

    public ProgressReadStream(Stream inner, long totalBytes, IProgress<(long BytesRead, long TotalBytes)>? progress)
    {
        _inner = inner;
        _totalBytes = totalBytes;
        _progress = progress;
    }

    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => _inner.Length;
    public override long Position
    {
        get => _inner.Position;
        set => throw new NotSupportedException();
    }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var n = _inner.Read(buffer, offset, count);
        Report(n);
        return n;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var n = await _inner.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
        Report(n);
        return n;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var n = await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        Report(n);
        return n;
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    private void Report(int n)
    {
        if (n <= 0)
            return;
        _bytesRead += n;
        _progress?.Report((_bytesRead, _totalBytes));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _inner.Dispose();
        base.Dispose(disposing);
    }
}

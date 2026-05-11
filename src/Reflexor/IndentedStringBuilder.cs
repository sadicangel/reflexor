using System.Text;

namespace Reflexor;

public struct IndentedStringBuilder()
{
    private StringBuilder? _builder = new StringBuilder();
    private bool _isLineStart = true;

    public int Indent { get; set; } = 0;

    public void Write(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        WriteIndentIfNeeded();
        Builder.Append(value);
    }

    public void WriteLine()
    {
        Builder.AppendLine();
        _isLineStart = true;
    }

    public void WriteLine(string? value)
    {
        Write(value);
        WriteLine();
    }

    public readonly override string ToString() => _builder?.ToString() ?? string.Empty;

    private StringBuilder Builder => _builder ??= new StringBuilder();

    private void WriteIndentIfNeeded()
    {
        if (!_isLineStart)
        {
            return;
        }

        Builder.Append(' ', Indent * 4);
        _isLineStart = false;
    }
}

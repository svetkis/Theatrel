using System;
using System.Linq;
using System.Text;
using theatrel.Interfaces.EncodingService;

namespace theatrel.Lib.EncodingServices;

internal class EncodingService : IEncodingService
{
    private const string Win1251 = "windows-1251";
    private const string Utf8 = "utf-8";
    private const string ContentCharset = "content=\"text/html; charset=";

    private readonly byte[] _contentCharsetBytes = Encoding.UTF8.GetBytes(ContentCharset);
    private readonly byte[] _win1251Bytes = Encoding.UTF8.GetBytes(Win1251 + "\"");
    private readonly byte[] _utf8Bytes = Encoding.UTF8.GetBytes(Utf8 + "\"");

    private readonly Encoding _encoding1251;

    public EncodingService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _encoding1251 = Encoding.GetEncoding(Win1251);
    }

    public byte[] ProcessBytes(byte[] bytesData)
    {
        Span<byte> span = bytesData.AsSpan();
        int contentCharsetIndex = span.IndexOf(_contentCharsetBytes);
        if (-1 == contentCharsetIndex)
            return bytesData;

        int charsetPosition = contentCharsetIndex + _contentCharsetBytes.Length;
        if (!CheckPatternInPosition(bytesData, _win1251Bytes, charsetPosition))
            return bytesData;

        ReplacePatternInPosition(bytesData, _win1251Bytes, _utf8Bytes, charsetPosition);

        return Encoding.Convert(_encoding1251, Encoding.UTF8, bytesData);
    }

    private bool CheckPatternInPosition(byte[] src, byte[] pattern, int position) => !pattern.Where((t, i) => src[position + i] != t).Any();

    private void ReplacePatternInPosition(byte[] src, byte[] pattern, byte[] newPattern, int position)
    {
        int newPatternLength = newPattern.Length;
        for (var i = 0; i < pattern.Length; ++i)
        {
            if (i < newPatternLength)
            {
                src[position + i] = newPattern[i];
            }
            else
            {
                src[position + i] = (byte)' ';
            }
        }
    }
}
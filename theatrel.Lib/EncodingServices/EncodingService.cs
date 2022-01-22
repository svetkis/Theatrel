using System.Text;
using theatrel.Interfaces.EncodingService;

namespace theatrel.Lib.EncodingServices;

internal class EncodingService : IEncodingService
{
    private const string Win1251 = "windows-1251";
    private const string Utf8 = "utf-8";
    private const string Content1251 = $"content=\"text/html; charset={Win1251}\"";

    public string Process(string data, byte[] bytesData)
    {
        if (data == null || !data.Contains(Content1251))
            return data;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding encoding1251 = Encoding.GetEncoding(Win1251);
        byte[] converted = Encoding.Convert(encoding1251, Encoding.UTF8, bytesData);

        return Encoding.UTF8.GetString(converted).Replace(Win1251, Utf8);
    }
}
using System.Text;
using theatrel.Interfaces.EncodingService;

namespace theatrel.Lib.EncodingServices;

internal class Rus1251ToUtf8Service : IEncodingService
{
    private const string Win1251 = "windows-1251";
    private const string Utf8 = "utf-8";

    public string Process(string data)
    {
        
        if (data == null || !data.Contains(Win1251))
            return data;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding encoding1251 = Encoding.GetEncoding(Win1251);
        byte[] converted = Encoding.Convert(encoding1251, Encoding.UTF8, encoding1251.GetBytes(data));
        return Encoding.UTF8.GetString(converted).Replace(Win1251, Utf8);
    }
}
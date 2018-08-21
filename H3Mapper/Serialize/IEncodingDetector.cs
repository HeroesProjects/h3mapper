using System.Text;

namespace H3Mapper.Serialize
{
    public interface IEncodingDetector
    {
        Encoding TryMatchEncoding(byte[] text);
    }
}
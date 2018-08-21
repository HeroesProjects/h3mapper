using System.Linq;
using System.Text;

namespace H3Mapper.Serialize
{
    public class PolishEncodingDetector : IEncodingDetector
    {
        private static readonly byte[] Letters = {
            185, // ą
            165, // Ą
            230, // ć
            198, // Ć
            234, // ę
            202, // Ę
            179, // ł
            163, // Ł
            241, // ń
            209, // Ń
            243, // ó
            211, // Ó
            156, // ś
            140, // Ś
            159, // ź
            143, // Ź
            191, // ż
            175, // Ż
        };

        public Encoding TryMatchEncoding(byte[] text)
        {
            return text.Where(x => x > 127).All(x => Letters.Contains(x)) ? Encoding.GetEncoding(1250) : null;
        }
    }
}
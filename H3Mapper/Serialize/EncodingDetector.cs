using System.Text;

namespace H3Mapper.Serialize
{
    public class EncodingDetector
    {
        private readonly IEncodingDetector[] encodings =
        {
            new PolishEncodingDetector(),
            new HungarianEncodingDetector(),
            new CyrilicEncodingDetector()
        };

        public Encoding GuessEncoding(byte[] text)
        {
            foreach (var language in encodings)
            {
                var encoding = language.TryMatchEncoding(text);
                if (encoding != null)
                    return encoding;
            }

            return Encoding.GetEncoding(1252);
        }
    }
}
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hackathon
{
    public class QuizModel 
    {
        public int ID { get; set; }

        [JsonPropertyName("soru_metni")]
        public string SoruMetni { get; set; }

        [JsonPropertyName("secenekler")]
        public Dictionary<string, string> Secenekler { get; set; }

        [JsonPropertyName("dogru_cevap")]
        public string DogruCevap { get; set; }

        [JsonIgnore]
        public string KullaniciCevabi { get; set; }

        [JsonIgnore]
        public bool IsCorrect { get; set; }
    }
}
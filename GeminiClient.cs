// GeminiClient.cs

using Hackathon; // Question sınıfı için
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

public enum GeminiModel
{
    GeminiPro,
    GeminiFlash
}

// Bu sınımlamalar aynı kalıyor.
public class GeminiResponse { [JsonPropertyName("candidates")] public List<Candidate> Candidates { get; set; } }
public class Candidate { [JsonPropertyName("content")] public Content Content { get; set; } }
public class Content { [JsonPropertyName("parts")] public List<Part> Parts { get; set; } }
public class Part { [JsonPropertyName("text")] public string Text { get; set; } }

public class GeminiClient
{
    private readonly string apiKey = ConfigurationManager.AppSettings["GeminiApiKey"];
    private readonly HttpClient _httpClient;

    public GeminiClient()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(5); 
    }

    /// <summary>
    /// Verilen tam ve detaylı prompt'u kullanarak Gemini API'sinden sorular üretir.
    /// </summary>
    /// <param name="fullUserPrompt">SoruUretim penceresinde tüm detaylarla oluşturulmuş olan istek metni.</param>
    public async Task<List<QuizModel>> GenerateQuestionsAsync(string fullUserPrompt, GeminiModel modelToUse)
    {
        var formattingInstructions = @"

--- ÇOK ÖNEMLİ FORMAT KURALLARI ---
1.  **Format Kesinliği:** Cevabı SADECE bir JSON dizisi (array) olarak döndür. Başka hiçbir metin, açıklama veya ```json``` gibi işaretçiler ekleme. Sadece saf JSON döndür.

Örnek JSON Formatı:
[
  {
    ""soru_metni"": ""Bu çoktan seçmeli bir sorudur..."",
    ""secenekler"": { ""A"": ""A şıkkı"", ""B"": ""B şıkkı"", ""C"": ""C şıkkı"", ""D"": ""D şıkkı"", ""E"": ""E şıkkı"" }},
    ""dogru_cevap"": ""A""
  },
  {
    ""soru_metni"": ""Bu bir açık uçlu sorudur..."",
    ""secenekler"": null,
    ""dogru_cevap"": ""Beklenen cevap anahtarı...""
  }
]
";
        var finalPrompt = fullUserPrompt + formattingInstructions;
        string responseText = await _ExecuteApiRequestAsync(finalPrompt, modelToUse, stream: false);

        if (responseText == null) return null;

        try
        {
            var cleanJson = responseText.Replace("```json", "").Replace("```", "").Trim();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                // Bu kural, sondaki fazladan virgüllere izin verir.
                AllowTrailingCommas = true
            };

            return JsonSerializer.Deserialize<List<QuizModel>>(cleanJson, options);
        }
        catch (JsonException jsonEx)
        {
            MessageBox.Show($"JSON dönüştürme hatası: {jsonEx.Message}\n\nAPI'den gelen ham cevap:\n{responseText}", "İşleme Hatası");
            return null;
        }
    }



    public async Task<string> GenerateHintAsync(QuizModel question)
    {
        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("Aşağıdaki çoktan seçmeli soru için, doğru cevabı doğrudan SÖYLEMEDEN, kullanıcıyı doğru düşünmeye yönlendirecek kısa ve tek cümlelik bir ipucu ver.");
        promptBuilder.AppendLine("Cevabı SADECE ve SADECE ipucu metni olarak döndür. Başka hiçbir açıklama ekleme.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"Soru: {question.SoruMetni}");

        if (question.Secenekler != null && question.Secenekler.Any())
        {
            promptBuilder.AppendLine("Seçenekler:");
            foreach (var option in question.Secenekler) { promptBuilder.AppendLine($"- {option.Key}) {option.Value}"); }
        }

        string hintText = await _ExecuteApiRequestAsync(promptBuilder.ToString(), GeminiModel.GeminiFlash, stream: false);
        return string.IsNullOrWhiteSpace(hintText) ? "İpucu üretilemedi, lütfen tekrar deneyin." : hintText.Trim('*', '`', '\n', ' ', '\r');
    }

    // GeminiClient.cs içinde bu metodu bulun ve tamamen değiştirin

    private async Task<string> _ExecuteApiRequestAsync(string prompt, GeminiModel modelToUse, bool stream, Action<string> onChunkReceived = null)
    {
        // ▼▼▼ YENİ: Otomatik Tekrar Deneme Mantığı ▼▼▼
        int maxRetries = 3; // En fazla 3 kez tekrar denesin
        int delay = 2000;   // Her deneme arasında 2 saniye beklesin (2000 ms)

        for (int i = 0; i < maxRetries; i++)
        {
            string modelName = modelToUse == GeminiModel.GeminiFlash ? "gemini-1.5-flash" : "gemini-1.5-pro";
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:{(stream ? "streamGenerateContent?alt=sse" : "generateContent")}?key={apiKey}";
            var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
            var jsonRequest = JsonSerializer.Serialize(requestBody);
            var contentJson = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            try
            {
                if (!stream)
                {
                    using (var response = await _httpClient.PostAsync(url, contentJson))
                    {
                        // Eğer 503 ServiceUnavailable hatası alırsak, döngünün bir sonraki adımına geçmeden önce bekle.
                        if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                        {
                            Debug.WriteLine($"API 503 (Overloaded) hatası verdi. {delay / 1000} saniye sonra tekrar denenecek... (Deneme {i + 1}/{maxRetries})");
                            await Task.Delay(delay);
                            continue; // Döngünün bir sonraki adımına geç
                        }

                        var responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!response.IsSuccessStatusCode) { MessageBox.Show($"API'den hata alındı (HTTP {response.StatusCode}): {responseText}", "API Hatası"); return null; }
                        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseText);
                        return geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                    }
                }
                else // Stream için de aynı mantığı uygulayalım
                {
                    using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url) { Content = contentJson })
                    using (var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead))
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                        {
                            Debug.WriteLine($"API 503 (Overloaded) hatası verdi. {delay / 1000} saniye sonra tekrar denenecek... (Deneme {i + 1}/{maxRetries})");
                            await Task.Delay(delay);
                            continue;
                        }

                        response.EnsureSuccessStatusCode();
                        // ... (Stream okuma kodunun geri kalanı aynı)
                        using (var responseStream = await response.Content.ReadAsStreamAsync())
                        using (var reader = new StreamReader(responseStream))
                        {
                            // ...
                        }
                        return "Stream Bitti";
                    }
                }
            }
            catch (TaskCanceledException) { MessageBox.Show("API isteği zaman aşımına uğradı.", "Zaman Aşımı"); return null; }
            catch (Exception ex)
            {
                // Eğer son denemede de hata alırsak, o zaman kullanıcıya göster.
                if (i == maxRetries - 1)
                {
                    MessageBox.Show($"Beklenmedik bir ağ hatası oluştu: {ex.Message}", "Genel Hata");
                }
                else
                {
                    // Hata sonrası da bir süre bekleyip tekrar deneyelim.
                    await Task.Delay(delay);
                }
            }
        }

        // Eğer tüm denemeler başarısız olursa, kullanıcıya bilgi ver.
        MessageBox.Show("API sunucusu şu anda çok yoğun ve birden çok denemeye rağmen cevap vermedi. Lütfen birkaç dakika sonra tekrar deneyin.", "API Sunucusu Meşgul", MessageBoxButton.OK, MessageBoxImage.Warning);
        return null;
    }

    /// <summary>
    /// Verilen analiz prompt'unu kullanarak Gemini'den gelen cevabı parça parça (stream) işler.
    /// </summary>
    /// <param name="analysisPrompt">Analiz için oluşturulan prompt.</param>
    /// <param name="onChunkReceived">Gelen her bir metin parçası için tetiklenecek olan eylem (callback).</param>
    public async Task StreamAnalysisAsync(string analysisPrompt, Action<string> onChunkReceived)
    {
        // Analiz gibi karmaşık bir iş için her zaman en güçlü olan Pro modelini seçiyoruz.
        string modelName = "gemini-1.5-pro";

        // Stream isteği için URL'ye özel parametre eklenir: "?alt=sse"
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:streamGenerateContent?alt=sse&key={apiKey}";

        var requestBody = new { contents = new[] { new { parts = new[] { new { text = analysisPrompt } } } } };
        var jsonRequest = JsonSerializer.Serialize(requestBody);
        var contentJson = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        try
        {
            // Stream (akış) olarak cevap alabilmek için özel bir HttpClient isteği gönderiyoruz.
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url) { Content = contentJson })
            using (var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode(); // Bir hata varsa (4xx, 5xx), burada exception fırlatır.

                // Cevap akışını okumak için bir StreamReader oluşturuyoruz.
                using (var responseStream = await response.Content.ReadAsStreamAsync())
                using (var reader = new StreamReader(responseStream))
                {
                    // Akışın sonuna gelene kadar satır satır oku.
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        // Gemini'nin stream formatı: "text": "gelen metin parçası"
                        // Bu satırı Regex ile işleyerek sadece metin parçasını alıyoruz.
                        Match match = Regex.Match(line, @"""text""\s*:\s*""(.*?)""");
                        if (match.Success)
                        {
                            // Gelen metin \n gibi kaçış karakterleri içerebilir, bunları düzeltelim.
                            string chunk = Regex.Unescape(match.Groups[1].Value);

                            // Gelen parçayı, bizi çağıran metoda (Analiz.xaml.cs) geri gönder.
                            onChunkReceived?.Invoke(chunk);
                        }
                    }
                }
            }
        }
        catch (TaskCanceledException) { MessageBox.Show("API isteği zaman aşımına uğradı.", "Zaman Aşımı"); }
        catch (Exception ex) { MessageBox.Show($"Beklenmedik bir ağ hatası oluştu: {ex.Message}", "Genel Hata"); }


    }

    public async Task<string> AnalyzeWrongAnswerAsync(TestDetay wrongAnswerDetail)
    {
        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("Sen, sabırlı ve teşvik edici bir öğretmensin.");
        promptBuilder.AppendLine("Aşağıdaki soruyu, öğrencinin verdiği yanlış cevabı ve doğru cevabı analiz et.");
        promptBuilder.AppendLine("Cevabını, doğrudan öğrenciye hitap ederek ve aşağıdaki 2 başlığı kullanarak, Markdown formatında, kısa ve net bir şekilde yapılandır:");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("### Neden Bu Cevap Yanlış?");
        promptBuilder.AppendLine("- Öğrencinin düşünce hatasının ne olabileceğini açıkla (Örn: 'Bu seçeneği seçmenin nedeni muhtemelen...').");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("### Doğru Cevap Neden Bu?");
        promptBuilder.AppendLine("- Doğru cevaba götüren mantığı veya bilgiyi adım adım anlat.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("--- ANALİZ EDİLECEK VERİLER ---");
        promptBuilder.AppendLine($"**Soru:** {wrongAnswerDetail.SoruMetni}");
        promptBuilder.AppendLine($"**Öğrencinin Yanlış Cevabı:** {wrongAnswerDetail.VerilenCevap}");
        promptBuilder.AppendLine($"**Doğru Cevap:** {wrongAnswerDetail.DogruCevap}");

        // Bu tür kısa analizler için en hızlı model olan GeminiFlash'ı kullanıyoruz.
        string analysisText = await _ExecuteApiRequestAsync(promptBuilder.ToString(), GeminiModel.GeminiFlash, stream: false);

        return string.IsNullOrWhiteSpace(analysisText) ? "Analiz üretilemedi, lütfen tekrar deneyin." : analysisText.Trim();
    }
}
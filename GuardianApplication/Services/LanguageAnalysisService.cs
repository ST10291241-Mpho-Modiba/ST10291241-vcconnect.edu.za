using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.AI.TextAnalytics;

namespace GuardianApplication.Services
{
    public class LanguageAnalysisService
    {
        private static readonly AzureKeyCredential credential = new AzureKeyCredential("1ShG46dmxtzkRHalIkPHiQ0Itud2ziYrxcz97SLmarrfWbhFOjIhJQQJ99BHACrIdLPXJ3w3AAAaACOG1CFZ");
        private static readonly Uri endpoint = new Uri("https://languageanalysises.cognitiveservices.azure.com/");

        private static readonly TextAnalyticsClient client = new TextAnalyticsClient(endpoint, credential);

        public static async Task<string> EstimateSeverityAsync(string message)
        {
            try
            {
                var sentiment = await client.AnalyzeSentimentAsync(message);
                var keyPhrases = await client.ExtractKeyPhrasesAsync(message);

                double score = sentiment.Value.ConfidenceScores.Negative;
                var keywords = string.Join(", ", keyPhrases.Value);

                if (score > 0.8 || ContainsCriticalPhrase(keyPhrases.Value))
                    return "CRITICAL";
                else if (score > 0.5)
                    return "HIGH";
                else if (score > 0.3)
                    return "MODERATE";
                else
                    return "LOW";
            }
            catch
            {
                return "UNKNOWN";
            }
        }

        private static bool ContainsCriticalPhrase(IReadOnlyCollection<string> phrases)
        {
            var criticalWords = new[] { "gun", "fire", "shot", "dead", "unconscious", "bleeding" };
            foreach (var phrase in phrases)
            {
                foreach (var word in criticalWords)
                {
                    if (phrase.ToLower().Contains(word))
                        return true;
                }
            }
            return false;
        }
    }
}

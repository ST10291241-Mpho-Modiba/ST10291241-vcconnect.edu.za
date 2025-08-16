using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace GuardianApplication.Services
{
   public class VoiceRecognitionService
    {
        private static readonly string speechKey = "2MAyAiL0udvA7g8h4VU1ivfKXuaxfT8J26wkww61BtyoAEkywnViJQQJ99BGACrIdLPXJ3w3AAAYACOGBaii";
        private static readonly string speechRegion = "southafricanorth";

        public static async Task<string> GetTranscribedTextAsync()
        {
            try
            {
                var config = SpeechConfig.FromSubscription(speechKey, speechRegion);
                using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
                using var recognizer = new SpeechRecognizer(config, audioConfig);

                Debug.WriteLine("Starting recognition...");
                var result = await recognizer.RecognizeOnceAsync();

                switch (result.Reason)
                {
                    case ResultReason.RecognizedSpeech:
                        Debug.WriteLine($"Recognized: {result.Text}");
                        return result.Text;

                    case ResultReason.NoMatch:
                        Debug.WriteLine("No speech could be recognized.");
                        return "Could not understand audio.";

                    case ResultReason.Canceled:
                        var cancellation = CancellationDetails.FromResult(result);
                        Debug.WriteLine($"Canceled: Reason={cancellation.Reason}");
                        if (cancellation.Reason == CancellationReason.Error)
                        {
                            Debug.WriteLine($"ErrorCode={cancellation.ErrorCode}");
                            Debug.WriteLine($"ErrorDetails={cancellation.ErrorDetails}");
                        }
                        return "Speech recognition canceled or failed.";

                    default:
                        return "Unknown error occurred during recognition.";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception during speech recognition: {ex.Message}");
                return "An error occurred while accessing the microphone or speech service.";
            }
        }
        }
}

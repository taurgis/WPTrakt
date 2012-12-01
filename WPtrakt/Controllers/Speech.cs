using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Phone.Speech.Recognition;
using Windows.Phone.Speech.Synthesis;

namespace WPtrakt.Controllers
{
    public class Speech
    {

        public static async Task Speak(string text)
        {
            SpeechSynthesizer tts = new SpeechSynthesizer();
            await tts.SpeakTextAsync(text);
        }

        public static async Task<string> GetResult(string exampleText)
        {
            String text = "";
            SpeechRecognizerUI sr = new SpeechRecognizerUI();
            sr.Recognizer.Grammars.AddGrammarFromPredefinedType("web", SpeechPredefinedGrammar.WebSearch);
            sr.Settings.ListenText = "Listening...";
            sr.Settings.ExampleText = exampleText;
            sr.Settings.ReadoutEnabled = false;
            sr.Settings.ShowConfirmation = false;

            SpeechRecognitionUIResult result = await sr.RecognizeWithUIAsync();
            if (result != null &&
                result.ResultStatus == SpeechRecognitionUIStatus.Succeeded &&
                result.RecognitionResult != null &&
                result.RecognitionResult.TextConfidence != SpeechRecognitionConfidence.Rejected)
            {
                await Speak("Looking for " + result.RecognitionResult.Text);
                text = result.RecognitionResult.Text;

            }
            return text;
        }
    }
}

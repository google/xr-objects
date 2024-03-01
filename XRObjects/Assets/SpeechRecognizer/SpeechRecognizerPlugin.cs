using UnityEngine;

public abstract class SpeechRecognizerPlugin
{
    protected bool isContinuousListening = false;
    protected string language = "en-US";
    protected int maxResults = 10;
    protected string gameObjectName = "SpeechRecognizer";
    
    protected SpeechRecognizerPlugin(string gameObjectName = null)
    {
        this.gameObjectName = gameObjectName;
        this.SetUp();
    }
    public static SpeechRecognizerPlugin GetPlatformPluginVersion(string gameObjectName = null)
    {
        if (Application.isEditor)
            return new SpeechRecognizerPlugin_Editor(gameObjectName);
        else
        {
            #if UNITY_ANDROID
                return new SpeechRecognizerPlugin_Android(gameObjectName);
            #endif
            
#pragma warning disable CS0162 // Unreachable code detected
            Debug.LogWarning("Remember to set project build to mobile device");
#pragma warning restore CS0162 // Unreachable code detected
            return null;
        }
    }

    public enum ERROR { UNKNOWN, INVALID_LANGUAGE_FORMAT }
    public interface ISpeechRecognizerPlugin
    {
        void OnResult(string recognizedResult);
        void OnError(string recognizedError);
    }

    //Features
    protected abstract void SetUp();
    public abstract void StartListening();
    public abstract void StartListening(bool setContinuousListening = false, string language = "en-US", int maxResults = 10);
    public abstract void StopListening();
    
    //Remember that all this modifier-methods will be applied when the last recognition ends...
    //...so only use them if continuous listening is enabled.
    public abstract void SetContinuousListening(bool isContinuousListening);
    public abstract void SetLanguageForNextRecognition(string newLanguage);
    public abstract void SetMaxResultsForNextRecognition(int newMaxResults);
}

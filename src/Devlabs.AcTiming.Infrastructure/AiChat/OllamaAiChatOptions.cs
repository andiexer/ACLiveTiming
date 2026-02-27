namespace Devlabs.AcTiming.Infrastructure.AiChat;

public class OllamaAiChatOptions
{
    public const string SectionName = "AiChat:Ollama";

    public bool IsEnabled { get; set; }
    public Uri? BaseUri { get; set; }
    public string? Model { get; set; }
}

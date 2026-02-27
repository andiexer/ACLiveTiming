using System.Text;
using Devlabs.AcTiming.Application.Shared;
using Microsoft.Extensions.Options;
using OllamaSharp;

namespace Devlabs.AcTiming.Infrastructure.AiChat;

public class OllamaAiChat(IOptions<OllamaAiChatOptions> options) : IAiChat
{
    private readonly OllamaApiClient _client = new(options.Value.BaseUri!, options.Value.Model!);

    public bool IsEnabled => options.Value.IsEnabled;

    public async Task<string> AskQuestionAsync(
        string systemPrompt,
        string userInput,
        CancellationToken cancellationToken = default
    )
    {
        var chat = new Chat(_client, systemPrompt);
        var sb = new StringBuilder();
        await foreach (var response in chat.SendAsync(userInput, cancellationToken))
        {
            sb.Append(response);
        }

        return StripThinkingBlock(sb.ToString());
    }

    // Qwen 3 (and other reasoning models) wrap chain-of-thought in <think>…</think>.
    // Strip it so the UI only shows the final answer.
    private static string StripThinkingBlock(string raw)
    {
        var start = raw.IndexOf("<think>", StringComparison.OrdinalIgnoreCase);
        var end = raw.IndexOf("</think>", StringComparison.OrdinalIgnoreCase);

        if (start < 0 || end < 0 || end <= start)
            return raw.Trim();

        var before = raw[..start];
        var after = raw[(end + "</think>".Length)..];

        return (before + after).Trim();
    }
}

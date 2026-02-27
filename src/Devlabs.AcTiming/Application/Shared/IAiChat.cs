namespace Devlabs.AcTiming.Application.Shared;

public interface IAiChat
{
    public bool IsEnabled { get; }

    public Task<string> AskQuestionAsync(
        string systemPrompt,
        string userInput,
        CancellationToken cancellationToken = default
    );
}

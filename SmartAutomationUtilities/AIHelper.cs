using Azure;
using Azure.AI.OpenAI;
using OpenAI;
using OpenAI.Chat;

namespace SmartAutomationUtilities;


static class SystemMessage
{
    public const string ImagePromptContext = "You are a helpful AI assistant which helps a user to analyze images";
}



public class AIHelper
{
    private readonly string _subscriptionKey;
    private readonly string _endpoint;
    private readonly string _model;

    public AIHelper(string subscriptionKey, string endpoint, string model)
    {
        _subscriptionKey = subscriptionKey;
        _endpoint = endpoint;
        _model = model;
    }

    public async Task<string> AnalyzeImageWithGivenPrompt(byte[] image, string supportingPrompt,
        string initialPrompt,
        string systemMessage = SystemMessage.ImagePromptContext,
        string imageType = "image/png")
    {
        // try
        // {
        //     Uri uri = new(_endpoint);
        //     AzureKeyCredential azureKeyCred = new(_subscriptionKey);
        //     OpenAIClient openAIClient = new(uri, azureKeyCred);

        //     var imageBytes = new BinaryData(image);
        //     var messages = new List<ChatMessage>
        //     {
        //         new ChatMessage(ChatRole.System, systemMessage),
        //         new ChatMessage(ChatRole.User, initialPrompt),
        //         new ChatMessage(ChatRole.User, supportingPrompt)
        //     };

        //     var chatCompletionsOptions = new ChatCompletionsOptions(_model, messages)
        //     {
        //         Messages = { new ChatMessage(ChatRole.User, imageBytes) }
        //     };

        //     var response = await openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
        //     return response.Value.Choices[0].Message.Content;
        // }
        // catch (Exception ex)
        // {
        //     throw new Exception($"Error analyzing image: {ex.Message}");
        // }
    }
}

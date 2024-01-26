using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using dAIlog.Models;
using Newtonsoft.Json;
using OpenAI.Net;

namespace dAIlog.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly HttpClient _httpClient;
    private readonly IOpenAIService _openAıService;
    private readonly string _apiKey;
    private readonly string _orgId;
    private const string ApiUrl = "https://api.openai.com/v1/chat/completions"; 
    
    public HomeController(ILogger<HomeController> logger, IOpenAIService openAıService ,IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _apiKey = configuration["OpenAI:ApiKey"];
        _orgId = configuration["OpenAI:OrgId"];

        
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", _orgId);
        
        _openAıService = openAıService;
    }

    public IActionResult Index()
    {
        return View();
    }
    
    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] MessageModel model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.Message))
        {
            return BadRequest("Message content cannot be null or empty.");
        }

        var data = new
        {
            model = "gpt-3.5-turbo", // Specify the model
            messages = new[]
            {
                new { role = "system", content = "You are a helpful assistant." },
                new { role = "user", content = model.Message }
            }
        };


        var content = new StringContent(JsonConvert.SerializeObject(data), System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(ApiUrl, content);
        var responseString = await response.Content.ReadAsStringAsync();

        return Ok(responseString);
    }
    
    [HttpPost]
    public async Task<IActionResult> SendMessageWithHistory([FromBody] MessageModel model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.Message))
        {
            return BadRequest("Message content cannot be null or empty.");
        }

        var conversationHistory = GetConversationHistory();
        conversationHistory.Add(new ChatMessage("user", model.Message));

        // Convert ChatMessage to Message for OpenAI.Net
        var openAiMessages = conversationHistory.Select(m => Message.Create(m.Role, m.Content)).ToList();

        var response = await _openAıService.Chat.Get(openAiMessages, o => {
            o.MaxTokens = 1000;
        });

        if (response.IsSuccess)
        {
            // Add AI responses to conversation history
            foreach (var choice in response.Result.Choices)
            {
                conversationHistory.Add(new ChatMessage("assistant", choice.Message.Content));
            }

            UpdateConversationHistory(conversationHistory);

            return Ok(new { messages = response.Result.Choices.Select(choice => choice.Message.Content) });
        }
        else
        {
            return BadRequest(response.ErrorMessage);
        }
    }
    
    [HttpPost]
    public async Task<IActionResult> SendMessageToAutogen([FromBody] MessageModel model)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(60000);
            string flaskEndpoint = "http://127.0.0.1:5001/chat"; // Flask endpoint URL
    
            // Construct the JSON object with an "input" key
            var requestData = new { input = model.Message };

            // Send POST request to the Flask app
            var response = await client.PostAsJsonAsync(flaskEndpoint, requestData);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error from Flask API: {response.StatusCode}");
                return StatusCode((int)response.StatusCode, "Error in communication with AutoGen service.");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            return Ok(JsonConvert.DeserializeObject(responseContent));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception in SendMessageToAutogen: {ex.Message}");
            return StatusCode(500, "Internal Server Error");
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    
    private List<ChatMessage> GetConversationHistory()
    {
        var chatHistoryJson = HttpContext.Session.GetString("ChatHistory");
        return string.IsNullOrEmpty(chatHistoryJson) 
            ? new List<ChatMessage>() 
            : JsonConvert.DeserializeObject<List<ChatMessage>>(chatHistoryJson);
    }

    private void UpdateConversationHistory(List<ChatMessage> messages)
    {
        var chatHistoryJson = JsonConvert.SerializeObject(messages);
        HttpContext.Session.SetString("ChatHistory", chatHistoryJson);
    }

    
    [HttpPost]
    public async Task<IActionResult> SendMessageWithAgent([FromBody] MessageModel model)
    {
        switch (model.AgentType)
        {
            case AgentType.Planner:
                return await HandlePlannerAgent(model);
            case AgentType.Programmer:
                return await HandleProgrammerAgent(model);
            case AgentType.QualityAssurance:
                return await HandleQualityAssuranceAgent(model);
            case AgentType.DevOps:
                return await HandleDevOpsAgent(model);
            case AgentType.Designer:
                return await HandleDesignerAgent(model);
            case AgentType.SecurityExpert:
                return await HandleSecurityExpertAgent(model);
            case AgentType.Advisor:
                return await HandleAdvisorAgent(model);
            case AgentType.DocumentationSpecialist:
                return await HandleDocumentationSpecialistAgent(model);
            default:
                return BadRequest("Unknown agent type.");
        }
    }

    
    private async Task<IActionResult> HandlePlannerAgent(MessageModel model)
    {
        var conversationHistory = GetConversationHistory();
        conversationHistory.Add(new ChatMessage("user", model.Message));

        // Craft a prompt specific to the Planner agent
        string plannerPrompt = $"As a project planner, {model.Message}";
        var openAiMessages = conversationHistory.Select(m => Message.Create(m.Role, m.Content)).ToList();
        openAiMessages.Add(Message.Create("system", plannerPrompt));

        var response = await _openAıService.Chat.Get(openAiMessages, o => {
            o.MaxTokens = 1000;
        });

        if (response.IsSuccess)
        {
            // Add AI responses to conversation history
            foreach (var choice in response.Result.Choices)
            {
                conversationHistory.Add(new ChatMessage("assistant", choice.Message.Content));
            }

            UpdateConversationHistory(conversationHistory);

            return Ok(new { messages = response.Result.Choices.Select(choice => choice.Message.Content) });
        }
        else
        {
            return BadRequest(response.ErrorMessage);
        }
    }

    
    private async Task<IActionResult> HandleProgrammerAgent(MessageModel model)
    {
        var conversationHistory = GetConversationHistory();
        conversationHistory.Add(new ChatMessage("user", model.Message));

        // Craft a prompt specific to the Planner agent
        string plannerPrompt = $"As a project programmer, {model.Message}";
        var openAiMessages = conversationHistory.Select(m => Message.Create(m.Role, m.Content)).ToList();
        openAiMessages.Add(Message.Create("system", plannerPrompt));

        var response = await _openAıService.Chat.Get(openAiMessages, o => {
            o.MaxTokens = 1000;
        });

        if (response.IsSuccess)
        {
            // Add AI responses to conversation history
            foreach (var choice in response.Result.Choices)
            {
                conversationHistory.Add(new ChatMessage("assistant", choice.Message.Content));
            }

            UpdateConversationHistory(conversationHistory);

            return Ok(new { messages = response.Result.Choices.Select(choice => choice.Message.Content) });
        }
        else
        {
            return BadRequest(response.ErrorMessage);
        }

    }
    
    private async Task<IActionResult> HandleQualityAssuranceAgent(MessageModel model)
    {
        var conversationHistory = GetConversationHistory();
        conversationHistory.Add(new ChatMessage("user", model.Message));

        // Craft a prompt specific to the Planner agent
        string plannerPrompt = $"As a project Quality Assurance, {model.Message}";
        var openAiMessages = conversationHistory.Select(m => Message.Create(m.Role, m.Content)).ToList();
        openAiMessages.Add(Message.Create("system", plannerPrompt));

        var response = await _openAıService.Chat.Get(openAiMessages, o => {
            o.MaxTokens = 1000;
        });

        if (response.IsSuccess)
        {
            // Add AI responses to conversation history
            foreach (var choice in response.Result.Choices)
            {
                conversationHistory.Add(new ChatMessage("assistant", choice.Message.Content));
            }

            UpdateConversationHistory(conversationHistory);

            return Ok(new { messages = response.Result.Choices.Select(choice => choice.Message.Content) });
        }
        else
        {
            return BadRequest(response.ErrorMessage);
        }

    }
    
    private async Task<IActionResult> HandleDevOpsAgent(MessageModel model)
    {
        var conversationHistory = GetConversationHistory();
        conversationHistory.Add(new ChatMessage("user", model.Message));

        // Craft a prompt specific to the Planner agent
        string plannerPrompt = $"As a project DevOps Engineer, {model.Message}";
        var openAiMessages = conversationHistory.Select(m => Message.Create(m.Role, m.Content)).ToList();
        openAiMessages.Add(Message.Create("system", plannerPrompt));

        var response = await _openAıService.Chat.Get(openAiMessages, o => {
            o.MaxTokens = 1000;
        });

        if (response.IsSuccess)
        {
            // Add AI responses to conversation history
            foreach (var choice in response.Result.Choices)
            {
                conversationHistory.Add(new ChatMessage("assistant", choice.Message.Content));
            }

            UpdateConversationHistory(conversationHistory);

            return Ok(new { messages = response.Result.Choices.Select(choice => choice.Message.Content) });
        }
        else
        {
            return BadRequest(response.ErrorMessage);
        }

    }
    
    private async Task<IActionResult> HandleDesignerAgent(MessageModel model)
    {
        var conversationHistory = GetConversationHistory();
        conversationHistory.Add(new ChatMessage("user", model.Message));

        // Craft a prompt specific to the Planner agent
        string plannerPrompt = $"As a project designer, {model.Message}";
        var openAiMessages = conversationHistory.Select(m => Message.Create(m.Role, m.Content)).ToList();
        openAiMessages.Add(Message.Create("system", plannerPrompt));

        var response = await _openAıService.Chat.Get(openAiMessages, o => {
            o.MaxTokens = 1000;
        });

        if (response.IsSuccess)
        {
            // Add AI responses to conversation history
            foreach (var choice in response.Result.Choices)
            {
                conversationHistory.Add(new ChatMessage("assistant", choice.Message.Content));
            }

            UpdateConversationHistory(conversationHistory);

            return Ok(new { messages = response.Result.Choices.Select(choice => choice.Message.Content) });
        }
        else
        {
            return BadRequest(response.ErrorMessage);
        }

    }
    
    private async Task<IActionResult> HandleSecurityExpertAgent(MessageModel model)
    {
        var conversationHistory = GetConversationHistory();
        conversationHistory.Add(new ChatMessage("user", model.Message));

        // Craft a prompt specific to the Planner agent
        string plannerPrompt = $"As a project security expert, {model.Message}";
        var openAiMessages = conversationHistory.Select(m => Message.Create(m.Role, m.Content)).ToList();
        openAiMessages.Add(Message.Create("system", plannerPrompt));

        var response = await _openAıService.Chat.Get(openAiMessages, o => {
            o.MaxTokens = 1000;
        });

        if (response.IsSuccess)
        {
            // Add AI responses to conversation history
            foreach (var choice in response.Result.Choices)
            {
                conversationHistory.Add(new ChatMessage("assistant", choice.Message.Content));
            }

            UpdateConversationHistory(conversationHistory);

            return Ok(new { messages = response.Result.Choices.Select(choice => choice.Message.Content) });
        }
        else
        {
            return BadRequest(response.ErrorMessage);
        }

    }
    
    private async Task<IActionResult> HandleAdvisorAgent(MessageModel model)
    {
        var conversationHistory = GetConversationHistory();
        conversationHistory.Add(new ChatMessage("user", model.Message));

        // Craft a prompt specific to the Planner agent
        string plannerPrompt = $"As an Mentor and Advisor, {model.Message}";
        var openAiMessages = conversationHistory.Select(m => Message.Create(m.Role, m.Content)).ToList();
        openAiMessages.Add(Message.Create("system", plannerPrompt));

        var response = await _openAıService.Chat.Get(openAiMessages, o => {
            o.MaxTokens = 1000;
        });

        if (response.IsSuccess)
        {
            // Add AI responses to conversation history
            foreach (var choice in response.Result.Choices)
            {
                conversationHistory.Add(new ChatMessage("assistant", choice.Message.Content));
            }

            UpdateConversationHistory(conversationHistory);

            return Ok(new { messages = response.Result.Choices.Select(choice => choice.Message.Content) });
        }
        else
        {
            return BadRequest(response.ErrorMessage);
        }

    }
    
    
    private async Task<IActionResult> HandleDocumentationSpecialistAgent(MessageModel model)
    {
        var conversationHistory = GetConversationHistory();
        conversationHistory.Add(new ChatMessage("user", model.Message));

        string plannerPrompt = $"As a project documentation specialist, {model.Message}";
        var openAiMessages = conversationHistory.Select(m => Message.Create(m.Role, m.Content)).ToList();
        openAiMessages.Add(Message.Create("system", plannerPrompt));

        var response = await _openAıService.Chat.Get(openAiMessages, o => {
            o.MaxTokens = 1000;
        });

        if (response.IsSuccess)
        {
            // Add AI responses to conversation history
            foreach (var choice in response.Result.Choices)
            {
                conversationHistory.Add(new ChatMessage("assistant", choice.Message.Content));
            }

            UpdateConversationHistory(conversationHistory);

            return Ok(new { messages = response.Result.Choices.Select(choice => choice.Message.Content) });
        }
        else
        {
            return BadRequest(response.ErrorMessage);
        }

    }

    
}
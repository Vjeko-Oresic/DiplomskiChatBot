using DiplomskiChatBot.Model;
using DiplomskiChatBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiplomskiChatBot.Bots
{
    public class GPTBot : ActivityHandler
    {
        private readonly IConfiguration _configuration;
        private readonly IStorageHelper _storageHelper;

        private readonly string sorryResponse = "Sorry, I didn't understand that.";

        public GPTBot(IConfiguration configuration, IStorageHelper storageHelper)
        {
            _configuration = configuration;
            _storageHelper = storageHelper;
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var text = turnContext.Activity.Text;
            CompletionRequest request;

            if (text.ToLower().Equals("new"))
            {
                request = new CompletionRequest
                {
                    Model = _configuration["OpenAI:APImodel"],
                    Messages = new List<Messages>()
                    {
                        new Messages()
                        {
                            Role = "user",
                            Content = "hi"
                        }
                    }
                };
            }
            else
            {
                var gptContext = await _storageHelper.GetEntityAsync<GPTResponse>(_configuration["AzureStorage:GPTContext"], turnContext.Activity.From.Id, turnContext.Activity.Conversation.Id);

                if (gptContext != null)
                {
                    request = new CompletionRequest
                    {
                        Model = _configuration["OpenAI:APImodel"],
                        Messages = JsonConvert.DeserializeObject<List<Messages>>(gptContext.GPTContext)
                    };
                    request.Messages.Add(
                      new Messages()
                      {
                          Role = "user",
                          Content = text
                      });
                }
                else
                {
                    request = new CompletionRequest
                    {
                        Model = _configuration["OpenAI:APImodel"],
                        Messages = new List<Messages>()
                        {
                            new Messages()
                            {
                                Role = "user",
                                Content = text
                            }
                        }
                    };
                }
            }

            var gptResponse = await GetGPTResponse(request);

            if (gptResponse != null)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text(gptResponse.Content), cancellationToken);
                request.Messages.Add(
                    new Messages()
                    {
                        Role = gptResponse.Role,
                        Content = gptResponse.Content
                    });

                var gptResponseObject = new GPTResponse()
                {
                    PartitionKey = turnContext.Activity.From.Id,
                    RowKey = turnContext.Activity.Conversation.Id,
                    GPTContext = JsonConvert.SerializeObject(request.Messages)
                };

                await _storageHelper.UpsertEntityAsync(_configuration["AzureStorage:GPTContext"], gptResponseObject);
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text(sorryResponse), cancellationToken);
            }
        }

        private async Task<Message> GetGPTResponse(CompletionRequest request)
        {
            try
            {
                using var client = new HttpClient();
                client.BaseAddress = new Uri(_configuration["OpenAI:APIendpoint"]);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _configuration["OpenAI:APIkey"]);

                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, client.BaseAddress);
                var requestString = JsonConvert.SerializeObject(request);
                requestMessage.Content = new StringContent(requestString, Encoding.UTF8, "application/json");

                using HttpResponseMessage responseMessage = await client.SendAsync(requestMessage).ConfigureAwait(false);

                responseMessage.EnsureSuccessStatusCode();
                string responseString = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                CompletionResponse responseJson = JsonConvert.DeserializeObject<CompletionResponse>(responseString);
                return responseJson.Choices[0].Message;
            }
            catch (HttpRequestException hex)
            {
                Debug.WriteLine(hex.Message);
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Hello and welcome to chat bot with ChatGPT!";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText), cancellationToken);
                }
            }
        }
    }
}
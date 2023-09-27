using Newtonsoft.Json;
using System.Collections.Generic;

namespace DiplomskiChatBot.Model
{
    public class CompletionRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("messages")]
        public List<Messages> Messages { get; set; }
    }

    public class Messages
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }
}
using DiplomskiChatBot.Model;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DiplomskiChatBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly ILogger _logger;
        private readonly string AgePromptDlgId = "AgePromptDialog";

        public MainDialog(ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _logger = logger;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new NumberPrompt<int>(AgePromptDlgId, ValidateAgeAsync));
            AddDialog(new DateResolverDialog());
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new AttachmentPrompt(nameof(AttachmentPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            var waterfallSteps = new WaterfallStep[]
            {
                IntroStepAsync,
                NameStepAsync,
                AgeStepAsync,
                DateStepAsync,
                LanguageStepAsync,
                PhotoStepAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Hi, I'm simple bot for create profil."), cancellationToken);

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var promptMessageText = $"What is your name?";
            var promptMessage = MessageFactory.Text(promptMessageText);

            var retryMessageText = $"Please enter your full name.";
            var retryMessage = MessageFactory.Text(retryMessageText);

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage, RetryPrompt = retryMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> AgeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["name"] = (string)stepContext.Result;

            var promptMessageText = $"How old are your?";
            var promptMessage = MessageFactory.Text(promptMessageText);

            var retryMessageText = $"Please enter a valid age.";
            var retryMessage = MessageFactory.Text(retryMessageText);

            return await stepContext.PromptAsync(AgePromptDlgId,
                new PromptOptions
                {
                    Prompt = promptMessage,
                    RetryPrompt = retryMessage
                }, cancellationToken);
        }

        private async Task<bool> ValidateAgeAsync(PromptValidatorContext<int> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
            {
                var age = promptContext.Recognized.Value;

                Regex regex = new Regex(@"^[0-9]+$");
                if (!regex.IsMatch(age.ToString()))
                {
                    return false;
                }

                if (age < 18)
                {
                    await promptContext.Context.SendActivityAsync(MessageFactory.Text("Welcome young one but you are too young. You must be 18 years old to use chat bot."), cancellationToken);
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                await promptContext.Context.SendActivityAsync(MessageFactory.Text("Please enter a valid age."), cancellationToken);
                return false;
            }
        }

        private async Task<DialogTurnResult> DateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["age"] = (int)stepContext.Result;

            var userDetails = new UserDetails();

            return await stepContext.BeginDialogAsync(nameof(DateResolverDialog), userDetails.Date, cancellationToken);
        }

        private async Task<DialogTurnResult> LanguageStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["date"] = (string)stepContext.Result;

            var promptMessageText = $"Please select your language.";
            var promptMessage = MessageFactory.Text(promptMessageText);

            var retryMessageText = $"Please select a valid option.";
            var retryMessage = MessageFactory.Text(retryMessageText);

            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions { Choices = ChoiceFactory.ToChoices(new List<string> { "English", "Croatian" }), Prompt = promptMessage, RetryPrompt = retryMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> PhotoStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["language"] = ((FoundChoice)stepContext.Result).Value;

            var promptMessageText = $"Please upload your photo.";
            var promptMessage = MessageFactory.Text(promptMessageText);

            var retryMessageText = $"Please upload a valid image.";
            var retryMessage = MessageFactory.Text(retryMessageText);

            return await stepContext.PromptAsync(nameof(AttachmentPrompt), new PromptOptions { Prompt = promptMessage, RetryPrompt = retryMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["photo"] = ((IList<Attachment>)stepContext.Result)[0].ContentUrl;

            var client = new WebClient();
            client.DownloadFile(new Uri((string)stepContext.Values["photo"]), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "Photo.png");

            var promptMessageText = $"Please confirm. Your name is {(string)stepContext.Values["name"]} and your age is {(int)stepContext.Values["age"]}.";
            var promptMessage = MessageFactory.Text(promptMessageText);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                var userDetails = new UserDetails()
                {
                    Name = (string)stepContext.Values["name"],
                    Age = (int)stepContext.Values["age"],
                    Date = (string)stepContext.Values["date"],
                    Language = (string)stepContext.Values["language"],
                    Photo = (string)stepContext.Values["photo"]
                };

                var promptMessageText = "Than you for your data :)";
                var promptMessage = MessageFactory.Text(promptMessageText);
                await stepContext.Context.SendActivityAsync(promptMessage, cancellationToken);

                return await stepContext.EndDialogAsync(userDetails, cancellationToken);
            }
            else
            {
                var promptMessageText = "Than you for your time. See you later.";
                var promptMessage = MessageFactory.Text(promptMessageText);
                await stepContext.Context.SendActivityAsync(promptMessage, cancellationToken);

                var retryMessage = "What else can I do for you?";
                return await stepContext.ReplaceDialogAsync(InitialDialogId, retryMessage, cancellationToken);
            }
        }
    }
}
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Linq;

var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
if (string.IsNullOrEmpty(token))
{
    Console.WriteLine("Ошибка! Токен не найден"); return;
}
using var cts = new CancellationTokenSource();
var bot = new TelegramBotClient(token, cancellationToken: cts.Token);

var me = await bot.GetMe();

Console.WriteLine($"Бот @{me.Username} запущен. Нажмите Enter для остановки.");

bot.OnError += OnError;
bot.OnUpdate += OnUpdate;

Console.ReadLine();
cts.Cancel();


async Task OnError(Exception exception, HandleErrorSource source)
{
    Console.WriteLine(exception);
    await Task.Delay(2000, cts.Token); 
}

async Task OnUpdate(Update update)
{
    switch (update)
    {
        case { Message: { } message }:
            await OnMessage(message);
            break;
        case { CallbackQuery: { } callbackquery }:
            await OnCallbackQuery(callbackquery);
            break;
        case { PollAnswer: { } pollanswer }:
            await OnPollAnswer(pollanswer);
            break;
        default: 
            Console.WriteLine("Ошибка! Необработанное обновление"); break;
    }
}

async Task OnMessage(Message message)
{
    if (string.IsNullOrEmpty(message.Text))
    {
        Console.WriteLine("Получено сообщение из текста");
        return;
    }
    if (message.Text.StartsWith('/'))
    {
        await OnCommand(message.Text, message);
    }
    else
    {
        await OnTextMessage(message);
    }

}


async Task OnTextMessage (Message message)
{
    Console.WriteLine($"Получен текст: {message.Text}");
    await bot.SendMessage(message.Chat, $"Вы написали: {message.Text}");
}

async Task OnCommand(string command, Message message)
{
    switch (command.ToLower())
    {
        case "/start":
           var keyboard = new InlineKeyboardMarkup(new[]{
                InlineKeyboardButton.WithCallbackData("Left", "left"),
                InlineKeyboardButton.WithCallbackData("Right", "right")
            });
            await bot.SendMessage(message.Chat, "Выберите направление", replyMarkup: keyboard);
            break;

        case "/help":
            await bot.SendMessage(message.Chat, "Я умею: /start, /help, /poll");
            break;

        case "/poll":
            await bot.SendPoll(
                chatId: message.Chat,
                question: "Ваш вопрос?",
                options:
                [
                    new InputPollOption("Вариант 1"),
                    new InputPollOption("Вариант 2"),
                    new InputPollOption("Вариант 3")
                ],
                isAnonymous: false);
            break;

        default:
            await bot.SendMessage(message.Chat, "Неизвестная команда. Попробуйте /start");
            break;
    }
}
async Task OnCallbackQuery(CallbackQuery query)
{
    await bot.AnswerCallbackQuery(query.Id);

    if (query.Message is not { } message)
        return;

    if (query.Data == "left")
    {
        await bot.SendMessage(message.Chat.Id, "Вы пошли налево");
    }
    else if (query.Data == "right")
    {
        await bot.SendMessage(message.Chat.Id, "Вы пошли направо");
    }
    else
    {
        await bot.SendMessage(message.Chat.Id, "Неизвестная кнопка");
    }
}

async Task OnPollAnswer(PollAnswer answer)
{
    if (answer.User != null)
    {
        string chosenOptions = string.Join(", ", answer.OptionIds.Select(id => id + 1));
        string text = $"Спасибо за голос, {answer.User.FirstName}! Вы выбрали вариант: {chosenOptions}";

        try
        {
            await bot.SendMessage(answer.User.Id, text);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Не удалось написать пользователю {answer.User.Id}: {ex.Message}");
        }
    }
    else
    {
        Console.WriteLine("Анонимный пользователь проголосовал");
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBot
{
    class TelegramBot
    {
        class TelegramToken
        {
            [JsonPropertyName("token")]
            public string Token { get; set; }
        }

        private static void Main(string[] args)
        {
            if (!System.IO.File.Exists("token.json"))
            {
                Console.Write("Your token : ");
                System.IO.File.WriteAllText("token.json", JsonSerializer.Serialize(new TelegramToken
                {
                    Token = Console.ReadLine()
                }));
            }
            var token = JsonSerializer.Deserialize<TelegramToken>(System.IO.File.ReadAllText("token.json"))?.Token;

            var bot = new TelegramBotClient(token);
            bot.OnMessage += Bot_OnMessage;

            Console.WriteLine("Start...");
            bot.StartReceiving();

            Console.WriteLine("Press any key to close this window . . .");
            Console.ReadKey();
        }
        private static void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            var botClient = sender as TelegramBotClient;

            switch (e.Message.Type)
            {
                case MessageType.Text:
                    {
                        var bitmap = GenerateBmpFromString(e.Message.Text);
                        Console.WriteLine($"\tSent {bitmap.Length} Bytes to {e.Message.Chat.Id}");

                        botClient.SendDocumentAsync(
                            chatId: e.Message.Chat.Id,
                            document: new InputMedia(new MemoryStream(bitmap),
                                $"{e.Message.Chat.Id}_{DateTime.Now:yyMMddhhmmss}.bmp"));
                        break;
                    }
                default:
                    {
                        string[] stickerIds =
                        {
                            "CAACAgIAAxkBAAIBtWE-Wh0ovP8pn3PrbOU5cqC0DQ2fAAIGAQAC9wLIDze9PVBIAAFjkiAE",
                            "CAACAgIAAxkBAAIBuGE-XDA_iwJpME7N4sN8S3KtcKStAALTAwACxKtoC5_3eyr8VMfsIAQ",
                            "CAACAgIAAxkBAAIBumE-XE7agKd_Ox7ODH22UcTbsfyEAAIOAwACbbBCAxdaoJVV2gKmIAQ",
                            "CAACAgIAAxkBAAIBvmE-XG9uWwlMBiP2EnuSb9Nff-T9AAIhAQAC9wLID0T1R1mjMUiuIAQ",
                            "CAACAgIAAxkBAAIBwGE-XIee7zm-SKQTeEMtYj30X_PqAAK1AAOtZbwUK0oHttUzKcIgBA",
                            "CAACAgIAAxkBAAIBwmE-XJ5QyRFar5wEaLKtcX_SrGExAAIiAQACUomRI8jhlEHnwmkzIAQ",
                            "CAACAgIAAxkBAAIBwmE-XJ5QyRFar5wEaLKtcX_SrGExAAIiAQACUomRI8jhlEHnwmkzIAQ",
                            "CAACAgIAAxkBAAIBw2E-XLf5y4S-Lb4EFZtpctMOAZEPAAL5AANWnb0KlWVuqyorGzYgBA",
                            "CAACAgIAAxkBAAIBxWE-XN8R2ElYIn79ZRLU-UlNAAHhhwACRwADUomRI9gLnctNAkcNIAQ",
                            "CAACAgIAAxkBAAIBx2E-XQPTYB7WNcHro9uJWijpXPGJAAKGAQACihKqDtQEq1Y5m_yfIAQ",
                        };

                        botClient.SendStickerAsync(
                            chatId: e.Message.Chat.Id,
                            sticker: stickerIds[new Random().Next(stickerIds.Length)]);
                        break;
                    }
            }
        }

        #region BMPProccessing

        private struct Color
        {
            public byte Red;
            public byte Green;
            public byte Blue;
        }
        private static Color[] StringToColorSet(string str)
        {
            switch (str.Length % 3)
            {
                case 1:
                    str += "\x00\x00";
                    break;
                case 2:
                    str += "\x00";
                    break;
            }

            var colors = new Color[str.Length / 3];
            for (int i = 0; i < str.Length; i += 3)
            {
                colors[i / 3].Red = (byte)str[i + 0];
                colors[i / 3].Green = (byte)str[i + 1];
                colors[i / 3].Blue = (byte)str[i + 2];
            }

            return colors;
        }
        private static byte[] IntToBytes(int number)
        {
            var bytes = new byte[4];

            bytes[0] = (byte)((number & 0xFF000000) >> 24);
            bytes[1] = (byte)((number & 0x00FF0000) >> 16);
            bytes[2] = (byte)((number & 0x0000FF00) >> 8);
            bytes[3] = (byte)((number & 0x000000FF) >> 0);

            return bytes;
        }
        private static byte[] GenerateBmpFromString(string str)
        {
            const int blockHeight = 128;
            const int blockWidth = blockHeight / 2;

            var colorSet = StringToColorSet(str);

            var imgPadding = (colorSet.Length * blockWidth) % 4;
            var imgSize = (colorSet.Length * 3 * blockWidth + imgPadding) * blockHeight;

            var bitmapSize = IntToBytes(imgSize);
            var fullSize = IntToBytes(imgSize + 0x36);
            var width = IntToBytes(colorSet.Length * blockWidth);
            var height = IntToBytes(blockHeight);

            // Header
            var bytes = new List<byte>
            {
                // File info
                0x42, 0x4D,                                                                 // BM
                fullSize[3], fullSize[2], fullSize[1], fullSize[0],                         // Size
                0x00, 0x00,                                                                 // Reserved
                0x00, 0x00,                                                                 // Reserved
                0x36, 0x00, 0x00, 0x00,                                                     // Start

                // Img info
                0x28, 0x00, 0x00, 0x00,                                                     // Header size
                width[3], width[2], width[1], width[0],                                     // Width
                height[3], height[2], height[1], height[0],                                 // Height
                0x01, 0x00,                                                                 // 1
                0x18, 0x00,                                                                 // Depth
                0x00, 0x00, 0x00, 0x00,                                                     // Compression method
                bitmapSize[3], bitmapSize[2], bitmapSize[1], bitmapSize[0],                 // Img Size
                0xC4, 0x0E, 0x00, 0x00,                                                     // Horizontal resolution
                0xC4, 0x0E, 0x00, 0x00,                                                     // Vertical resolution
                0x00, 0x00, 0x00, 0x00,                                                     // N of Colors
                0x00, 0x00, 0x00, 0x00,                                                     // N of Important Colors
            };

            // Bitmap
            for (int i = 0; i < blockHeight; i++)
            {
                // Row
                foreach (var color in colorSet)
                {
                    for (int j = 0; j < blockWidth; j++)
                    {
                        bytes.AddRange(new[]
                        {
                            color.Blue,
                            color.Green,
                            color.Red,
                        });
                    }
                }

                // Padding
                for (int j = 0; j < imgPadding; j++)
                {
                    bytes.Add(0x00);
                }
            }

            return bytes.ToArray();
        }

        #endregion
    }
}

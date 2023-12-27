using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;

class Program
{
    static void Main()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
           .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)

            .CreateLogger();

        string configFile = "config.json";

        try
        {
            List<Config> configs = JsonConvert.DeserializeObject<List<Config>>(File.ReadAllText(configFile));

            foreach (Config config in configs)
            {
                StartServer(config);
            }

         
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Log.Error($"Ошибка: {ex.Message}");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static void StartServer(Config config)
    {
        TcpListener tcpListener = new TcpListener(IPAddress.Parse(config.ServerAddress), config.ServerPort);
        tcpListener.Start();

        Console.WriteLine($"Сервер запущен на {config.ServerAddress}:{config.ServerPort}. Ожидание подключений...");

        Task.Run(async () =>
        {
            while (true)
            {
                TcpClient client = await tcpListener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
        });
    }

    static async Task HandleClientAsync(TcpClient client)
    {
        using (NetworkStream stream = client.GetStream())
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
        {
            string message = await reader.ReadLineAsync();
            Log.Information($"Получено сообщение от клиента: {message}");

            // Обработка сообщения
            string processedMessage = ProcessMessage(message);

            // Отправка ответа клиенту
            await writer.WriteLineAsync(processedMessage);
            await writer.FlushAsync();
        }
    }

    static string ProcessMessage(string message)
    {
       

       
        string[] parts = message.Split(new[] { "#27", "#91" }, StringSplitOptions.None);
        if (parts.Length == 3)
        {
            string header = parts[0];
            string dataSection = parts[1];

            // Разбиваем секцию данных на <Data1> и <Data2>
            string[] dataParts = dataSection.Split(';');
            if (dataParts.Length == 2)
            {
                string data1 = dataParts[0];
                string data2 = dataParts[1];

                
                string newData1 = ProcessData1(data1);
                string newData2 = ProcessData2(data2);

                if (newData1 != "NoRead" && newData2 != "NoRead" && newData1 == data1 && newData2 == data2)
                {
                
                    return $"{header}#27{newData1};{newData2}#91";
                }
                else
                {
                    
                    return $"{header}#27NoRead;{newData2}#91";
                }
            }
        }

      
        return message;
    }

    static string ProcessData(List<string> dataList)
    {
      
        HashSet<string> uniqueValues = new HashSet<string>(dataList);

        // Если у нас есть только одно уникальное значение, возвращаем его, иначе NoRead
        return uniqueValues.Count == 1 ? uniqueValues.First() : "NoRead";
    }
    static string ProcessData1(string data1)
    {
        try
        {
            DateTime date1 = ParseDate(data1);

            //  проверка на определенное условие и возврат нового значения в зависимости от этого условия
            // просто добавим к дате один день.
            DateTime newData1 = date1.AddDays(1);

            // Возвращаем новое значение
            return FormatDate(newData1);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обработке Data1: {ex.Message}");
            return "NoRead"; // Возвращаем NoRead в случае ошибки
        }
    }

    static string ProcessData2(string data2)
    {
        try
        {
            DateTime date2 = ParseDate(data2);

            

            // просто вычитаем из даты один день.
            DateTime newData2 = date2.AddDays(-1);

       
            return FormatDate(newData2);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обработке Data2: {ex.Message}");
            return "NoRead"; 
        }
    }

    // Вспомогательная функция для парсинга даты
    static DateTime ParseDate(string dateString)
    {
    
        return DateTime.ParseExact(dateString, "ddMMyy", null);
    }

    // Вспомогательная функция для форматирования даты
    static string FormatDate(DateTime date)
    {
       
        return date.ToString("ddMMyy");
    }
}

class Config
{
    public string ServerAddress { get; set; }
    public int ServerPort { get; set; }
}

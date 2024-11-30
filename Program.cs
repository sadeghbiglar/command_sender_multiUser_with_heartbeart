﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace RemoteController
{
    
    
    class Program
    {
        public static bool flag = false;
        public const int port = 5000;
        public const int cport = 5001;
        public const int fport = 5002;
        static HttpListener listener;
        static void Main(string[] args)
        {
            Thread menuThread = new Thread(ShowMenu);
            menuThread.Start();
           // StartApi();
            void ShowMenu()
            {
                while (true)
                {
                    Console.WriteLine("\n--- Menu ---");
                    Console.WriteLine("1. Send File To Server");
                    Console.WriteLine("2. Show Online Clients");
                    Console.WriteLine("3. Send Command to Client");
                    Console.WriteLine("4. Exit");
                    Console.Write("Choose an option: ");

                    string choice = Console.ReadLine();

                    switch (choice)
                    {
                        case "1":
                            //  ListClients();
                        // دریافت آدرس IP سرور
                        Console.WriteLine("\nEnter the server IP address (or 'exit' to quit):");
                        string serverIp = Console.ReadLine();
                        if (serverIp?.ToLower() == "exit") break;

                        // دریافت آدرس فایل از کاربر
                        Console.WriteLine("\nEnter the filepath want to send:");
                        string filepath = Console.ReadLine();
                        if (filepath?.ToLower() == "exit") break;
                         // دریافت مسیر مقصد فایل از کاربر
                        Console.WriteLine("\nEnter the destinationpath to save file:");
                        string destinationpath = Console.ReadLine();
                        if (destinationpath?.ToLower() == "exit") break;
                            SendFileWithProgress(serverIp, 5002,filepath, destinationpath);
                            break;
                        case "2":
                            //  ShowOnlineClients();
                            break;
                        case "3":
                            SendCommand();
                            break;
                        case "4":
                            Environment.Exit(0);
                            break;
                        default:
                            Console.WriteLine("\nInvalid option. Please try again.");
                            break;
                    }
                }
            }
            
            void SendCommand()
            {
                while (true)
                {
                    try
                    {
                        // دریافت آدرس IP سرور
                        Console.WriteLine("\nEnter the server IP address (or 'exit' to quit):");
                        string serverIp = Console.ReadLine();
                        if (serverIp?.ToLower() == "exit") break;

                        // دریافت دستور از کاربر
                        Console.WriteLine("\nEnter the command to execute on the server:");
                        string command = Console.ReadLine();
                        if (command?.ToLower() == "exit") break;

                        // اتصال به سرور و ارسال دستور
                        using (TcpClient client = new TcpClient(serverIp, cport))
                        using (NetworkStream stream = client.GetStream())
                        using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            // ارسال دستور به سرور
                            writer.WriteLine(command);
                            Console.WriteLine("\nCommand sent. Waiting for response...");

                            // دریافت نتیجه از سرور
                            string result = reader.ReadToEnd();
                            Console.WriteLine($"Result from server:\n{result}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }
                Thread.Sleep(1000); // شبیه‌سازی تأخیر

            }
            Thread thread1 = new Thread(() =>
            {
                TcpListener listener1 = new TcpListener(IPAddress.Any, port);
                listener1.Start();
                Console.WriteLine("\nStart listening for connections...");

                while (true)
                {
                    try
                    {
                        TcpClient client = listener1.AcceptTcpClient();
                        string clientEndpoint = client.Client.RemoteEndPoint.ToString();
                        string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                        using (NetworkStream stream = client.GetStream())
                        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            string initialMessage = reader.ReadLine();
                            if (initialMessage.StartsWith("register"))
                            {
                                Console.WriteLine($"\nRegister message from client {clientIp}: {initialMessage}");
                            }
                            else if (initialMessage.StartsWith("heartbeat"))
                            {
                                Console.WriteLine($"\nHeartbeat message from client {clientIp}: {initialMessage}");
                            }
                        }

                        client.Close(); // سوکت را پس از پردازش ببندید
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in Thread1: {ex.Message}");
                    }
                }
            });

            thread1.Start();
            void StartApi()
            {
                listener = new HttpListener();
                listener.Prefixes.Add("http://localhost:8000/"); // آدرس API
                listener.Start();
                Console.WriteLine("\nAPI is running on http://localhost:8000/");

                while (true)
                {
                    try
                    {
                        // انتظار برای دریافت درخواست
                        HttpListenerContext context = listener.GetContext();
                        HttpListenerRequest request = context.Request;
                        HttpListenerResponse response = context.Response;

                        // پردازش درخواست
                        if (request.HttpMethod == "POST")
                        {
                            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                            {
                                string body = reader.ReadToEnd();
                                var parameters = ParseBody(body); // تجزیه پارامترها (IP و Command)

                                if (parameters.ContainsKey("ip") && parameters.ContainsKey("command"))
                                {
                                    string serverIp = parameters["ip"];
                                    string command = parameters["command"];

                                    Console.WriteLine($"Received Command: {command} for IP: {serverIp}");

                                    // ارسال دستور به سرور
                                    string result = SendCommandToServer(serverIp, command);

                                    // ارسال پاسخ به کلاینت
                                    byte[] buffer = Encoding.UTF8.GetBytes(result);
                                    response.ContentLength64 = buffer.Length;
                                    response.OutputStream.Write(buffer, 0, buffer.Length);
                                    response.OutputStream.Close();
                                }
                                else
                                {
                                    // پاسخ خطا
                                    response.StatusCode = 400;
                                    byte[] buffer = Encoding.UTF8.GetBytes("Invalid parameters");
                                    response.ContentLength64 = buffer.Length;
                                    response.OutputStream.Write(buffer, 0, buffer.Length);
                                    response.OutputStream.Close();
                                }
                            }
                        }
                        else
                        {
                            // پاسخ برای متدهای غیر POST
                            response.StatusCode = 405; // Method Not Allowed
                            byte[] buffer = Encoding.UTF8.GetBytes("Only POST method is allowed");
                            response.ContentLength64 = buffer.Length;
                            response.OutputStream.Write(buffer, 0, buffer.Length);
                            response.OutputStream.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in API: {ex.Message}");
                    }
                }
            }

            string SendCommandToServer(string serverIp, string command)
            {
                try
                {
                    using (TcpClient client = new TcpClient(serverIp, cport))
                    using (NetworkStream stream = client.GetStream())
                    using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        // ارسال دستور
                        writer.WriteLine(command);

                        // دریافت پاسخ
                        string result = reader.ReadToEnd();
                        return result ?? "No response from server.";
                    }
                }
                catch (Exception ex)
                {
                    return $"Error communicating with server: {ex.Message}";
                }
            }

            Dictionary<string, string> ParseBody(string body)
            {
                var parameters = new Dictionary<string, string>();
                var pairs = body.Split('&');
                foreach (var pair in pairs)
                {
                    var keyValue = pair.Split('=');
                    if (keyValue.Length == 2)
                    {
                        parameters[WebUtility.UrlDecode(keyValue[0])] = WebUtility.UrlDecode(keyValue[1]);
                    }
                }
                return parameters;
            }
            StartApi();
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                if (listener != null)
                {
                    listener.Stop();
                    Console.WriteLine("Listener stopped on exit.");
                }
            };
            void SendFileWithProgress(string serverIp, int port, string filePath, string destinationPath)
            {
                try
                {
                    using (TcpClient client = new TcpClient(serverIp, fport))
                    using (NetworkStream stream = client.GetStream())
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        string fileName = Path.GetFileName(filePath);
                        writer.Write(fileName); // ارسال نام فایل
                        writer.Write(destinationPath); // ارسال مسیر مقصد
                        writer.Write(fs.Length); // ارسال طول فایل

                        byte[] buffer = new byte[8192];
                        int bytesRead;
                        long totalBytesRead = 0;
                        long fileSize = fs.Length;

                        Console.WriteLine($"Sending file: {fileName} ({fileSize} bytes)");

                        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            writer.Write(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;

                            // نمایش پراگرس بار
                            ShowProgress(totalBytesRead, fileSize);
                        }

                        Console.WriteLine("\nFile sent successfully.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending file: {ex.Message}");
                }
            }

            void ShowProgress(long bytesTransferred, long totalBytes)
            {
                int progressBarWidth = 50; // عرض پراگرس بار
                double percentage = (double)bytesTransferred / totalBytes;
                int filledBars = (int)(percentage * progressBarWidth);

                Console.CursorLeft = 0;
                Console.Write("[");
                Console.Write(new string('#', filledBars));
                Console.Write(new string('-', progressBarWidth - filledBars));
                Console.Write($"] {percentage:P0}");
            }


        }
    }
}
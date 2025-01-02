using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using MySqlX.XDevAPI.Common;
using System.Collections.Concurrent;
namespace RemoteController
{


    class Program
    {
        private static readonly string Key = "my-super-secret-key!"; // کلید 32 بایتی
        private static readonly string IV = "my-init-vector-123";    // مقدار IV ثابت 16 بایتی

        public static bool flag = false;
        public const int port = 5000;
        static HttpListener listener;
        static HttpListener listener1;
        private static readonly string connectionString = "Server=localhost;Database=clientsdb;User ID=root;Password=;";
        private static Dictionary<string, TcpClient> clients = new Dictionary<string, TcpClient>();
        private static readonly string key = "YourSecureKey123";
        public static bool isInputMode = false;
        // صف برای پیام‌های Register و Heartbeat
        private static ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

        // صف برای پیام‌های مربوط به کامندها و فایل‌ها
        private static ConcurrentQueue<string> commandQueue = new ConcurrentQueue<string>();
        static void Main(string[] args)
        {
            Thread menuThread = new Thread(ShowMenu);
            menuThread.Start();
            // StartApi();
             static string Encrypt(string plainText)
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(Key.PadRight(32).Substring(0, 32));
                    aes.IV = Encoding.UTF8.GetBytes(IV.PadRight(16).Substring(0, 16));
                    aes.Mode = CipherMode.CBC;

                    using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    {
                        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                        byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                        return Convert.ToBase64String(cipherBytes);
                    }
                }
            }
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
                            isInputMode = true;
                            //  ListClients();
                            // دریافت نام کلاینت هدف
                            Console.WriteLine("\nEnter the client name (or 'exit' to quit):");
                            string clientName = Console.ReadLine();
                            if (clientName?.ToLower() == "exit")
                            {
                                isInputMode = false;
                                break;
                            }
                               
                            // دریافت آدرس فایل از کاربر
                            Console.WriteLine("\nEnter the filepath want to send:");
                            string filepath = Console.ReadLine();
                            if (filepath?.ToLower() == "exit")
                            {
                                isInputMode = false;
                                break;
                            }
                            // دریافت مسیر مقصد فایل از کاربر
                            Console.WriteLine("\nEnter the destinationpath to save file:");
                            string destinationpath = Console.ReadLine();
                            if (destinationpath?.ToLower() == "exit")
                            {
                                isInputMode = false;
                                break;
                            }
                            isInputMode = false;
                            SendFileWithProgress(clientName, filepath, destinationpath);

                            break;
                        case "2":
                            //  ShowOnlineClients();
                            break;
                        case "3":
                            isInputMode = true;
                            SendCommand();
                            break;
                        case "4":
                            isInputMode = false;
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
                        // دریافت نام کلاینت هدف
                        Console.WriteLine("\nEnter the client name (or 'exit' to quit):");
                        string clientName = Console.ReadLine();
                        if (clientName?.ToLower() == "exit")
                        {
                            isInputMode = false;
                            break;
                        }

                        // دریافت دستور از کاربر
                        Console.WriteLine("\nEnter the command to execute on the client:");
                        string command = "";
                        command = Console.ReadLine();
                        if (command?.ToLower() == "exit")
                        {
                            isInputMode = false;
                            break;
                        }

                        // ارسال کامند به کلاینت هدف
                        SendMessageToClient(clientName, command);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }

                Thread.Sleep(1000); // شبیه‌سازی تأخیر
            }
            void AddMessageToQueue(string message)
            {
                lock (messageQueue)
                {
                    messageQueue.Enqueue(message); // پیام به صف اضافه می‌شود
                   // Console.WriteLine($"Message added to queue: {message}");
                }
            }

            void ProcessMessagesFromQueue()
            {
                while (true)
                {
                    lock (messageQueue)
                    {
                        if (messageQueue.TryDequeue(out string message))
                        {
                          //  Console.WriteLine($"Processing message: {message}");

                            if (message.StartsWith("register"))
                            {
                                string clientName = message.Split(':')[1];
                                if (!isInputMode)
                                {
                                    Console.WriteLine($"Registering client: {clientName}");
                                }
                               

                              
                            }
                            else if (message.StartsWith("heartbeat"))
                            {
                                string clientName = message.Split(':')[1];
                                if (!isInputMode)
                                {
                                    Console.WriteLine($"Heartbeat received from client: {clientName}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Unknown message type: {message}");
                            }
                        }
                    }

                    Thread.Sleep(10); // تأخیر کوچک برای بهینه‌سازی CPU
                }
            }





            string SendMessageToClient(string clientName, string command)
            {
                lock (clients)
                {
                    if (clients.ContainsKey(clientName))
                    {
                        try
                        {
                           // NotifyServersBusy();
                            TcpClient client = clients[clientName];
                            if (client.Connected)
                            {
                                NetworkStream stream = client.GetStream();
                                StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                                StreamReader reader = new StreamReader(stream, Encoding.UTF8);

                                // ارسال دستور
                                Thread.Sleep(2000);
                                string encryptedMessage = Encrypt($"cmd:{command}");
                                writer.WriteLine(encryptedMessage);
                                Console.WriteLine("\nCommand sent. Waiting for response...");

                                // دریافت نتیجه
                                string message;
                                StringBuilder resultBuilder = new StringBuilder(); // برای ذخیره نتیجه

                                while ((message = reader.ReadLine()) != null)
                                {
                                    if (message.StartsWith("result:"))
                                    {
                                        string resultLine = message.Substring(7);
                                        resultBuilder.AppendLine(resultLine); // ذخیره هر خط از نتیجه

                                        Console.WriteLine(resultLine); // نمایش در کنسول (اختیاری)
                                    }
                                    else if (message == "endresult")
                                    {
                                        Console.WriteLine("\nEnd of result.");
                                       
                                        break; // پایان دریافت نتیجه
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Unknown message from client {clientName}: {message}");
                                    }
                                }

                                return resultBuilder.ToString(); // بازگرداندن نتیجه به عنوان یک رشته
                            }
                            else
                            {
                                Console.WriteLine($"Client {clientName} is not connected.");
                                return "Error: Client not connected.";
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error sending command to {clientName}: {ex.Message}");
                            return $"Error: {ex.Message}";
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Client {clientName} not found in dictionary.");
                        return "Error: Client not found in dictionary.";
                    }
                }
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
                        string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                        Console.WriteLine($"New connection from {clientIp}");

                        // ایجاد یک ترد جدید برای مدیریت این کلاینت
                        Thread clientThread = new Thread(() => HandleClient(client));
                        clientThread.IsBackground = true;
                        clientThread.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in main listener thread: {ex.Message}");
                    }
                }
            });


            void HandleClient(TcpClient client)
            {
                string clientEndpoint = client.Client.RemoteEndPoint.ToString();
                string clientName = "";
                Console.WriteLine($"Handling client: {clientEndpoint}");

                try
                {
                    NetworkStream stream = client.GetStream();
                    StreamReader reader = new StreamReader(stream, Encoding.UTF8);

                    while (client.Connected)
                    {
                        try
                        {
                            string message = reader.ReadLine();
                            if (message == null) break; // اتصال قطع شده است
                            if (string.IsNullOrEmpty(message)) continue;

                            // بررسی صحت پیام
                            if (IsValidMessage(message))
                            {
                                AddMessageToQueue(message);
                            }
                            else
                            {
                                Console.WriteLine($"Invalid message ignored: {message}");
                            }

                            if (message.StartsWith("register"))
                            {
                                 clientName = message.Split(':')[1];
                                // ذخیره کلاینت در دیکشنری
                                lock (clients)
                                {
                                    if (!clients.ContainsKey(clientName))
                                    {
                                        clients[clientName] = client;
                                        Console.WriteLine($"Client {clientName} added to dictionary.");
                                        stream.Flush();
                                        RegisterOrUpdateClient(clientName,clientEndpoint);
                                    }
                                }

                            }
                            else if (message.StartsWith("heartbeat"))
                            {
                                 clientName = message.Split(':')[1];
                                // بروزرسانی وضعیت کلاینت در دیکشنری
                                lock (clients)
                                {
                                    if (clients.ContainsKey(clientName))
                                    {
                                        clients[clientName] = client;
                                        UpdateClientStatus(clientName);
                                    }
                                }
                            }
                          
                         //   Console.WriteLine($"Message received from {clientEndpoint}: {message}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error while reading message from {clientEndpoint}: {ex.Message}");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in client thread for {clientEndpoint}: {ex.Message}");
                }
                finally
                {
                    // حذف کلاینت از دیکشنری
                    lock (clients)
                    {
                        if (!string.IsNullOrEmpty(clientName) && clients.ContainsKey(clientName))
                        {
                            clients.Remove(clientName);
                            Console.WriteLine($"Client {clientName} removed from dictionary.");

                        }
                    }

                    client.Close();
                    Console.WriteLine($"Client {clientEndpoint} disconnected.");
                }
            }

            bool IsValidMessage(string message)
            {
                return message.StartsWith("register") ||
                       message.StartsWith("heartbeat") ||
                       message.StartsWith("cmd") ||
                       message.StartsWith("file");
            }

           
            thread1.Start();
            Thread messageProcessorThread = new Thread(() =>
            {
                ProcessMessagesFromQueue(); // پردازش پیام‌ها از صف
            });
            messageProcessorThread.Start();

            void StartApi()
            {
                listener1 = new HttpListener();
                listener1.Prefixes.Add("http://localhost:8000/"); // آدرس API
                listener1.Start();
                Console.WriteLine("\nAPI is running on http://localhost:8000/");

                while (true)
                {
                    try
                    {
                        // انتظار برای دریافت درخواست
                        HttpListenerContext context = listener1.GetContext();
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
                                    string result = SendMessageToClient(serverIp, command);

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
            void StartFileApi()
            {
                listener = new HttpListener();
                listener.Prefixes.Add("http://localhost:8001/"); // آدرس API
                listener.Start();
                Console.WriteLine("\nFile API is running on http://localhost:8001/");

                while (true)
                {
                    try
                    {
                        // انتظار برای دریافت درخواست
                        HttpListenerContext context = listener.GetContext();
                        HttpListenerRequest request = context.Request;
                        HttpListenerResponse response = context.Response;

                        if (request.HttpMethod == "POST")
                        {
                            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                            {
                                string body = reader.ReadToEnd();
                                var parameters = ParseBody(body); // تجزیه پارامترها (IP و Path و Dest)

                                if (parameters.ContainsKey("ip") && parameters.ContainsKey("dest"))
                                {
                                    string serverIp = parameters["ip"];
                                    string path = @"C:\xampp\htdocs\upload"; // مسیر فایل‌ها
                                    string dest = parameters["dest"];

                                    Console.WriteLine($"Received request: IP={serverIp}, Destination={dest}");

                                    // خواندن تمام فایل‌های موجود در مسیر
                                    var files = Directory.GetFiles(path);
                                    if (files.Length == 0)
                                    {
                                        Console.WriteLine("No files found in the directory.");
                                        byte[] buffer = Encoding.UTF8.GetBytes("No files found in the directory.");
                                        response.ContentLength64 = buffer.Length;
                                        response.OutputStream.Write(buffer, 0, buffer.Length);
                                        response.OutputStream.Close();
                                        continue;
                                    }

                                    string result = "";

                                    // ارسال هر فایل به سرور
                                    foreach (var filePath in files)
                                    {
                                        string fileName = Path.GetFileName(filePath);
                                        Console.WriteLine($"Sending file: {fileName} to {serverIp}");
                                        string temp_result = SendFileWithProgress(serverIp, filePath, dest) + Environment.NewLine;
                                        result += temp_result + Environment.NewLine;

                                        Thread.Sleep(1000);
                                        Console.WriteLine(temp_result);
                                        // حذف فایل پس از ارسال موفق
                                        try
                                        {
                                            if (temp_result.StartsWith("successfully:"))
                                            {
                                                File.Delete(filePath);
                                                Console.WriteLine($"Deleted file after sending: {fileName}");
                                            }

                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"Error deleting file {fileName}: {ex.Message}");
                                        }

                                    }

                                    // ارسال پاسخ
                                    byte[] responseBuffer = Encoding.UTF8.GetBytes(result);
                                    response.ContentLength64 = responseBuffer.Length;
                                    response.OutputStream.Write(responseBuffer, 0, responseBuffer.Length);
                                    response.OutputStream.Close();
                                }
                                else
                                {
                                    // پاسخ خطا برای پارامترهای نامعتبر
                                    response.StatusCode = 400;
                                    byte[] buffer = Encoding.UTF8.GetBytes("Invalid parameters. Please provide 'ip' and 'dest'.");
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

            Task fileApiTask = Task.Run(() => StartFileApi());
            Task apiTask = Task.Run(() => StartApi());

            Task.WaitAll(fileApiTask, apiTask);

                AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                if (listener != null)
                {
                    listener.Stop();
                    Console.WriteLine("Listener stopped on exit.");
                }
            };
           
            string SendFileWithProgress(string clientName, string filePath, string destinationPath)
            {
                lock (clients)
                {
                    if (!clients.ContainsKey(clientName))
                        return $"Client {clientName} not found in dictionary.";
                    TcpClient client = clients[clientName];
                    NetworkStream stream = client.GetStream();

                    BinaryWriter writer = new BinaryWriter(stream);
                    StreamWriter swriter = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                   // swriter.WriteLine($"file:{clientName}");
                    Thread.Sleep(1000);
                    try
                    {
                       
                        if (!client.Connected)
                            return $"Client {clientName} is not connected.";

                      
                        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                           
                            string encryptedMessage = Encrypt($"file:{clientName}");
                            // ارسال پیام file
                            swriter.WriteLine(encryptedMessage);
                            Thread.Sleep(7000);
                            stream.Flush();

                            string fileName = Path.GetFileName(filePath);
                            Console.WriteLine(fileName);
                            writer.Write(fileName); // ارسال نام فایل
                            writer.Write(destinationPath); // ارسال مسیر مقصد
                            Console.WriteLine(destinationPath);

                            writer.Write(fs.Length); // ارسال طول فایل
                            Console.WriteLine(fs.Length);

                            byte[] buffer = new byte[8192];
                            long totalBytesRead = 0;
                            long fileSize = fs.Length;

                            Console.WriteLine($"Sending file: {fileName} ({fileSize} bytes)");

                            while (true)
                            {
                                int bytesRead = fs.Read(buffer, 0, buffer.Length);
                                if (bytesRead <= 0)
                                    break;

                                writer.Write(buffer, 0, bytesRead);
                                writer.Flush(); // اطمینان از نوشتن کامل داده
                                totalBytesRead += bytesRead;

                                // نمایش پراگرس بار
                                ShowProgress(totalBytesRead, fileSize);
                            }
                          
                            Console.WriteLine("\nFile sent successfully.");

                            stream.Flush();
                          
                            return $"successfully:File {fileName} sent  to {clientName}.";

                        }
                    }
                    catch (Exception ex)
                    {
                        stream.Flush();
                        return $"Error sending file to {clientName}: {ex.Message}";
                    }
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
            void RegisterOrUpdateClient(string clientName, string clientIP)
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // بررسی وجود کلاینت در دیتابیس
                    string checkQuery = "SELECT COUNT(*) FROM clients WHERE client_name = @clientName";
                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection);
                    checkCmd.Parameters.AddWithValue("@clientName", clientName);
                    int clientCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (clientCount > 0)
                    {
                        // به‌روزرسانی رکورد موجود
                        string updateQuery = "UPDATE clients SET last_seen = @lastSeen, ip_address = @ip, status = 'online' WHERE client_name = @clientName";
                        MySqlCommand updateCmd = new MySqlCommand(updateQuery, connection);
                        updateCmd.Parameters.AddWithValue("@lastSeen", DateTime.Now);
                        updateCmd.Parameters.AddWithValue("@ip", clientIP);
                        updateCmd.Parameters.AddWithValue("@clientName", clientName);
                        updateCmd.ExecuteNonQuery();
                    }
                    else
                    {
                        // درج رکورد جدید
                        string insertQuery = "INSERT INTO clients (client_name, ip_address, status, last_seen) VALUES (@clientName, @ip, 'online', @lastSeen)";
                        MySqlCommand insertCmd = new MySqlCommand(insertQuery, connection);
                        insertCmd.Parameters.AddWithValue("@clientName", clientName);
                        insertCmd.Parameters.AddWithValue("@ip", clientIP);
                        insertCmd.Parameters.AddWithValue("@lastSeen", DateTime.Now);
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }

            void UpdateClientStatus(string clientName)
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string updateQuery = "UPDATE clients SET last_seen = @lastSeen, status = 'online' WHERE client_name = @clientName";
                    MySqlCommand updateCmd = new MySqlCommand(updateQuery, connection);
                    updateCmd.Parameters.AddWithValue("@lastSeen", DateTime.Now);
                    updateCmd.Parameters.AddWithValue("@clientName", clientName);
                    updateCmd.ExecuteNonQuery();
                }
            }

        }
    }
}

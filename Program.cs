using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using jsonDataStructs;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;

namespace QBot
{
    class Program
    {
        static IEnumerable<string> files;
        static List<string> keyWord = new List<string>();
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World! Welcome to Qbot");
            bot();
            while (true) ;
        }
        static void refreshFiles()
        {
            files = Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.command");
            foreach (string item in files)
            {
                keyWord.Add(item.Substring(item.LastIndexOf(Path.DirectorySeparatorChar) + 1));
                ColorWriteLine($"Found Command:{item.Substring(item.LastIndexOf(Path.DirectorySeparatorChar) + 1)}", ConsoleColor.Yellow);
            }
        }
        static public void ColorWriteLine(string data, ConsoleColor consoleColor)
        {
            Console.ForegroundColor = consoleColor;
            Console.WriteLine(data);
            Console.ResetColor();
        }

        static string getRandWord(string FilePath)
        {
            StreamReader sr = File.OpenText(FilePath);
            List<string> words = new List<string>();
            string s = "";
            while ((s = sr.ReadLine()) != null)
            {

                words.Add(s);
            }
            Random random = new Random();
            return words[random.Next(0, words.Count)];
        }
        async static public void bot()
        {
            System.Timers.Timer timer = new System.Timers.Timer(500000);
            timer.Elapsed += Timer_Elapsed;
            ColorWriteLine($"CurrentDir:{Directory.GetCurrentDirectory()}", ConsoleColor.Yellow);
            ClientWebSocket cWS = new ClientWebSocket();
            try
            {
                refreshFiles();
                String cqUri = "ws://127.0.0.1:6700/";
                await cWS.ConnectAsync(new Uri(cqUri), CancellationToken.None);
                ColorWriteLine("websocket connected", ConsoleColor.Green);
            }
            catch(Exception ex)
            {
                ColorWriteLine(ex.Message, ConsoleColor.Red);
            }
            while (true)
            {
                Thread.Sleep(10);
                try
                {
                    byte[] receiveBuffer = new byte[4096];
                    ArraySegment<byte> cWsReceiveBuffer = new ArraySegment<byte>(receiveBuffer);
                    CancellationToken cts = new CancellationToken();
                    WebSocketReceiveResult webSocketReceiveResult = await cWS.ReceiveAsync(cWsReceiveBuffer, cts);
                    string cWsReceivedData = Encoding.UTF8.GetString(cWsReceiveBuffer.ToArray());
                    cWsReceivedData = cWsReceivedData.Substring(0, cWsReceivedData.IndexOf('\0'));
                    Console.WriteLine(cWsReceivedData);
                    jsonStruct_receivedData jsonData = JsonSerializer.Deserialize<jsonStruct_receivedData>(cWsReceivedData);
                    Console.WriteLine(jsonData.message);
                    if(jsonData.message!=null)
                    {
                        string command = "";
                        if (jsonData.message.IndexOf(' ') > -1)
                        {
                            command = jsonData.message.Substring(0, jsonData.message.IndexOf(' '));
                        }
                        else
                        {
                            command = jsonData.message;
                        }
                        if (jsonData.post_type == "message" && keyWord.Contains(command + ".command"))
                        {
                            byte[] sendBuffer = new byte[4096];
                            object sendDataParam = null;
                            StreamReader sr = File.OpenText(command + ".command");
                            if (sr.ReadLine() != "[ex]")
                            {
                                if (jsonData.message_type == "group")
                                {
                                    sendDataParam = new jsonStruct_send_group_message { group_id = jsonData.group_id, message = getRandWord(jsonData.message + ".command"), auto_escape = false };
                                }
                                if (jsonData.message_type == "private")
                                {
                                    sendDataParam = new jsonStruct_send_private_message { user_id = jsonData.user_id, message = getRandWord(jsonData.message + ".command"), auto_escape = false };
                                }
                            }
                            else
                            {
                                switch (sr.ReadLine())
                                {
                                    case "[append command]":
                                        {
                                            string message = jsonData.message;
                                            message = message.Substring(message.IndexOf(' ') + 1);
                                            string appendCommand = message.Substring(0, message.IndexOf(' '));
                                            string data = message.Substring(message.IndexOf(' ') + 1);
                                            StreamWriter sw = File.AppendText(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + appendCommand + ".command");
                                            sw.WriteLine(data);
                                            sw.Dispose();
                                            if (jsonData.message_type == "group")
                                            {
                                                sendDataParam = new jsonStruct_send_group_message { group_id = jsonData.group_id, message ="添加成功", auto_escape = true };
                                            }
                                            if (jsonData.message_type == "private")
                                            {
                                                sendDataParam = new jsonStruct_send_private_message { user_id = jsonData.user_id, message = "添加成功", auto_escape = true };
                                            }
                                            refreshFiles();
                                            break;
                                        }
                                    case "[delete commmand]":
                                        {
                                            string message = jsonData.message;
                                            message = message.Substring(message.IndexOf(' ') + 1);
                                            string TargetCommand = message.Substring(0, message.IndexOf(' '));
                                            if(File.Exists(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + TargetCommand + ".command"))
                                            {
                                                File.Delete(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + TargetCommand + ".command");
                                                if (jsonData.message_type == "group")
                                                {
                                                    sendDataParam = new jsonStruct_send_group_message { group_id = jsonData.group_id, message = "操作成功", auto_escape = true };
                                                }
                                                if (jsonData.message_type == "private")
                                                {
                                                    sendDataParam = new jsonStruct_send_private_message { user_id = jsonData.user_id, message = "操作成功", auto_escape = true };
                                                }
                                            }
                                            else
                                            {
                                                throw new QBotException(jsonData,"文件不存在");
                                            }
                                            break;
                                        }
                                    default:
                                        break;
                                }
                            }

                            jsonStruct_api apiData = new jsonStruct_api { action = "send_msg", param = sendDataParam };
                            Console.WriteLine($"apidata:{JsonSerializer.Serialize<jsonStruct_api>(apiData)}");
                            if (sendDataParam != null) await cWS.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize<jsonStruct_api>(apiData))), WebSocketMessageType.Text, true, cts);
                        }
                    }
                }
                catch (QBotException ex)
                {
                    //ex.SendOutError(cWS); //未测试
                }
                catch (Exception ex)
                {
                    ColorWriteLine($"Error:{ex.Message}",ConsoleColor.Red);
                    //cWS.Dispose();
                }
            }

        }

        private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            refreshFiles();
        }
    }
    class QBotException:Exception
    {
        private jsonStruct_receivedData jsonData;
        private string message=null;
        public QBotException(jsonStruct_receivedData Data)
        {
            jsonData = Data;
        }
        public QBotException(jsonStruct_receivedData Data,string msg) 
        {
            jsonData = Data;
            message = msg;
        }
        public async void SendOutError(ClientWebSocket cWS)
        {
            if (message == null) message = "其他错误";
            if(cWS.State==WebSocketState.Connecting)
            {
                object sendDataParam=null;
                if (jsonData.message_type == "group")
                {
                    sendDataParam = new jsonStruct_send_group_message { group_id = jsonData.group_id, message = message, auto_escape = true };
                }
                if (jsonData.message_type == "private")
                {
                    sendDataParam = new jsonStruct_send_private_message { user_id = jsonData.user_id, message = message, auto_escape = true };
                }
                jsonStruct_api apiData = new jsonStruct_api { action = "send_msg", param = sendDataParam };
                CancellationToken cts = new CancellationToken();
                Console.WriteLine($"apidata:{JsonSerializer.Serialize<jsonStruct_api>(apiData)}");
                if (sendDataParam != null) await cWS.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize<jsonStruct_api>(apiData))), WebSocketMessageType.Text, true, cts);
            }
        }
    }
}

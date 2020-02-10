using System;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using dotnet_etcd;
using Etcdserverpb;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Mvccpb;

namespace Etcd.DotNetCore.Test
{
    class Program
    {
        private static EtcdClient m_client;

        private static string m_ClientName = "Client0";


        static async Task Main(string[] args)
        {
            //Console.WriteLine("Hello World!");


            

            if (args != null && args.Length > 0)
            {
                m_ClientName = args[0];
            }

            Console.WriteLine($"当前运行{m_ClientName}");

            m_client = NewClient();

            try
            {

                if (m_client != null)
                {
                    Console.WriteLine($"{m_ClientName}连接成功");
                    //await CreateClientTree();
                    //await CreateMessageTree();


                    await ClientWatcher();
                    await UpdateOnlineState();
                    //await GroupMsgWatcher();
                    //await PrivateMsgWatcher();
                }



                //CreateClientTree();
                //CreateMessageTree();


                bool isExit = false;


                while (!isExit)
                {
                    var command = Console.ReadLine();

                    if (command == "exit")
                    {
                        isExit = true;
                    }
                    else
                    {
                        await ExcuteCommand(command);
                    }


                }

                await ExitCommand();


                if (m_client != null)
                {
                    m_client.Dispose();
                }

            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
            {
                // Do something with the exception. 
            }


            Console.ReadLine();

        }


        /// <summary>
        /// 新建Client
        /// </summary>
        /// <returns></returns>
        private static EtcdClient NewClient()
        {
            EtcdClient etcdClient = new EtcdClient("http://192.168.1.14:2380");

            return etcdClient;
        }

        /// <summary>
        /// 创建Client节点树
        /// </summary>
        /// <returns></returns>
        private static async Task CreateClientTree()
        {
            if (m_client != null)
            {
                //移除Client所有节点
                await m_client.DeleteRangeAsync("Client/");
                await m_client.PutAsync("Client/Client0", "OffLine");
                await m_client.PutAsync("Client/Client1", "OffLine");
                await m_client.PutAsync("Client/Client2", "OffLine");
            }
        }

        /// <summary>
        /// 创建Message节点树
        /// </summary>
        /// <returns></returns>
        private static async Task CreateMessageTree()
        {
            if (m_client != null)
            {
                //移除Message所有节点
                await m_client.DeleteRangeAsync("Message/");
                await m_client.PutAsync("/Message/GroupMessage", "");
                await m_client.PutAsync("/Message/PrivateMessage/Client0", "");
                await m_client.PutAsync("/Message/PrivateMessage/Client1", "");
                await m_client.PutAsync("/Message/PrivateMessage/Client2", "");
            }
        }

        /// <summary>
        /// 下线退出
        /// </summary>
        /// <returns></returns>
        private static async Task ExitCommand()
        {
            if (m_client != null)
            {
                string path = $"Client/{m_ClientName}";

                await m_client.DeleteAsync(path);
                await m_client.PutAsync(path, "OffLine");

                await OutputOnlineState();

                m_client.Dispose();
                m_client = null;

                Console.WriteLine($"{m_ClientName}已经关闭");
            }

        }


        static private async Task ExcuteCommand(string commandStr)
        {
            //查询在线状态
            if (commandStr == "state")
            {
                await OutputOnlineState();
            }
            else if(commandStr.StartsWith("update "))
            {

                string msg = commandStr.Replace("update ", "");
                await UpdateOnlineState(msg);
            }

            //发送群组消息
            else if (commandStr.StartsWith("groupmsg "))
            {
                //string msg = commandStr.Replace("groupmsg ", "");

                //await SendGroupMessage(msg);
            }
            //发送私有消息
            else if (commandStr.StartsWith("privatemsg "))
            {
                //string str = commandStr.Replace("privatemsg ", "");
                //var result = str.Split(" ");

                //if (result.Length >= 2)
                //{
                //    await SendPrivateMessage(result[0], result[1]);
                //}

            }
        }


        #region 在线状态

        /// <summary>
        /// 更新该节点上线状态
        /// </summary>
        /// <returns></returns>
        private static async Task UpdateOnlineState()
        {
            if (m_client != null)
            {
                string path = $"Client/{m_ClientName}";
                await m_client.DeleteAsync(path);
                await m_client.PutAsync(path, "OnLine");
            }
        }

        private static async Task UpdateOnlineState(string state)
        {
            if (m_client != null)
            {
                string path = $"Client/{m_ClientName}";
                await m_client.DeleteAsync(path);
                await m_client.PutAsync(path, state);
            }
        }

        /// <summary>
        /// 输出在线状态
        /// </summary>
        /// <returns></returns>
        private static async Task OutputOnlineState()
        {

            try
            {
                if (m_client != null)
                {
                    var childs = await m_client.GetRangeAsync("Client/");

                    foreach (var child in childs.Kvs)
                    {
                        var key = child.Key.ToStringUtf8().Replace("Client/", "");
                        var value = child.Value.ToStringUtf8();

                        string result = $"{key}状态是{value}";
                        Console.WriteLine(result);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }


        #endregion


        #region Watcher

        /// <summary>
        /// 检测Client上下线
        /// </summary>
        /// <returns></returns>
        private static async Task ClientWatcher()
        {
            if (m_client != null)
            {
                try
                {

                    var childs = await m_client.GetRangeAsync("Client/");
                    foreach (var child in childs.Kvs)
                    {
                        string temp = $"方法 ClientWatcher 开始Watch{child.Key.ToStringUtf8()}";
                        Console.WriteLine(temp);


                        WatchRequest request = new WatchRequest()
                        {
                            CreateRequest = new WatchCreateRequest()
                            {
                                Key = child.Key
                            }
                        };

                         m_client.WatchRange(request, (response =>
                            {
                                foreach (WatchEvent e1 in response)
                                {
                                    if (e1.Type == Event.Types.EventType.Put)
                                    {
                                        string client = e1.Key.Replace("Client/", "");
                                        string onlineresult = e1.Value;
                                        string result = $"方法 ClientWatcher  Put 通知:{client}状态是{onlineresult}";
                                        Console.WriteLine(result);

                                        AddWatcher(e1.Key);
                                    }
                                    else if (e1.Type == Event.Types.EventType.Delete)
                                    {
                                        string client = e1.Key.Replace("Client/", "");
                                        string onlineresult = e1.Value;
                                        string result = $"方法 ClientWatcher Delete 通知:{client}Delete";
                                        Console.WriteLine(result);
                                    }


                                }
                            })
                            , null, exceptionAction);

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }




            }
        }


        private static void AddWatcher(string pathkey)
        {
            if (m_client != null)
            {
                try
                {

                    WatchRequest request = new WatchRequest()
                    {
                        CreateRequest = new WatchCreateRequest()
                        {
                            Key = ByteString.CopyFromUtf8(pathkey)
                        }
                    };
                    string temp = $"方法 AddWatcher 开始Watch{pathkey}";
                    Console.WriteLine(temp);

                    m_client.WatchRange(request, (response =>
                        {
                            foreach (WatchEvent e1 in response)
                            {
                                if (e1.Type == Event.Types.EventType.Put)
                                {
                                    string client = e1.Key.Replace("Client/", "");
                                    string onlineresult = e1.Value;
                                    string result = $"方法 ClientWatcher  Put 通知:{client}状态是{onlineresult}";
                                    Console.WriteLine(result);

                                     AddWatcher(e1.Key);
                                }
                                else if (e1.Type == Event.Types.EventType.Delete)
                                {
                                    string client = e1.Key.Replace("Client/", "");
                                    string onlineresult = e1.Value;
                                    string result = $"方法 ClientWatcher Delete 通知:{client}Delete";
                                    Console.WriteLine(result);
                                }
                            }
                        })
                        , null, exceptionAction);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }




            }
        }

        private static void exceptionAction(Exception obj)
        {
            //ClientWatcherOld();
        }



        #endregion
    }
}

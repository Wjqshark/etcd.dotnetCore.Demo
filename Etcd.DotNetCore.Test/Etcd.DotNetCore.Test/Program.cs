using System;
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


                    await UpdateOnlineState();
                    await ClientWatcher();
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
                await m_client.PutAsync(path, "OffLine");

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
                await m_client.PutAsync(path, "OnLine");
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
                var childs = await m_client.GetRangeAsync("Client/");

                foreach (var child in childs.Kvs)
                {

                    m_client.Watch(child.Key.ToStringUtf8(), (response =>
                    {
                        foreach (WatchEvent e1 in response)
                        {
                            if (e1.Type == Event.Types.EventType.Put)
                            {
                                string client = e1.Key.Replace("Client/", "");
                                string onlineresult = e1.Value;
                                string result = $"通知:{client}状态是{onlineresult}";
                                Console.WriteLine(result);

                            }
                        }
                    }), null, exceptionAction);
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

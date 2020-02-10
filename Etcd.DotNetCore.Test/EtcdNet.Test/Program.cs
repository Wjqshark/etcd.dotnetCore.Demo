using System;
using System.Threading.Tasks;

namespace EtcdNet.Test
{
    class Program
    {
        private static EtcdClient m_client;

        private static string m_ClientName = "Client0";


        static async Task Main(string[] args)
        {
            try
            {
                if (args != null && args.Length > 0)
                {
                    m_ClientName = args[0];
                }

                Console.WriteLine($"当前运行{m_ClientName}");

                m_client = NewClient();

                await OutputOnlineState();

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }



        }



        /// <summary>
        /// 新建Client
        /// </summary>
        /// <returns></returns>
        private static EtcdClient NewClient()
        {

            try
            {
                EtcdClientOpitions options = new EtcdClientOpitions()
                {
                    Urls = new string[] { "http://192.168.1.14:2380" },
                    Username = "",
                    Password = "",
                    UseProxy = false,
                    IgnoreCertificateError = true,
                };
                EtcdClient etcdClient = new EtcdClient(options);

                return etcdClient;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }

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
                await m_client.DeleteNodeAsync("Client/");
                await m_client.CreateNodeAsync("Client/Client0", "OffLine");
                await m_client.CreateNodeAsync("Client/Client1", "OffLine");
                await m_client.CreateNodeAsync("Client/Client2", "OffLine");
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
                await m_client.DeleteNodeAsync("Message/");
                await m_client.CreateNodeAsync("/Message/GroupMessage", "");
                await m_client.CreateNodeAsync("/Message/PrivateMessage/Client0", "");
                await m_client.CreateNodeAsync("/Message/PrivateMessage/Client1", "");
                await m_client.CreateNodeAsync("/Message/PrivateMessage/Client2", "");
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
                string path = $"/Client/{m_ClientName}";
                await m_client.SetNodeAsync(path, "OnLine");
            }
        }

        private static async Task UpdateOnlineState(string state)
        {
            if (m_client != null)
            {
                string path = $"/Client/{m_ClientName}";
                await m_client.SetNodeAsync(path, state);
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
                    //var response = await m_client.GetNodeAsync("/Client");

                    try
                    {

                        EtcdResponse resp1 = await m_client.CreateNodeAsync("/Client", "valye");

                        EtcdResponse resp = await m_client.SetNodeAsync("/Client", "valye");
                        Console.WriteLine("Key `{0}` is changed, modifiedIndex={1}", "/Client", resp.Node.ModifiedIndex);


                        var value = await m_client.GetNodeValueAsync("/Client",true);
                        Console.WriteLine("The value of `{0}` is `{1}`", "/Client", value);
                    }
                    catch (EtcdCommonException.KeyNotFound)
                    {
                        Console.WriteLine("Key `{0}` does not exist", "/Client");
                    }


                    var response = await m_client.GetNodeAsync("/",true);

                    if (response != null && response.Node != null)
                    {
                        if (response.Node.Nodes != null)
                        {
                            foreach (var child in response.Node.Nodes)
                            {
                                var key = child.Key.Replace("/Client/", "");
                                var value = child.Value;

                                string result = $"{key}状态是{value}";
                                Console.WriteLine(result);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }


        #endregion
    }

    class NewtonsoftJsonDeserializer : EtcdNet.IJsonDeserializer
    {
        public T Deserialize<T>(string json)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }
    }
}

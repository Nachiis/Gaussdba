using SqlSugar;
using System;
using System.Data;
using System.Data.Common;

namespace Gaussdb
{
    class Program
    {
        static void Main(string[] args)
        {
            MessageProcesser.Instance.Init();
            SqlSugar.Instance.Init();
            SqlServer.Server().GetAwaiter().GetResult();
        }
    }
}
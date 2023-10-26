using RabbitMQ.Client;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Text.Json;

namespace UdemyRabbitMQ.Publisher
{
    public enum LogNames
    {
        Critical=1,
        Error=2,
        Warning=3,
        Information=4,
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory();
            factory.Uri = new Uri("amqp://guest:guest@localhost:5672");

            using var connection = factory.CreateConnection();

            var channel = connection.CreateModel();

            //channel.QueueDeclare("hello-queue", true, false, false);

            channel.ExchangeDeclare("header-exchange", durable: true,type:ExchangeType.Headers);

            Dictionary<string, object> header = new Dictionary<string, object>();

            header.Add("format","pdf");
            header.Add("shape","a4");

            var properties = channel.CreateBasicProperties();
            properties.Headers = header;
            properties.Persistent = true;

            var product = new Product { Id = 1, Name = "kalem", Price = 100, Stock = 10 };
            var productJsonString = JsonSerializer.Serialize(product);

            channel.BasicPublish("header-exchange", string.Empty, properties, Encoding.UTF8.GetBytes(productJsonString));
            Console.WriteLine("mesaj gönderildi");

            Console.ReadLine();

            
        }
    }
}

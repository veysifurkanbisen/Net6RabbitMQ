using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace UdemyRabbitMQ.Subscriber
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory();
            factory.Uri = new Uri("amqp://guest:guest@localhost:5672");

            using var connection = factory.CreateConnection();

            var channel = connection.CreateModel();
            channel.ExchangeDeclare("header-exchange", durable: true, type: ExchangeType.Headers);
            channel.BasicQos(0, 1, false);

            var consumer = new EventingBasicConsumer(channel);

            var queueName = channel.QueueDeclare().QueueName;

            Dictionary<string, object> header = new Dictionary<string, object>();

            header.Add("format", "pdf");
            header.Add("shape", "a4");
            header.Add("x-match", "any");

            channel.QueueBind(queueName,"header-exchange","",header);

            channel.BasicConsume(queueName,false,consumer);

            Console.WriteLine("logları dinliyorum");

            consumer.Received += (object sender, BasicDeliverEventArgs e) =>
            {
                var message = Encoding.UTF8.GetString(e.Body.ToArray());

                Product product = JsonSerializer.Deserialize<Product>(message);

                Thread.Sleep(1500);
                Console.WriteLine($"Gelen mesaj: {product.Id}-{product.Name}-{product.Price}-{product.Stock}");
                
                channel.BasicAck(e.DeliveryTag, false);
            };
            
            Console.ReadLine();
        }

       
    }
}

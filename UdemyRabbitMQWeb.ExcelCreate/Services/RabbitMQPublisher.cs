﻿using RabbitMQ.Client;
using Shared;
using System.Text;
using System.Text.Json;

namespace UdemyRabbitMQWeb.ExcelCreate.Services
{
    public class RabbitMQPublisher
    {
        private readonly RabbitMQClientService _rabbitmqClientService;

        public RabbitMQPublisher(RabbitMQClientService rabbitmqClientService)
        {
            _rabbitmqClientService = rabbitmqClientService;
        }
        public void Publish(CreateExcelMessage createExcelMessage)
        {
            var channel = _rabbitmqClientService.Connect();

            var bodyString = JsonSerializer.Serialize(createExcelMessage);
            var bodyByte = Encoding.UTF8.GetBytes(bodyString);
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            channel.BasicPublish(RabbitMQClientService.ExchangeName,
                RabbitMQClientService.RoutingExcel, properties, bodyByte);
        }
    }

}

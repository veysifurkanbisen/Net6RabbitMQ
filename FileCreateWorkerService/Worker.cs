using ClosedXML.Excel;
using FileCreateWorkerService.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FileCreateWorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly RabbitMQClientService _rabbitmqClientService;
        private readonly IServiceProvider _serviceProvider;
        private IModel _model;
        public Worker(ILogger<Worker> logger, RabbitMQClientService rabbitMQClientService, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _rabbitmqClientService = rabbitMQClientService;
            _serviceProvider = serviceProvider;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _model = _rabbitmqClientService.Connect();
            _model.BasicQos(0, 1, false);
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumer = new AsyncEventingBasicConsumer(_model);
                _model.BasicConsume(RabbitMQClientService.QueueName, false, consumer);
                consumer.Received += Consumer_Received;
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            await Task.Delay(5000);

            var excel = JsonSerializer.Deserialize<CreateExcelMessage>(Encoding.UTF8.GetString(@event.Body.ToArray()));

            using var ms = new MemoryStream();

            var wb = new XLWorkbook();
            var ds = new DataSet();
            ds.Tables.Add(GetTable("products"));

            wb.Worksheets.Add(ds);
            wb.SaveAs(ms);

            MultipartFormDataContent multipartFormDataContent = new MultipartFormDataContent();
            
            multipartFormDataContent.Add(new ByteArrayContent(ms.ToArray()),"file",Guid.NewGuid().ToString()+".xlxs");

            var baseUrl = "https://localhost:44321/api/files";
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsync(baseUrl, multipartFormDataContent);
                if (response.IsSuccessStatusCode)
                    _model.BasicAck(@event.DeliveryTag, false);
            }

            
        }

        private DataTable GetTable(string tableName)
        {
            //List<Product> products;
            return new DataTable();
        }
    }
}

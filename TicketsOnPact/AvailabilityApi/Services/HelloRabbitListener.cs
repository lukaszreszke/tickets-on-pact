namespace AvailabilityApi.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

public class HelloRabbitListener : BackgroundService
{
    
    private readonly ILogger<HelloRabbitListener> _logger;
    private readonly IConfiguration _configuration;

    public HelloRabbitListener(ILogger<HelloRabbitListener> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitMqHost = _configuration["RabbitMQ:Host"];
        var factory = new ConnectionFactory { Uri = new Uri($"amqp://zenek:secret@{rabbitMqHost}:5672/") };

        using var connection = await factory.CreateConnectionAsync();
        using var channel    = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: "hello",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            _logger.LogInformation($"Received: {message}");
            await Task.CompletedTask;
        };

        await channel.BasicConsumeAsync(
            queue: "hello",
            autoAck: true,
            consumer: consumer);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
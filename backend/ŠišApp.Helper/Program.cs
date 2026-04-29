using MassTransit;
using ŠišApp.Helper.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<EmailConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = builder.Configuration["RabbitMQ:Host"]
            ?? throw new InvalidOperationException("RabbitMQ host is not configured");
        var rabbitUser = builder.Configuration["RabbitMQ:Username"]
            ?? throw new InvalidOperationException("RabbitMQ username is not configured");
        var rabbitPass = builder.Configuration["RabbitMQ:Password"]
            ?? throw new InvalidOperationException("RabbitMQ password is not configured");
        cfg.Host(rabbitHost, "/", h => {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        cfg.ReceiveEndpoint("email-queue", e =>
        {
            e.UseMessageRetry(r => r.Intervals(
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(8)));
            e.ConfigureConsumer<EmailConsumer>(context);
        });
    });
});

var host = builder.Build();
host.Run();

﻿using System.Reflection;

using AppEngine.Json;

using Azure.Messaging.ServiceBus;

using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AppEngine.ServiceBus;

public class MessageQueueReceiver : IAsyncDisposable
{
    private readonly IServiceProvider _container;
    private readonly Serializer _serializer;
    private readonly ServiceBusClient _client;
    private readonly ILogger _logger;
    //private readonly IMediator _mediator;
    private readonly MethodInfo _typedProcessMethod;
    private ServiceBusProcessor? _processor;

    public MessageQueueReceiver(//IMediator mediator,
                                ILogger<MessageQueueReceiver> logger,
                                IServiceProvider container,
                                Serializer serializer,
                                ServiceBusClient client)
    {
        //_mediator = mediator;
        _logger = logger;
        _container = container;
        _serializer = serializer;
        _client = client;

        var typedProcessMethod = typeof(MessageQueueReceiver).GetMethod(nameof(ProcessTypedMessage),
                                                                        BindingFlags.NonPublic | BindingFlags.Instance)!;

        if (typedProcessMethod == null)
        {
            throw new MissingMethodException("Method ProcessTypedMessage<TRequest> not found");
        }

        _typedProcessMethod = typedProcessMethod;
    }

    public async void StartReceiveLoop()
    {
        if (_processor != null)
        {
            return;
        }

        var options = new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 1,
            AutoCompleteMessages = true
        };
        _processor = _client.CreateProcessor(CommandQueue.CommandQueueName, options);
        _processor.ProcessMessageAsync += ProcessMessage;
        _processor.ProcessErrorAsync += ProcessError;

        await _processor.StartProcessingAsync();
    }

    private Task ProcessError(ProcessErrorEventArgs arg)
    {
        _logger.LogError(arg.Exception,
                         $"Error while processing a message from queue. Namespace: {0}, Identifier: {1}, ErrorSource: {2}, ClientId {3}, Exception: {4}, EntityPath {5}",
                         arg.FullyQualifiedNamespace,
                         arg.Identifier,
                         arg.ErrorSource,
                         arg.Exception.Message,
                         arg.EntityPath);

        return Task.CompletedTask;
    }

    private async Task ProcessMessage(ProcessMessageEventArgs arg)
    {
        var commandMessage = _serializer.Deserialize<CommandMessage>(arg.Message.Body.ToString());

        if (commandMessage?.CommandType == null)
        {
            throw new ArgumentException($"Invalid message: {arg.Message.Body}");
        }

        var commandType = Type.GetType(commandMessage.CommandType);

        if (commandType == null)
        {
            throw new ArgumentException($"Unknown command type: {commandType}");
        }

        var typedMethod = _typedProcessMethod.MakeGenericMethod(commandType);

        if (typedMethod.Invoke(this, [commandMessage.CommandSerialized, CommandQueue.CommandQueueName, arg.CancellationToken]) is Task task)
        {
            await task;
        }
    }

    private async Task ProcessTypedMessage<TRequest>(string? commandSerialized,
                                                     string queueName,
                                                     CancellationToken cancellationToken)
        where TRequest : IRequest
    {
        var request = default(TRequest);

        if (commandSerialized != null)
        {
            request = _serializer.Deserialize<TRequest>(commandSerialized);
        }

        request ??= Activator.CreateInstance<TRequest>();

        await using var scope = _container.CreateAsyncScope();
        _container.GetService<SourceQueueProvider>()!.SourceQueueName = queueName;
        //await _mediator.Send(request, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_processor != null)
        {
            await _processor.StopProcessingAsync();
            await _processor.DisposeAsync();
        }
    }
}
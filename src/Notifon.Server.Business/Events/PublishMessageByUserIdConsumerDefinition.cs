using MassTransit;

namespace Notifon.Server.Business.Events;

public class PublishMessageByUserIdConsumerDefinition : ConsumerDefinition<PublishMessageByUserIdConsumer> {
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
                                              IConsumerConfigurator<PublishMessageByUserIdConsumer> consumerConfigurator) { }
}
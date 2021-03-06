﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using CaseOnline.Azure.WebJobs.Extensions.Mqtt.Config;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using MQTTnet;

namespace CaseOnline.Azure.WebJobs.Extensions.Mqtt.Bindings
{
    /// <summary>
    /// Provides binding of the <see cref="MqttTriggerAttribute"/>.
    /// </summary>
    public class MqttTriggerAttributeBindingProvider : ITriggerBindingProvider
    {
        private readonly INameResolver _nameResolver;
        private readonly ILogger _logger; 

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttTriggerAttribute"/>.
        /// </summary>
        /// <param name="nameResolver">The name resolver.</param>
        /// <param name="logger">The logger.</param>
        internal MqttTriggerAttributeBindingProvider(INameResolver nameResolver, ILogger logger)
        {
            _nameResolver = nameResolver;
            _logger = logger; 
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var mqttTriggerAttribute = GetMqttTriggerAttribute(context.Parameter);
            if (mqttTriggerAttribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }

            _logger.LogDebug($"Creating binding for parameter '{context.Parameter.Name}', using custom config creator is '{mqttTriggerAttribute.UseCustomConfigCreator}'");

            var mqttTriggerBinding = GetMqttTriggerBinding(context.Parameter, mqttTriggerAttribute);

            _logger.LogDebug($"Succesfully created binding for parameter '{context.Parameter.Name}'");

            return Task.FromResult<ITriggerBinding>(mqttTriggerBinding);
        }

        private MqttTriggerAttribute GetMqttTriggerAttribute(ParameterInfo parameter)
        {
            var mqttTriggerAttribute = parameter.GetCustomAttribute<MqttTriggerAttribute>(inherit: false);

            if (mqttTriggerAttribute == null)
            {
                return null;
            }

            if (parameter.ParameterType != typeof(PublishedMqttMessage))
            {
                throw new InvalidOperationException($"Can't bind MqttTriggerAttribute to type '{parameter.ParameterType}'.");
            }

            return mqttTriggerAttribute;
        }

        private MqttTriggerBinding GetMqttTriggerBinding(ParameterInfo parameter, MqttTriggerAttribute mqttTriggerAttribute)
        {
            var attributeToConfigConverter = new AttributeToConfigConverter(mqttTriggerAttribute, _nameResolver, _logger);

            MqttConfiguration mqttConfiguration;
            try
            {
                mqttConfiguration = attributeToConfigConverter.GetMqttConfiguration();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                throw;
            }

            var mqttFactory = new MqttFactory();

            return new MqttTriggerBinding(parameter, mqttFactory, mqttConfiguration, _logger);
        }
    }
}

using System.Text;
using Microsoft.Extensions.Options;
using MQTTnet.Formatter;

namespace Org.Product.Infrastructure.Utilities.Options;

public sealed class MqttOptionsValidator : IValidateOptions<MqttOptions>
{
    private static readonly UTF8Encoding _utf8 = new(false, true);

    public ValidateOptionsResult Validate(string? name, MqttOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Host)
            || string.IsNullOrWhiteSpace(options.ClientId)
            || !IsValidUtf8(options.ClientId))
        {
            failures.Add("Mqtt requires Host and a unique, non-empty ClientId.");
        }

        if (options.Port is < 1 or > 65535
            || options.KeepAliveSeconds is < 1 or > 65535
            || options.ReconnectDelaySeconds is < 1 or > 86400
            || options.OperationTimeoutSeconds is < 1 or > 3600
            || options.ReceiveQueueCapacity <= 0)
        {
            failures.Add("Mqtt requires valid Port/KeepAliveSeconds and positive reconnect, timeout, and queue values.");
        }

        if (options.ProtocolVersion is not (MqttProtocolVersion.V311 or MqttProtocolVersion.V500)
            || !Enum.IsDefined(options.PublishQualityOfService))
        {
            failures.Add("Mqtt requires protocol V311 or V500 and QoS 0, 1, or 2.");
        }

        if (options.Password is not null && string.IsNullOrEmpty(options.Username))
        {
            failures.Add("Mqtt requires Username when Password is configured.");
        }

        if (options.Subscriptions is null)
        {
            failures.Add("Mqtt Subscriptions must be a list.");
        }
        else
        {
            var filters = new HashSet<string>(StringComparer.Ordinal);
            foreach (var subscription in options.Subscriptions)
            {
                if (subscription is null
                    || string.IsNullOrEmpty(subscription.TopicFilter)
                    || !Enum.IsDefined(subscription.QualityOfService))
                {
                    failures.Add("Mqtt subscriptions require a filter and QoS 0, 1, or 2.");
                    continue;
                }

                if (!IsValidFilter(subscription.TopicFilter))
                {
                    failures.Add("Mqtt contains an invalid subscription filter.");
                }
                else if (!filters.Add(subscription.TopicFilter))
                {
                    failures.Add("Mqtt subscription filters must be distinct.");
                }
            }
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsValidUtf8(string value)
    {
        if (value.Contains('\0'))
        {
            return false;
        }

        try
        {
            return _utf8.GetByteCount(value) <= ushort.MaxValue;
        }
        catch (EncoderFallbackException)
        {
            return false;
        }
    }

    private static bool IsValidFilter(string filter)
    {
        if (!IsValidUtf8(filter))
        {
            return false;
        }

        var levels = filter.Split('/');
        for (var i = 0; i < levels.Length; i++)
        {
            if ((levels[i].Contains('+') && levels[i] != "+")
                || (levels[i].Contains('#') && (levels[i] != "#" || i != levels.Length - 1)))
            {
                return false;
            }
        }

        return true;
    }
}

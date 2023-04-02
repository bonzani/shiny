﻿using System.Collections.Generic;
using Shiny.Stores;
using Shiny.Support.Repositories.Impl;

namespace Shiny.Notifications.Infrastructure;


public class ChannelRepositoryConverter : RepositoryConverter<Channel>
{
    public override Channel FromStore(IDictionary<string, object> values, ISerializer serializer)
    {
        var channel = new Channel
        {
            Identifier = (string)values[nameof(Channel.Identifier)],
            Description = this.ConvertFromStoreValue<string>(values, nameof(Channel.Description)),
            CustomSoundPath = this.ConvertFromStoreValue<string>(values, nameof(Channel.CustomSoundPath)),
            Sound = this.ConvertFromStoreValue<ChannelSound>(values, nameof(Channel.Sound)),
            Importance = (ChannelImportance)(long)values[nameof(Channel.Importance)]
        };

        if (values.ContainsKey(nameof(Channel.Actions)))
            channel.Actions = serializer.Deserialize<List<ChannelAction>>((string)values[nameof(channel.Actions)])!;

        return channel;
    }

    public override IEnumerable<(string Property, object Value)> ToStore(Channel entity, ISerializer serializer)
    {
        yield return (nameof(entity.Importance), entity.Importance);
        yield return (nameof(entity.Sound), entity.Sound);

        if (entity.CustomSoundPath != null)
            yield return (nameof(entity.CustomSoundPath), entity.CustomSoundPath);

        if (entity.Description != null)
            yield return (nameof(entity.Description), entity.Description);

        if (entity.Actions.Count > 0)
            yield return (nameof(entity.Actions), serializer.Serialize(entity.Actions));
    }
}

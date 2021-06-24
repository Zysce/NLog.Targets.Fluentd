// NLog.Targets.Fluentd
// 
// Copyright (c) 2014 Moriyoshi Koizumi and contributors.
// 
// This file is licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MsgPack;
using MsgPack.Serialization;

namespace NLog.Targets
{
  internal class FluentdEmitter
  {
    private static readonly DateTime unixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly Packer packer;
    private readonly SerializationContext serializationContext;
    private readonly Stream destination;

    public FluentdEmitter(Stream stream)
    {
      destination = stream;
      packer = Packer.Create(destination);
      serializationContext = new SerializationContext(PackerCompatibilityOptions.PackBinaryAsRaw);
      ConfigureSerializationContext();
    }

    public async Task Emit(DateTime timestamp, string tag, IDictionary<string, object?> data, CancellationToken cancellationToken)
    {
      long unixTimestamp = GetUnixTimestamp(timestamp);
      await PackData(tag, data, unixTimestamp, cancellationToken).ConfigureAwait(false);
      await packer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task PackData(string tag, IDictionary<string, object?> data, long unixTimestamp, CancellationToken cancellationToken)
    {
      await packer.PackArrayHeaderAsync(3, cancellationToken).ConfigureAwait(false);
      await packer.PackStringAsync(tag, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
      await packer.PackAsync((ulong)unixTimestamp, cancellationToken).ConfigureAwait(false);
      await packer.PackAsync(data, serializationContext, cancellationToken).ConfigureAwait(false);
    }

    private static long GetUnixTimestamp(DateTime timestamp)
    {
      return timestamp.ToUniversalTime().Subtract(unixEpoch).Ticks / 10000000;
    }

    private void ConfigureSerializationContext()
    {
      var embeddedContext = new SerializationContext(packer.CompatibilityOptions);
      embeddedContext.Serializers.Register(new OrdinaryDictionarySerializer(embeddedContext, null));
      serializationContext.Serializers.Register(new OrdinaryDictionarySerializer(serializationContext, embeddedContext));
    }
  }
}

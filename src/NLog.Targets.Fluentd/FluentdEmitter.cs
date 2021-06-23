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
using MsgPack;
using MsgPack.Serialization;

namespace NLog.Targets
{
  internal class FluentdEmitter
  {
    private static DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly Packer packer;
    private readonly SerializationContext serializationContext;
    private readonly Stream destination;

    public void Emit(DateTime timestamp, string tag, IDictionary<string, object?> data)
    {
      long unixTimestamp = timestamp.ToUniversalTime().Subtract(unixEpoch).Ticks / 10000000;
      packer.PackArrayHeader(3);
      packer.PackString(tag, Encoding.UTF8);
      packer.Pack((ulong)unixTimestamp);
      packer.Pack(data, serializationContext);
      destination.Flush();    // Change to packer.Flush() when packer is upgraded
    }

    public FluentdEmitter(Stream stream)
    {
      destination = stream;
      packer = Packer.Create(destination);
      var embeddedContext = new SerializationContext(packer.CompatibilityOptions);
      embeddedContext.Serializers.Register(new OrdinaryDictionarySerializer(embeddedContext, null));
      serializationContext = new SerializationContext(PackerCompatibilityOptions.PackBinaryAsRaw);
      serializationContext.Serializers.Register(new OrdinaryDictionarySerializer(serializationContext, embeddedContext));
    }
  }
}

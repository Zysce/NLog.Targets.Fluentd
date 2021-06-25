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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MsgPack;
using MsgPack.Serialization;

namespace NLog.Targets
{
  internal class OrdinaryDictionarySerializer : MessagePackSerializer<IDictionary<string, object?>>
  {
    private readonly SerializationContext embeddedContext;

    internal OrdinaryDictionarySerializer(SerializationContext ownerContext, SerializationContext? embeddedContext) : base(ownerContext)
    {
      this.embeddedContext = embeddedContext ?? ownerContext;
    }

    protected override void PackToCore(Packer packer, IDictionary<string, object?> objectTree)
    {
      PackToAsyncCore(packer, objectTree, CancellationToken.None).GetAwaiter().GetResult();
    }

    protected override IDictionary<string, object?> UnpackFromCore(Unpacker unpacker)
    {
      return UnpackFromAsyncCore(unpacker, CancellationToken.None).GetAwaiter().GetResult();
    }

    protected override async Task PackToAsyncCore(Packer packer, IDictionary<string, object?> objectTree, CancellationToken cancellationToken)
    {
      await packer.PackMapHeaderAsync(objectTree, cancellationToken).ConfigureAwait(false);
      foreach (KeyValuePair<string, object?> pair in objectTree)
      {
        await packer.PackStringAsync(pair.Key, cancellationToken).ConfigureAwait(false);
        if (pair.Value == null)
        {
          await packer.PackNullAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
          await packer.PackAsync(pair.Value, embeddedContext, cancellationToken).ConfigureAwait(false);
        }
      }
    }

    protected override async Task<IDictionary<string, object?>> UnpackFromAsyncCore(Unpacker unpacker, CancellationToken cancellationToken)
    {
      if (!unpacker.IsMapHeader)
      {
        throw new InvalidMessagePackStreamException("map header expected");
      }

      var retval = new Dictionary<string, object?>();
      await UnpackTo(unpacker, retval, cancellationToken).ConfigureAwait(false);
      return retval;
    }

    private async Task UnpackTo(Unpacker unpacker, IDictionary<string, object?> dict, long mapLength, CancellationToken cancellationToken)
    {
      for (long i = 0; i < mapLength; i++)
      {
        var key = await GetString(unpacker, cancellationToken).ConfigureAwait(false);
        var value = await GetObject(unpacker, cancellationToken).ConfigureAwait(false);

        if (unpacker.LastReadData.IsNil)
        {
          dict.Add(key, null);
        }
        else if (unpacker.IsMapHeader)
        {
          long innerMapLength = value.AsInt64();
          var innerDict = new Dictionary<string, object?>();
          await UnpackTo(unpacker, innerDict, innerMapLength, cancellationToken).ConfigureAwait(false);
          dict.Add(key, innerDict);
        }
        else if (unpacker.IsArrayHeader)
        {
          long innerArrayLength = value.AsInt64();
          var innerArray = new List<object>();
          await UnpackTo(unpacker, innerArray, innerArrayLength, cancellationToken).ConfigureAwait(false);
          dict.Add(key, innerArray);
        }
        else
        {
          dict.Add(key, value.ToObject());
        }
      }
    }

    private async Task UnpackTo(Unpacker unpacker, IList<object> array, long arrayLength, CancellationToken cancellationToken)
    {
      for (long i = 0; i < arrayLength; i++)
      {
        var value = await GetObject(unpacker, cancellationToken).ConfigureAwait(false);
        if (unpacker.IsMapHeader)
        {
          long innerMapLength = value.AsInt64();
          var innerDict = new Dictionary<string, object?>();
          await UnpackTo(unpacker, innerDict, innerMapLength, cancellationToken).ConfigureAwait(false);
          array.Add(innerDict);
        }
        else if (unpacker.IsArrayHeader)
        {
          long innerArrayLength = value.AsInt64();
          var innerArray = new List<object>();
          await UnpackTo(unpacker, innerArray, innerArrayLength, cancellationToken).ConfigureAwait(false);
          array.Add(innerArray);
        }
        else
        {
          array.Add(value.ToObject());
        }
      }
    }

    private async Task UnpackTo(Unpacker unpacker, IDictionary<string, object?> collection, CancellationToken cancellationToken)
    {
      var mapLength = await GetMapLength(unpacker, cancellationToken).ConfigureAwait(false);
      await UnpackTo(unpacker, collection, mapLength, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> GetString(Unpacker unpacker, CancellationToken cancellationToken)
    {
      var readStringResult = await unpacker.ReadStringAsync(cancellationToken).ConfigureAwait(false);
      return !readStringResult.Success
          ? throw new InvalidMessagePackStreamException("string expected for a map key")
          : readStringResult.Value;
    }

    private static async Task<MessagePackObject> GetObject(Unpacker unpacker, CancellationToken cancellationToken)
    {
      var readObjectResult = await unpacker.ReadObjectAsync(cancellationToken).ConfigureAwait(false);
      return !readObjectResult.Success ? throw new InvalidMessagePackStreamException("unexpected EOF") : readObjectResult.Value;
    }

    private static async Task<long> GetMapLength(Unpacker unpacker, CancellationToken cancellationToken)
    {
      var mapLengthResult = await unpacker.ReadMapLengthAsync(cancellationToken).ConfigureAwait(false);
      return !mapLengthResult.Success ? throw new InvalidMessagePackStreamException("map header expected") : mapLengthResult.Value;
    }
  }
}

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
      packer.PackMapHeader(objectTree);
      foreach (KeyValuePair<string, object?> pair in objectTree)
      {
        packer.PackString(pair.Key);
        if (pair.Value == null)
        {
          packer.PackNull();
        }
        else
        {
          packer.Pack(pair.Value, embeddedContext);
        }
      }
    }

    protected void UnpackTo(Unpacker unpacker, IDictionary<string, object?> dict, long mapLength)
    {
      for (long i = 0; i < mapLength; i++)
      {
        if (!unpacker.ReadString(out string key))
        {
          throw new InvalidMessagePackStreamException("string expected for a map key");
        }

        if (!unpacker.ReadObject(out MessagePackObject value))
        {
          throw new InvalidMessagePackStreamException("unexpected EOF");
        }

        if (unpacker.LastReadData.IsNil)
        {
          dict.Add(key, null);
        }
        else if (unpacker.IsMapHeader)
        {
          long innerMapLength = value.AsInt64();
          var innerDict = new Dictionary<string, object?>();
          UnpackTo(unpacker, innerDict, innerMapLength);
          dict.Add(key, innerDict);
        }
        else if (unpacker.IsArrayHeader)
        {
          long innerArrayLength = value.AsInt64();
          var innerArray = new List<object>();
          UnpackTo(unpacker, innerArray, innerArrayLength);
          dict.Add(key, innerArray);
        }
        else
        {
          dict.Add(key, value.ToObject());
        }
      }
    }

    protected void UnpackTo(Unpacker unpacker, IList<object> array, long arrayLength)
    {
      for (long i = 0; i < arrayLength; i++)
      {
        if (!unpacker.ReadObject(out MessagePackObject value))
        {
          throw new InvalidMessagePackStreamException("unexpected EOF");
        }
        if (unpacker.IsMapHeader)
        {
          long innerMapLength = value.AsInt64();
          var innerDict = new Dictionary<string, object?>();
          UnpackTo(unpacker, innerDict, innerMapLength);
          array.Add(innerDict);
        }
        else if (unpacker.IsArrayHeader)
        {
          long innerArrayLength = value.AsInt64();
          var innerArray = new List<object>();
          UnpackTo(unpacker, innerArray, innerArrayLength);
          array.Add(innerArray);
        }
        else
        {
          array.Add(value.ToObject());
        }
      }
    }

    public void UnpackTo(Unpacker unpacker, IDictionary<string, object?> collection)
    {
      if (!unpacker.ReadMapLength(out long mapLength))
      {
        throw new InvalidMessagePackStreamException("map header expected");
      }
      UnpackTo(unpacker, collection, mapLength);
    }

    protected override IDictionary<string, object?> UnpackFromCore(Unpacker unpacker)
    {
      if (!unpacker.IsMapHeader)
      {
        throw new InvalidMessagePackStreamException("map header expected");
      }

      var retval = new Dictionary<string, object?>();
      UnpackTo(unpacker, retval);
      return retval;
    }

    public void UnpackTo(Unpacker unpacker, object collection)
    {
      if (collection is not IDictionary<string, object?> dictionary)
        throw new NotSupportedException();
      UnpackTo(unpacker, dictionary);
    }
  }
}

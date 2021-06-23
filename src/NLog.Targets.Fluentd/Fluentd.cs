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
using System.Net.Sockets;
using System.Diagnostics;
using System.Reflection;
using System.Linq;

namespace NLog.Targets
{

  [Target("Fluentd")]
  public class Fluentd : TargetWithLayout
  {
    public string Host { get; set; }

    public int Port { get; set; }

    public string Tag { get; set; }

    public bool NoDelay { get; set; }

    public int ReceiveBufferSize { get; set; }

    public int SendBufferSize { get; set; }

    public int SendTimeout { get; set; }

    public int ReceiveTimeout { get; set; }

    public bool LingerEnabled { get; set; }

    public int LingerTime { get; set; }

    public bool EmitStackTraceWhenAvailable { get; set; }

    public bool IncludeAllProperties { get; set; }

    public bool IncludeCallerInfo { get; set; }

    public ISet<string> ExcludeProperties { get; }

    private TcpClient? _client;

    private Stream? _stream;

    private FluentdEmitter? _emitter;

    public Fluentd()
    {
      Host = "127.0.0.1";
      Port = 24224;
      ReceiveBufferSize = 8192;
      SendBufferSize = 8192;
      ReceiveTimeout = 1000;
      SendTimeout = 1000;
      LingerEnabled = true;
      LingerTime = 1000;
      EmitStackTraceWhenAvailable = false;
      Tag = Assembly.GetCallingAssembly().GetName().Name!;
      ExcludeProperties = new HashSet<string>();
      IncludeCallerInfo = false;
    }

    protected override void InitializeTarget()
    {
      base.InitializeTarget();
    }

    protected void EnsureConnected()
    {
      if (_client == null)
      {
        InitializeClient();
        ConnectClient();
      }
      else if (!_client.Connected)
      {
        Cleanup();
        InitializeClient();
        ConnectClient();
      }
    }


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "should catch all exceptions")]
    protected void Cleanup()
    {
      try
      {
        _stream?.Dispose();
        _client?.Close();
      }
      catch (Exception ex)
      {
        Common.InternalLogger.Warn("Fluentd Close - " + ex.ToString());
      }
      finally
      {
        _stream = null;
        _client = null;
        _emitter = null;
      }
    }

    protected override void Dispose(bool disposing)
    {
      Cleanup();
      base.Dispose(disposing);
    }

    protected override void CloseTarget()
    {
      Cleanup();
      base.CloseTarget();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "from nlog")]
    protected override void Write(LogEventInfo logEvent)
    {
      var record = new Dictionary<string, object?> 
            {
                { "level", logEvent.Level.Name },
                { "message", Layout.Render(logEvent) },
                { "logger_name", logEvent.LoggerName },
                { "sequence_id", logEvent.SequenceID },
            };

      AddAdditionnalProperties(logEvent, record);

      EmitRecord(logEvent, record);
    }

    private void AddAdditionnalProperties(LogEventInfo logEvent, Dictionary<string, object?> record)
    {
      if (EmitStackTraceWhenAvailable && logEvent.HasStackTrace)
      {
        AddStackTrace(logEvent, record);
      }

      if (IncludeAllProperties && logEvent.HasProperties)
      {
        AddLoggerProperties(logEvent, record);
      }

      if (IncludeCallerInfo)
      {
        AddCallerInfo(logEvent, record);
      }
    }

    private static void AddCallerInfo(LogEventInfo logEvent, Dictionary<string, object?> record)
    {
      var callerInfo = new Dictionary<string, object?>
        {
          { "class_name", logEvent.CallerClassName},
          { "member_name", logEvent.CallerMemberName},
          { "filePath", logEvent.CallerFilePath},
          { "lineNumber", logEvent.CallerLineNumber}
        };

      record.Add("caller", callerInfo);
    }

    private void EmitRecord(LogEventInfo logEvent, Dictionary<string, object?> record)
    {
      try
      {
        EnsureConnected();
      }
      catch (Exception ex)
      {
        Common.InternalLogger.Warn("Fluentd Connect - " + ex.ToString());
        throw;  // Notify NLog of failure
      }

      try
      {
        _emitter?.Emit(logEvent.TimeStamp, Tag, record);
      }
      catch (Exception ex)
      {
        Common.InternalLogger.Warn("Fluentd Emit - " + ex.ToString());
        throw;  // Notify NLog of failure
      }
    }

    private void AddLoggerProperties(LogEventInfo logEvent, Dictionary<string, object?> record)
    {
      var filteredProperties = logEvent.Properties
                .Where(x =>
                  !string.IsNullOrEmpty(x.Key.ToString()) &&
                  !ExcludeProperties.Contains(x.Key.ToString()!));

      foreach (var property in filteredProperties)
      {
        var propertyKey = property.Key.ToString();

        record[propertyKey!] = SerializePropertyValue(property.Value);
      }
    }

    private static void AddStackTrace(LogEventInfo logEvent, Dictionary<string, object?> record)
    {
      var transcodedFrames = new List<Dictionary<string, object?>>();
      StackTrace stackTrace = logEvent.StackTrace;
      foreach (StackFrame frame in stackTrace.GetFrames())
      {
        var transcodedFrame = new Dictionary<string, object?>
                    {
                        { "filename", frame.GetFileName() },
                        { "line", frame.GetFileLineNumber() },
                        { "column", frame.GetFileColumnNumber() },
                        { "method", frame.GetMethod()?.ToString() },
                        { "il_offset", frame.GetILOffset() },
                        { "native_offset", frame.GetNativeOffset() },
                    };
        transcodedFrames.Add(transcodedFrame);
      }
      record.Add("stacktrace", transcodedFrames);
    }

    private void InitializeClient()
    {
      _client = new TcpClient
      {
        NoDelay = NoDelay,
        ReceiveBufferSize = ReceiveBufferSize,
        SendBufferSize = SendBufferSize,
        SendTimeout = SendTimeout,
        ReceiveTimeout = ReceiveTimeout,
        LingerState = new LingerOption(LingerEnabled, LingerTime)
      };
    }

    private void ConnectClient()
    {
      _client!.Connect(Host, Port);
      _stream = _client.GetStream();
      _emitter = new FluentdEmitter(_stream);
    }

    private static object? SerializePropertyValue(object propertyValue)
    {
      if (propertyValue == null || Convert.GetTypeCode(propertyValue) != TypeCode.Object || propertyValue is decimal)
      {
        return propertyValue;   // immutable
      }
      else
      {
        return propertyValue.ToString();
      }
    }
  }
}

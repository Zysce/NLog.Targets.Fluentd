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

using NLog;
using NLog.Config;

namespace Demo
{
  class Program
  {
    static void Main()
    {
      SimpleTest();
      SimpleTestWithProperties();
    }

    private static void SimpleTest()
    {
      System.Console.WriteLine("Simple Test");
      using (var fluentdTarget = new NLog.Targets.Fluentd())
      {
        fluentdTarget.Layout = new NLog.Layouts.SimpleLayout("${longdate}|${level}|${callsite}|${logger}|${message}");

        var logger = CreateLogger(fluentdTarget);
        logger.Info("Hello World!");
      }


      System.Console.WriteLine("Simple Test Done");
    }

    private static void SimpleTestWithProperties()
    {
      System.Console.WriteLine("Simple Test With Properties");
      using (var fluentdTarget = new NLog.Targets.Fluentd { IncludeAllProperties = true })
      {
        fluentdTarget.Layout = new NLog.Layouts.SimpleLayout("${longdate}|${level}|${callsite}|${logger}|${message}");

        var logger = CreateLogger(fluentdTarget);
        logger.Properties.Add("test", "test");
        logger.Info("Hello World!");
      }

      System.Console.WriteLine("Simple Test With Properties done");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "for demo purpose")]
    private static Logger CreateLogger(NLog.Targets.Fluentd fluentdTarget)
    {
      var config = CreateConfig(fluentdTarget);
      var loggerFactory = new LogFactory(config);
      return loggerFactory.GetLogger("demo");
    }

    private static LoggingConfiguration CreateConfig(NLog.Targets.Fluentd fluentdTarget)
    {
      var config = new LoggingConfiguration();

      config.AddTarget("fluentd", fluentdTarget);
      config.LoggingRules.Add(new LoggingRule("demo", LogLevel.Debug, fluentdTarget));

      return config;
    }

    
  }
}

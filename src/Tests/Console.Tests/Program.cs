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

using Tests.Common;

namespace Demo
{
  class Program
  {
    static void Main()
    {
      //SimpleTest();
      //SimpleTestWithProperties();
      ComplexeTest();
    }

    private static void SimpleTest()
    {
      System.Console.WriteLine("Simple Test");
      using (var factory = new FluentdTargetLoggerFactory())
      {
        var logger = factory.CreateLogger();
        logger.Info("Hello World!");
      }


      System.Console.WriteLine("Simple Test Done");
    }

    private static void SimpleTestWithProperties()
    {
      System.Console.WriteLine("Simple Test With Properties");
      using (var factory = new FluentdTargetLoggerFactory())
      {
        var logger = factory.WithIncludeAllProperties().CreateLogger();
        logger.Properties.Add("test", "test");
        logger.Info("Hello World!");
      }

      System.Console.WriteLine("Simple Test With Properties done");
    }

    private static void ComplexeTest()
    {
      System.Console.WriteLine("Complex Test");
      using (var factory = new FluentdTargetLoggerFactory())
      {
        var logger = factory
          .WithEmitStackTraceWhenAvailable()
          .WithIncludeAllProperties()
          .WithIncludeCallerInfo()
          .CreateLogger();
        logger.Properties.Add("test", "test");
        logger.Info("Hello World!");
      }

      System.Console.WriteLine("Complex Test done");
    }
  }
}

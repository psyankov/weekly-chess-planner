using System.Diagnostics;

namespace Ant.Weekly;

public static class Log
{
   public static TraceSource AppSource { get; } = new TraceSource("App");
   public static TraceSource RatingSource { get; } = new TraceSource("Rating");
   public static TraceSource PlanningSource { get; } = new TraceSource("Planning");
   public static TraceSource MatchingSource { get; } = new TraceSource("Matching");
   public static TraceSource SchedulingSource { get; } = new TraceSource("Scheduling");

   public static TraceListener? FileListener { get; private set; }

   public static void Configure(LoggingConfiguration config)
   {
      AppSource.Listeners.Clear();
      RatingSource.Listeners.Clear();
      PlanningSource.Listeners.Clear();
      MatchingSource.Listeners.Clear();
      SchedulingSource.Listeners.Clear();

      if (config.LogToConsole)
      {
         var consoleListener = new ConsoleTraceListener();

         AppSource.Listeners.Add(consoleListener);
         RatingSource.Listeners.Add(consoleListener);
         PlanningSource.Listeners.Add(consoleListener);
         MatchingSource.Listeners.Add(consoleListener);
         SchedulingSource.Listeners.Add(consoleListener);
      }

      var currentDirectory = Environment.CurrentDirectory;
      var fileInfo = new FileInfo(Path.Join(currentDirectory, $"{App.S.Logging.LogFileName}"));

      if (fileInfo.Exists)
      {
         fileInfo.Delete();
      }

      if (config.LogToFile &&
         (config.Level.Trim().ToLower() != "off" || config.Planning ||
         config.Matching || config.Scheduling || config.Rating))
      {

         FileListener = new DelimitedListTraceListener(config.LogFileName, "FileListener");
         FileListener.TraceOutputOptions |= TraceOptions.DateTime;

         AppSource.Listeners.Add(FileListener);
         RatingSource.Listeners.Add(FileListener);
         PlanningSource.Listeners.Add(FileListener);
         MatchingSource.Listeners.Add(FileListener);
         SchedulingSource.Listeners.Add(FileListener);
      }

      switch (config.Level.Trim().ToLower())
      {
         default:
            Out.Warning($"Invalid log level '{config.Level}' is specified in the application settings - log level is set to 'Off'");
            AppSource.Switch.Level = SourceLevels.Off;
            break;
         case "off":
            AppSource.Switch.Level = SourceLevels.Off;
            break;
         case "error":
            AppSource.Switch.Level = SourceLevels.Error;
            break;
         case "warning":
            AppSource.Switch.Level = SourceLevels.Error;
            AppSource.Switch.Level |= SourceLevels.Warning;
            break;
         case "info":
         case "information":
            AppSource.Switch.Level = SourceLevels.Error;
            AppSource.Switch.Level |= SourceLevels.Warning;
            AppSource.Switch.Level |= SourceLevels.Information;
            break;
         case "verbose":
            AppSource.Switch.Level = SourceLevels.Error;
            AppSource.Switch.Level |= SourceLevels.Warning;
            AppSource.Switch.Level |= SourceLevels.Information;
            AppSource.Switch.Level |= SourceLevels.Verbose;
            break;
      }

      if (config.Rating)
      {
         RatingSource.Switch.Level |= SourceLevels.Verbose;
      }
      if (config.Planning)
      {
         PlanningSource.Switch.Level |= SourceLevels.Verbose;
      }
      if (config.Matching)
      {
         MatchingSource.Switch.Level |= SourceLevels.Verbose;
      }
      if (config.Scheduling)
      {
         SchedulingSource.Switch.Level |= SourceLevels.Verbose;
      }

      Trace.AutoFlush = true;

      BeginLog();
   }

   public static void BeginLog()
   {
      Error("Sample log: app error");
      Warning("Sample log: app warning");
      Info("Sample log: app info");
      Verbose("Sample log: app verbose");

      Rating("Sample log: rating (verbose)");
      Planning("Sample log: planning (verbose)");
      Matching("Sample log: matching (verbose)");
      Scheduling("Sample log: scheduling (verbose)");

      FileListener?.WriteLine("\n");
   }

   public static void Error(string format, params object[] data)
   {
      Write(TraceEventType.Error, format, data);
   }

   public static void Warning(string format, params object[] data)
   {
      Write(TraceEventType.Warning, format, data);
   }

   public static void Info(string format, params object[] data)
   {
      Write(TraceEventType.Information, format, data);
   }

   public static void Verbose(string format, params object[] data)
   {
      Write(TraceEventType.Verbose, format, data);
   }

   public static void Rating(string format, params object[] data)
   {
      Write(RatingSource, TraceEventType.Verbose, 1, format, data);
   }

   public static void Planning(string format, params object[] data)
   {
      Write(PlanningSource, TraceEventType.Verbose, 1, format, data);
   }

   public static void Matching(string format, params object[] data)
   {
      Write(MatchingSource, TraceEventType.Verbose, 1, format, data);
   }

   public static void Scheduling(string format, params object[] data)
   {
      Write(SchedulingSource, TraceEventType.Verbose, 1, format, data);
   }

   private static void Write(TraceEventType eventType, string format, params object[] data)
   {
      Write(eventType, 1, format, data);
   }

   private static void Write(TraceEventType eventType, int id, string format, params object[] data)
   {
      Write(AppSource, eventType, id, format, data);
   }

   private static void Write(TraceSource source, TraceEventType eventType, int id, string format, params object[] data)
   {
      var message = string.Format(format, data);
      source.TraceData(eventType, id, message);
   }

   public static void ApplicationConfiguration(object configurationObject)
   {
      var type = configurationObject.GetType();
      foreach (var propInfo in type.GetProperties())
      {
         var name = propInfo.Name;
         var value = propInfo.GetValue(configurationObject);
         var propertyType = propInfo.PropertyType;
         if (propertyType.IsClass && propertyType != typeof(string))
         {
            if (value != null)
            {
               Info($"{name}");
               ApplicationConfiguration(value);
            }
            else
            {
               var str = $"Property {name} of type {propertyType.Name} has null value in the referenced instance";
               Out.Error(str);
            }
         }
         else
         {
            var str = $"    {name,-35}{value,15}";
            Info(str);
         }
      }
   }
}

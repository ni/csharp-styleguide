# Banned Methods

| Class/Method Banned | Why | Replacement API |
| -------------------- | -------------------- | -------------------- |
| System.Console       | Unless you are a stand alone application, you should use the logging API | NationalInstruments.Core.Logging.Log |
| System.Diagnostics.Trace | Unless you are a stand alone application, you should use the logging API | NationalInstruments.Core.Logging.Log |
| System.Diagnostics.Debug | Unless you are a stand alone application, you should use the logging API | NationalInstruments.Core.Logging.Log |
| Path.GetFileName | Use LongPath instead to avoid problems handling long paths | LongPath.GetFileName |
| Path.GetFileNameWithoutExtension | Use LongPath instead to avoid problems handling long paths | LongPath.GetFileNameWithoutExtension |

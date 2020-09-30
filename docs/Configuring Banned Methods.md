# Configuring Banned Methods (NI1006)

NI1006 is an analyzer rule that prevents specific methods from being used in source. This ensures developers don't accidentally introduce hard-to-find bugs, inappropriate diagnostics, obsolete API usages, etc.

# Process

The analyzer works by looking through "BannedMethods" [additional files](https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Using%20Additional%20Files.md). An additional file's contents will be parsed if the following conditions are true:


1. The filename contains the text "BannedMethods"
2. The contents of the file are XML with the appropriate schema
3. The files are added to the build as an [additional file](https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Using%20Additional%20Files.md)

## Determine the Entry to be Banned

A method invocation is banned if its fully-qualified name starts with an entry's text*. 

For example, let's say we wanted ban `Console.WriteLine("Hello!")`. The fully-qualified name of this method, including parameters, is `System.Console.WriteLine(System.String)`. This entry would ban our example, but it would not ban `Console.WriteLine("Hello,{0}!", "World")`, a method whose signature is `System.Console.WriteLine(System.String, System.Object)`. To ban both, we could change our entry to be `System.Console.WriteLine` (or some other common string).  You can also add a Justification and an Alternative attribute to give more information in the error message, as well an Assemblies attribute to limit the banned method to certain assemblies.

*Technically, the regex is ^{0}(\b|$), so an entry of "System.Con" would not ban "System.Console" methods

## BannedMethods XML Schema

```xml
<BannedMethods>
  <EntryGroup Justification="Unless you are a standalone application, use the logging API"
              Alternative="NationalInstruments.Core.Logging.Log">
    <Entry>System.Console</Entry>
    <Entry Alternative="Use ETW events instead">System.Diagnostics.Trace</Entry>    
  </EntryGroup>
  <Entry Alternative="EventArgs.Empty"
         Justification="Performance - we don't need to create an empty object each time we raise a standard event.">
    System.EventArgs.EventArgs
  </Entry>
</BannedMethods>
```

When creating entries, use either `EntryGroup` or `Entry`. `EntryGroup` is useful to reuse any of the optional attributes across a set of entries. `Entry` items can be defined at the root, as a sibling to `EntryGroup`.

### Optional Attributes

**Assemblies** - Specifies the assembly name where the entry should be banned.

**Justification** - A text justification to display to the developer about why a method is banned.

**Alternative** - The method that should be used instead of the banned method.
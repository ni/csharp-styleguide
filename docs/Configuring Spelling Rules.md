# Configuring Spelling Rules
By default, this package comes with a dictionary as acceptable spellings.

# Process

The analyzer works by looking through `CodeAnalysisDictionary.xml` [additional files](https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Using%20Additional%20Files.md). An additional file's contents will be parsed if the following conditions are true:


1. The filename contains the text `CodeAnalysisDictionary.xml`
2. The files are added to the build as an [additional file](https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Using%20Additional%20Files.md)

# CodeAnalysisDictionary XML Schema

```xml
<?xml version="1.0" encoding="utf-8"?>
<Dictionary>
  <Words>
    <Recognized>
        <Word>example</Word>
    </Recognized>
    <Compound>
      <Term CompoundAlternate="ExampleAlternative">examplealternative</Term>
    </Compound>
    <DiscreteExceptions>
    </DiscreteExceptions>
  </Words>
  <Acronyms>
    <CasingExceptions>
    </CasingExceptions>
  </Acronyms>
</Dictionary>
```

Learn more about the dictionary elements in the XML schema
[here](https://docs.microsoft.com/en-us/visualstudio/code-quality/how-to-customize-the-code-analysis-dictionary?view=vs-2019#custom-dictionary-elements).

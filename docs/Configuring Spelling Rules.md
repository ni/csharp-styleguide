# Configuring Spelling Rules
By default, this package comes with a dictionary containing acceptable spellings.

# Process

The default dictionary can be customized with [additional files](https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Using%20Additional%20Files.md). An additional file's contents will be parsed and applied if its name contains the terms "dictionary" or "custom" and ends with `.xml` or `.dic`.

# XML Schema

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

# DIC Schema

```
words
to
accept
```

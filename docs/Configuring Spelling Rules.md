# Configuring Spelling Rules
By default, this package comes with a dictionary as acceptable spellings.

# Process
In order to add project specific spellings, create a file called `CodeAnalysisDictionary.xml` in the same directory
of your solution file.

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

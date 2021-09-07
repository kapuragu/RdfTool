# RdfTool
 A tool for decompiling and compiling MGSV .rdf files, created using youarebritish's LbaTool source code.
https://github.com/youarebritish/LbaTool

Requirements
--------
```
Microsoft .NET Framework 4.5.1 
```

Usage
--------
Drag and drop one or more .rdf files onto the .exe to unpack them to .rdf.xml files. Do the same with .rdf.xml files to repack them into .rdf format. You can also bind the .rdf format to the .exe. The dialogueEvent, voiceType and voiceId values are found in .sbp soundbanks' HIRC sections, which you'll still have to get manually, as Wwise soundbanks in MGSV still need documentation and tooling.

Detailed binary file format breakdown can be found here:
https://metalgearmodding.fandom.com/wiki/RDF

Credits
--------
Huge thanks to youarebritish for helping me learn how to use this and allowing me use the LbaTool source!
